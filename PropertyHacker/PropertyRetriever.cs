using SwissILKnife;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace PropertyHacker
{
	internal static class Helpers
	{
		// this class: courtesy of VariableInjection: https://github.com/SirJosh3917/VariableInjection/blob/master/VariableInjection/Helpers.cs

		// https://stackoverflow.com/a/672212/3780113

		public static MemberInfo GetMemberInfo<TSource, TProperty>(this Expression<Func<TSource, TProperty>> propertyLambda)
		{
			var type = typeof(TSource);

			if (!(propertyLambda.Body is MemberExpression memberExpression))
				throw new ArgumentException($"Expression '{propertyLambda.ToString()}' refers to a method, not a property/field.");

			var member = memberExpression.Member;

			if (type != member.ReflectedType &&
				!type.IsSubclassOf(member.ReflectedType))
				throw new ArgumentException($"Expression '{propertyLambda.ToString()}' refers to a member that is not from type {type}.");

			return member;
		}
	}

	public class Modder
	{
		// the name of an auto property's field that the compiler generates
		public const string DefaultBackingfieldName = "<{0}>k__BackingField";

		public static readonly Modder Default = new Modder();

		public Modder()
		{
		}

		// allows for Get(e => e.X);
		public bool TryGet<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda, out EasyField<TSource, TProperty> easyField)
			=> TryGet(propertyLambda.GetMemberInfo(), out easyField);

		// more extensible as to be able to use reflection
		public bool TryGet<TSource, TProperty>(MemberInfo member, out EasyField<TSource, TProperty> easyField)
		{
			// fields don't need anything done to them
			if (member is FieldInfo field)
			{
				easyField = new EasyField<TSource, TProperty>(field);
				return true;
			}

			if (!(member is PropertyInfo property))
			{
				// if it's not a prop/field :(
				throw new ArgumentException(nameof(member));
			}

			// TODO: make it easier to search for different strings
			var members = property.DeclaringType
				.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

			var search = string.Format(DefaultBackingfieldName, property.Name);

			foreach (var i in members)
			{
				if (i.Name == search &&
					i is FieldInfo fieldInfo)
				{
					// found the auto's field
					easyField = new EasyField<TSource, TProperty>(fieldInfo);
					return true;
				}
			}

			easyField = default;
			return false;
		}
	}

	public delegate void Set(object instance, object value);
	public delegate object Get(object instance);

	public delegate void Set<TSource, TProperty>(TSource instance, TProperty value);
	public delegate TProperty Get<TSource, TProperty>(TSource instance);

	public class EasyField
	{
		public EasyField(FieldInfo field)
		{
			Field = field;
			Set = new Set(MemberUtils.GetSetMethod(field));
			Get = new Get(MemberUtils.GetGetMethod(field));
		}

		public FieldInfo Field { get; }
		public Set Set { get; }
		public Get Get { get; }
	}

	// just a generic & type safe wrapper over EasyField
	public class EasyField<TSource, TProperty> : EasyField
	{
		public EasyField(FieldInfo field) : base(field)
		{
			Set = new Set<TSource, TProperty>((instance, value) =>
			{
				base.Set(instance, value);
			});

			Get = new Get<TSource, TProperty>((instance) =>
			{
				return (TProperty)base.Get(instance);
			});
		}

		public new Set<TSource, TProperty> Set { get; }
		public new Get<TSource, TProperty> Get { get; }
	}
}
