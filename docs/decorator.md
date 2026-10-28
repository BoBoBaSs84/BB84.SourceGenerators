# Decorator Generator

Generates a decorator class that wraps an inner instance, delegates all interface members to it, and exposes them as virtual methods and properties for selective overriding. This replaces runtime proxy generation (e.g., `DispatchProxy`, Castle.Core `DynamicProxy`) with zero-overhead compile-time code.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

Decorators add cross-cutting behavior such as logging, caching or retries without changing the original class. A hand-written decorator must forward every interface member, and runtime proxies like `DispatchProxy` or Castle `DynamicProxy` use reflection. This generator emits the full forwarding class at compile time, with virtual members and partial hooks you can override.

## Benefits

- Generates full interface delegation at compile time with zero runtime overhead
- Replaces `DispatchProxy`, Castle.Core `DynamicProxy`, and manual decorator implementations
- All generated members are virtual, enabling selective behavior overriding
- Supports multiple interfaces on a single decorator class
- Fully supports generic interface methods with type parameters and constraints
- Delegates interface events via `add`/`remove` accessors
- Generates `partial void` hooks (`On{Method}Executing`/`On{Method}Executed`) for optional logging/tracing without subclassing
- Automatically stays in sync with interface changes — no manual maintenance required
- Constructor injection of the inner instance with null checking

## Attributes

```csharp
[GenerateDecorator]
```

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

public interface IMyService
{
    string? Name { get; set; }
    int Count { get; }
    event EventHandler? Changed;
    string GetMessage(string input);
    T Transform<T>(T input) where T : class;
    void DoWork();
}

[GenerateDecorator]
public partial class MyServiceDecorator : IMyService
{ }
```

### Generated Code

The generator creates the following members on the partial class:

- A constructor accepting the interface type and storing it as a protected field (`_inner`)
- Virtual property implementations that delegate to the inner instance
- Virtual method implementations that delegate to the inner instance
- Event `add`/`remove` accessors that delegate to the inner instance
- Generic method implementations preserving type parameters and constraints
- `partial void On{MethodName}Executing()` and `partial void On{MethodName}Executed()` declarations for each method, invoked before and after delegation respectively
- Null check on the constructor parameter

### Usage Example

```csharp
// Basic delegation - all calls forwarded to inner instance
var inner = new MyService();
var decorator = new MyServiceDecorator(inner);

decorator.DoWork();           // delegates to inner.DoWork()
string msg = decorator.GetMessage("Hi"); // delegates to inner.GetMessage("Hi")

// Generic methods work seamlessly
string result = decorator.Transform("hello"); // delegates to inner.Transform<string>("hello")

// Events are delegated
decorator.Changed += (s, e) => Console.WriteLine("Changed!");

// Override specific behavior by inheriting from the decorator
public class LoggingDecorator(IMyService inner) : MyServiceDecorator(inner)
{
    public override string GetMessage(string input)
    {
        Console.WriteLine($"GetMessage called with: {input}");
        return base.GetMessage(input);
    }

    public override string? Name
    {
        get => base.Name?.ToUpperInvariant();
        set => base.Name = value;
    }
}

// Use partial method hooks for logging/tracing without subclassing
[GenerateDecorator]
public partial class TracedServiceDecorator : IMyService
{
    partial void OnGetMessageExecuting()
        => Console.WriteLine("GetMessage is about to be called");

    partial void OnGetMessageExecuted()
        => Console.WriteLine("GetMessage has completed");

    partial void OnDoWorkExecuting()
        => Trace.TraceInformation("DoWork starting");

    partial void OnDoWorkExecuted()
        => Trace.TraceInformation("DoWork completed");
}

// Multiple interfaces are supported
public interface ICalculator
{
    int Calculate(int a, int b);
}

[GenerateDecorator]
public partial class MultiDecorator : IMyService, ICalculator
{ }

// In your DI container
services.AddSingleton<IMyService>(sp =>
    new LoggingDecorator(sp.GetRequiredService<MyService>()));
```

### Features

- **Full interface delegation** — properties, methods, and events are all delegated to the inner instance
- **Generic method support** — generic interface methods with type parameters and constraints are fully supported
- **Event delegation** — interface events are delegated via `add`/`remove` accessors
- **Partial method hooks** — `partial void On{MethodName}Executing()` and `partial void On{MethodName}Executed()` methods are generated for each interface method, enabling optional logging/tracing without subclassing

## Diagnostics

| ID           | Severity | Description                                       |
| ------------ | -------- | ------------------------------------------------- |
| `BB84SG0003` | Error    | GenerateDecorator requires at least one interface |
