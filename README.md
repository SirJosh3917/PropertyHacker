# PropertyHacker
Set &amp; Get values of *any* auto property in C#.



# Example

Simple example:

```cs
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
```

As you could expect, calling Change on an instance of ForeignClass will set the get-only property to any value you desire.