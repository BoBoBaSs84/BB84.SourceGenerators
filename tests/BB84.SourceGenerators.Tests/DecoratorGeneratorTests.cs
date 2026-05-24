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

	[TestMethod]
	public void DecoratorShouldDelegateGenericMethods()
	{
		GenericService inner = new();
		GenericServiceDecorator decorator = new(inner);

		string result = decorator.Transform("hello");

		Assert.AreEqual("hello", result);
	}

	[TestMethod]
	public void DecoratorShouldDelegateGenericMethodsWithMultipleTypeParameters()
	{
		GenericService inner = new();
		GenericServiceDecorator decorator = new(inner);

		object result = decorator.Convert<string, object>("input");

		Assert.IsNotNull(result);
	}

	[TestMethod]
	public void DecoratorShouldDelegateEvents()
	{
		EventService inner = new();
		EventServiceDecorator decorator = new(inner);

		bool changed = false;
		string? status = null;
		decorator.Changed += (s, e) => changed = true;
		decorator.StatusChanged += (s, st) => status = st;

		inner.RaiseChange();

		Assert.IsTrue(changed);
		Assert.AreEqual("changed", status);
	}

	[TestMethod]
	public void DecoratorShouldInvokePartialMethodHooks()
	{
		TracedService inner = new();
		TracedServiceDecorator decorator = new(inner);
		string[] expected = ["Process:before", "Process:after", "Execute:before", "Execute:after"];

		string result = decorator.Process("test");
		decorator.Execute();

		Assert.AreEqual("processed:test", result);
		CollectionAssert.AreEqual(expected, decorator.Log.ToArray());
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

// Generic interface for testing generic method support
public interface IGenericService
{
	T Transform<T>(T input) where T : class;
	TResult Convert<TInput, TResult>(TInput input) where TResult : new();
}

public class GenericService : IGenericService
{
	public T Transform<T>(T input) where T : class => input;
	public TResult Convert<TInput, TResult>(TInput input) where TResult : new() => new();
}

[GenerateDecorator]
public partial class GenericServiceDecorator : IGenericService
{ }

// Event interface for testing event delegation
public delegate void StatusChangedHandler(object sender, string status);

public interface IEventService
{
	event EventHandler? Changed;
	event StatusChangedHandler? StatusChanged;
	void RaiseChange();
}

public class EventService : IEventService
{
	public event EventHandler? Changed;
	public event StatusChangedHandler? StatusChanged;

	public void RaiseChange()
	{
		Changed?.Invoke(this, EventArgs.Empty);
		StatusChanged?.Invoke(this, "changed");
	}
}

[GenerateDecorator]
public partial class EventServiceDecorator : IEventService
{ }

// Decorator with partial method hooks
public interface ITracedService
{
	string Process(string input);
	void Execute();
}

public class TracedService : ITracedService
{
	public string Process(string input) => $"processed:{input}";
	public void Execute() { }
}

[GenerateDecorator]
public partial class TracedServiceDecorator : ITracedService
{
	public List<string> Log { get; } = [];

	partial void OnProcessExecuting() => Log.Add("Process:before");
	partial void OnProcessExecuted() => Log.Add("Process:after");
	partial void OnExecuteExecuting() => Log.Add("Execute:before");
	partial void OnExecuteExecuted() => Log.Add("Execute:after");
}
