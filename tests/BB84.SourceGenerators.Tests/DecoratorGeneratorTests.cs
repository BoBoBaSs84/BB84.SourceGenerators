// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class DecoratorGeneratorTests
{
	[TestMethod]
	public void DecoratorShouldDelegateMethodCalls()
	{
		DecoratorTestService inner = new();
		DecoratorTestServiceDecorator decorator = new(inner);

		string result = decorator.GetMessage("Hello");

		Assert.AreEqual("Hello!", result);
	}

	[TestMethod]
	public void DecoratorShouldDelegateVoidMethodCalls()
	{
		DecoratorTestService inner = new();
		DecoratorTestServiceDecorator decorator = new(inner);

		decorator.DoWork();

		Assert.IsTrue(inner.WorkDone);
	}

	[TestMethod]
	public void DecoratorShouldDelegatePropertyGet()
	{
		DecoratorTestService inner = new() { Name = "Test" };
		DecoratorTestServiceDecorator decorator = new(inner);

		Assert.AreEqual("Test", decorator.Name);
	}

	[TestMethod]
	public void DecoratorShouldDelegatePropertySet()
	{
		DecoratorTestService inner = new();
		DecoratorTestServiceDecorator decorator = new(inner)
		{
			Name = "Updated"
		};

		Assert.AreEqual("Updated", inner.Name);
	}

	[TestMethod]
	public void DecoratorShouldDelegateReadOnlyProperty()
	{
		DecoratorTestService inner = new();
		DecoratorTestServiceDecorator decorator = new(inner);

		int count = decorator.Count;

		Assert.AreEqual(42, count);
	}

	[TestMethod]
	public void DecoratorShouldAllowOverridingMethods()
	{
		DecoratorTestService inner = new();
		OverridingDecorator decorator = new(inner);

		string result = decorator.GetMessage("Hello");

		Assert.AreEqual("[DECORATED] Hello!", result);
	}

	[TestMethod]
	public void DecoratorShouldAllowOverridingProperties()
	{
		DecoratorTestService inner = new() { Name = "Original" };
		OverridingDecorator decorator = new(inner);

		Assert.AreEqual("ORIGINAL", decorator.Name);
	}

	[TestMethod]
	public void DecoratorShouldDelegateMultipleInterfaceMembers()
	{
		MultiInterfaceService inner = new();
		MultiInterfaceServiceDecorator decorator = new(inner);

		string msg = decorator.GetMessage("Hi");
		int value = decorator.Calculate(3, 4);

		Assert.AreEqual("Hi!", msg);
		Assert.AreEqual(7, value);
	}

	[TestMethod]
	public void DecoratorConstructorShouldRejectNull()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new DecoratorTestServiceDecorator(null!));
	}
}

public interface IDecoratorTestService
{
	string? Name { get; set; }
	int Count { get; }
	string GetMessage(string input);
	void DoWork();
}

public interface ICalculator
{
	int Calculate(int a, int b);
}

public class DecoratorTestService : IDecoratorTestService
{
	public string? Name { get; set; }
	public int Count => 42;
	public bool WorkDone { get; private set; }

	public string GetMessage(string input) => $"{input}!";

	public void DoWork() => WorkDone = true;
}

public class MultiInterfaceService : IDecoratorTestService, ICalculator
{
	public string? Name { get; set; }
	public int Count => 0;

	public string GetMessage(string input) => $"{input}!";
	public void DoWork() { }
	public int Calculate(int a, int b) => a + b;
}

[GenerateDecorator]
public partial class DecoratorTestServiceDecorator : IDecoratorTestService
{ }

[GenerateDecorator]
public partial class MultiInterfaceServiceDecorator : IDecoratorTestService, ICalculator
{ }

public class OverridingDecorator(IDecoratorTestService inner) : DecoratorTestServiceDecorator(inner)
{
	public override string GetMessage(string input)
		=> $"[DECORATED] {base.GetMessage(input)}";

	public override string? Name
	{
		get => base.Name?.ToUpperInvariant();
		set => base.Name = value;
	}
}
