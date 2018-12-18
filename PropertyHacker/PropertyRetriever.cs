using SwissILKnife;
using System;
using System.Linq;
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

			// either a property or field
			return member;
		}
	}

	/// <summary>Allows you to "mod"ify properties</summary>
	/// <remarks>puns (:</remarks>
	public class Modder
	{
		public Modder(params Func<string, string>[] stringTransformers)
		{
			_stringTransformers = stringTransformers;
		}

		private Func<string, string>[] _stringTransformers;

		// the name of an auto property's field that the compiler generates
		public const string DefaultBackingfieldName = "<{0}>k__BackingField";

		public static Func<string, string> DefaultTransformer = (name) => string.Format(DefaultBackingfieldName, name);
		public static Modder Default = new Modder(DefaultTransformer);

		/// <summary>Tries to get an EasyField from a field/property</summary>
		/// <typeparam name="TSource">The type of the source</typeparam>
		/// <typeparam name="TProperty">The type of the property</typeparam>
		/// <param name="propertyLambda"><code>e => e.MyProperty</code> syntax sugar</param>
		/// <param name="easyField">The easy field</param>
		/// <exception cref="System.ArgumentException">Expression '<paramref name="propertyLambda"/>' refers to a method, not a property/field</exception>
		/// <exception cref="System.ArgumentException">Expression '<paramref name="propertyLambda"/>' refers to a member that is not from the type <typeparamref name="TSource"/></exception>
		/// <exception cref="System.ArgumentException">member</exception>
		/// <returns>If it could find a field to use</returns>
		public bool TryGet<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda, out EasyField<TSource, TProperty> easyField)
		{
			var member = propertyLambda.GetMemberInfo();

			if (member is PropertyInfo propertyInfo)
			{
				return TryGet(propertyInfo, out easyField);
			}
			else if (member is FieldInfo fieldInfo)
			{
				easyField = new EasyField<TSource, TProperty>(fieldInfo);
				return true;
			}

			// this shouldn't happen...
			throw new Exception($"Unsupported item");
		}

		/// <summary>Tries to get an EasyField from the backing of a property</summary>
		/// <typeparam name="TSource">The type of the source</typeparam>
		/// <typeparam name="TProperty">The type of the property</typeparam>
		/// <param name="property">The <see cref="MemberInfo"/> to use (must be a <see cref="FieldInfo"/> or a <see cref="PropertyInfo"/></param>
		/// <param name="easyField">The easy field</param>
		/// <returns>If it could find a field to use</returns>
		/// <exception cref="System.ArgumentException">member</exception>
		public bool TryGet<TSource, TProperty>(PropertyInfo property, out EasyField<TSource, TProperty> easyField)
		{
			var res = TryGet(property, out EasyField ef);

			if (ef == default)
			{
				easyField = default;
				return res;
			}

			easyField = new EasyField<TSource, TProperty>(ef.Field);

			return res;
		}

		/// <summary>Tries the get an EasyField from the backing of a property</summary>
		/// <param name="property">The property</param>
		/// <param name="easyField">The easy field</param>
		/// <returns>If it could find a field to use</returns>
		public bool TryGet(PropertyInfo property, out EasyField easyField)
		{
			// TODO: make it easier to search for different strings
			var members = property.DeclaringType
				.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

			var name = property.Name;

			var search = new string[_stringTransformers.Length];

			for (var i = 0; i < _stringTransformers.Length; i++)
			{
				search[i] = _stringTransformers[i](name);
			}

			foreach (var i in members)
			{
				if (search.Contains(i.Name) &&
					i is FieldInfo field)
				{
					easyField = new EasyField(field);
					return true;
				}
			}

			easyField = default;
			return false;
		}
	}

	public delegate void Set(object instance, object value);
	public delegate object Get(object instance);

	/// <summary>Allows for easy manipulation of a field</summary>
	public class EasyField
	{
		public EasyField(FieldInfo field)
		{
			Field = field;
			Set = new Set(MemberUtils.GetSetMethod(field));
			Get = new Get(MemberUtils.GetGetMethod(field));
		}

		/// <summary>The field being get/set to</summary>
		public FieldInfo Field { get; }

		/// <summary>A type unsafe way of setting the value of a given instance</summary>
		public Set Set { get; }

		/// <summary>A type unsafe way of getting the value of a given instance</summary>
		public Get Get { get; }
	}

	public delegate void Set<TSource, TProperty>(TSource instance, TProperty value);
	public delegate TProperty Get<TSource, TProperty>(TSource instance);

	/// <summary>A generic & typesafe wrapper of an <see cref="EasyField"/></summary>
	/// <typeparam name="TSource">The class to modify</typeparam>
	/// <typeparam name="TProperty">The type of the property to modify</typeparam>
	public class EasyField<TSource, TProperty> : EasyField
	{
		public EasyField(FieldInfo field) : base(field)
		{
			// literally a wrapper :)

			Set = new Set<TSource, TProperty>((instance, value) =>
			{
				base.Set(instance, value);
			});

			Get = new Get<TSource, TProperty>((instance) =>
			{
				return (TProperty)base.Get(instance);
			});
		}

		/// <summary>A delegate for setting the value of a given instance of <typeparamref name="TSource"/></summary>
		public new Set<TSource, TProperty> Set { get; }

		/// <summary>A delegate for getting the value of a given instance of <typeparamref name="TSource"/></summary>
		public new Get<TSource, TProperty> Get { get; }
	}
}
