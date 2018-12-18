using FluentAssertions;
using System;
using Xunit;

namespace PropertyHacker.Tests
{
	public class Tests
	{
		public class ExampleClass
		{
			public string GetSet { get; set; }
			public string GetOnly { get; }
			public string SetOnly { private get; set; }

			public string SetOnlyValue() => SetOnly;
		}

		public ExampleClass Instance { get; } = new ExampleClass();
		public const string StringValue = "test";

		[Fact]
		public void GetsAndSetGetSet()
		{
			Modder.TryGet(e => e.GetSet, out var myProp)
							.Should().BeTrue();

			myProp.Set(Instance, StringValue);

			myProp.Get(Instance)
				.Should().Be(StringValue);

			Instance.GetSet
				.Should().Be(StringValue);
		}

		[Fact]
		public void GetsAndSetsGetOnly()
		{
			Modder.TryGet(e => e.GetOnly, out var myProp)
							.Should().BeTrue();

			myProp.Set(Instance, StringValue);

			myProp.Get(Instance)
				.Should().Be(StringValue);

			Instance.GetOnly
				.Should().Be(StringValue);
		}

		[Fact]
		public void GetsAndSetsSetOnly()
		{
			Modder.TryGet(typeof(ExampleClass).GetProperty(nameof(ExampleClass.SetOnly)), out var myProp)
										.Should().BeTrue();

			myProp.Set(Instance, StringValue);

			myProp.Get(Instance)
				.Should().Be(StringValue);

			Instance.SetOnlyValue()
				.Should().Be(StringValue);
		}

		[Fact]
		public void ReallyCanModifyAnything()
		{
			Modder.TryGet(e => e.GetSet, out var myProp)
							.Should().BeTrue();
			Modder.TryGet(e => e.Get, out var modifyEasyField)
							.Should().BeTrue();

			myProp.Set(Instance, StringValue);
			myProp.Get(Instance)
				.Should().Be(StringValue);

			// now modify the delegate lol

			modifyEasyField.Set(myProp, (e) =>
			{
				// break it :)
				return "xP";
			});

			// does it work?

			myProp.Get(Instance)
				.Should().Be("xP");

			// and just to make sure this is still StringValue

			Instance.GetSet
				.Should().Be(StringValue);
		}
	}

	public class Example
	{
		public class ForeignClass
		{
			public string ChangeMe { get; }
		}

		public class ForeignClassModifier
		{
			public ForeignClassModifier()
			{
				var pr = new Modder();

				if (!Modder.TryGet(e => e.ChangeMe, out var property))
				{
					throw new ArgumentException("Can't modify the property.");
				}

				_backer = property;
			}

			public EasyField<ForeignClass, string> _backer;

			public void Change(ForeignClass instance, string value)
			{
				_backer.Set(instance, value);
			}
		}

		[Fact]
		public void ExampleWorks()
		{
			var fcm = new ForeignClassModifier();
			var instance = new ForeignClass();

			instance.ChangeMe
				.Should().Be(default);

			const string value = "new";

			fcm.Change(instance, value);

			instance.ChangeMe
				.Should().Be(value);
		}
	}
}
