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

			private string _customBacking;
			public string CustomBacking { get => _customBacking; set => _customBacking = value; }

			public string SetOnlyValue() => SetOnly;
		}

		public ExampleClass Instance { get; } = new ExampleClass();
		public const string StringValue = "test";

		[Fact]
		public void FindsCustomBacking()
		{
			var modder = new Modder(Modder.DefaultTransformer, (n) =>
			{
				// don't copy and paste into your own code if you are reading this please
				var piece = n.Substring(0, 1).ToLower();
				return $"_{piece}{n.Substring(1)}";
			});

			modder.TryGet<ExampleClass, string>(e => e.CustomBacking, out var ef)
				.Should().BeTrue();

			var ec = new ExampleClass();

			ec.CustomBacking
				.Should().BeNull();

			const string value = "test";

			ef.Set(ec, value);
			ef.Get(ec)
				.Should().Be(value);

			ec.CustomBacking
				.Should().Be(value);
		}

		[Fact]
		public void GetsAndSetGetSet()
		{
			Modder.Default.TryGet<ExampleClass, string>(e => e.GetSet, out var myProp)
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
			Modder.Default.TryGet<ExampleClass, string>(e => e.GetOnly, out var myProp)
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
			Modder.Default.TryGet<ExampleClass, string>(typeof(ExampleClass).GetProperty(nameof(ExampleClass.SetOnly)), out var myProp)
										.Should().BeTrue();

			myProp.Set(Instance, StringValue);

			myProp.Get(Instance)
				.Should().Be(StringValue);

			Instance.SetOnlyValue()
				.Should().Be(StringValue);
		}

		[Fact]
		public void DoesFail()
		{
			var failModder = new Modder((name) => "");
			failModder.TryGet<ExampleClass, string>(e => e.GetOnly, out var ef)
				.Should().BeFalse();

			ef.Should().Be(default);
		}

		[Fact]
		public void ReallyCanModifyAnything()
		{
			Modder.Default.TryGet<ExampleClass, string>(e => e.GetSet, out var myProp)
							.Should().BeTrue();
			Modder.Default.TryGet<EasyField<ExampleClass, string>, Get<ExampleClass, string>>(e => e.Get, out var modifyEasyField)
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

				if (!Modder.Default.TryGet<ForeignClass, string>(e => e.ChangeMe, out var property))
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
