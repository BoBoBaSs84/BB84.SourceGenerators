# Singleton Generator

Generates the singleton pattern for classes, providing a static `Instance` property and a private constructor. Supports thread-safe lazy initialization via `Lazy<T>` (default) or simple static field initialization. When the class implements an interface, the `Instance` property is typed as the interface.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

A correct, thread-safe singleton needs a private constructor, a static instance and the right initialization strategy. Writing this by hand for every service is repetitive and error-prone. This generator emits the pattern with lazy or eager initialization and checks at compile time that the class can actually be created without parameters.

## Benefits

- Generates thread-safe singleton pattern at compile time with zero boilerplate
- Supports both `Lazy<T>`-backed (default) and simple static field initialization
- Automatically types `Instance` as the implemented interface when present
- Compile-time validation prevents usage on classes with `required` members
- Replaces manual singleton implementations that are tedious and error-prone

## Attributes

```csharp
[GenerateSingleton(bool useLazy = true)]
```

**Parameters:**

- `useLazy` - When `true` (default), the singleton is backed by `Lazy<T>` for thread-safe lazy initialization. When `false`, a simple static readonly field is used instead.

**Constraints:**

- The attribute cannot be applied to classes with `required` fields or properties. A compile-time error (`BB84SG0002`) will be emitted if the class contains any required members that prevent parameterless initialization.

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

// Lazy singleton (default)
[GenerateSingleton]
public partial class MyService { }

// Non-lazy singleton
[GenerateSingleton(useLazy: false)]
public partial class MyCache { }

// Singleton with interface
[GenerateSingleton]
internal partial class MyService : IMyService { }
```

### Generated Code (Lazy, no interface)

```csharp
public partial class MyService
{
    private static readonly Lazy<MyService> _lazyInstance = new Lazy<MyService>(() => new MyService());

    /// <summary>
    /// Gets the singleton instance of <see cref="MyService"/>.
    /// </summary>
    public static MyService Instance => _lazyInstance.Value;

    private MyService() { }
}
```

### Generated Code (Lazy, with interface)

```csharp
internal partial class MyService
{
    private static readonly Lazy<MyService> _lazyInstance = new Lazy<MyService>(() => new MyService());

    /// <summary>
    /// Gets the singleton instance of <see cref="MyService"/> as <see cref="IMyService"/>.
    /// </summary>
    public static IMyService Instance => _lazyInstance.Value;

    private MyService() { }
}
```

### Generated Code (Non-lazy, no interface)

```csharp
public partial class MyCache
{
    /// <summary>
    /// Gets the singleton instance of <see cref="MyCache"/>.
    /// </summary>
    public static MyCache Instance { get; } = new MyCache();

    private MyCache() { }
}
```

### Usage Example

```csharp
// Access the singleton instance
MyService service = MyService.Instance;

// With interface typing
IMyService service = MyService.Instance;

// Thread-safe - always returns the same instance
MyService a = MyService.Instance;
MyService b = MyService.Instance;
// a and b are the same reference
```

## Diagnostics

| ID           | Severity | Description                                             |
| ------------ | -------- | ------------------------------------------------------- |
| `BB84SG0002` | Error    | GenerateSingleton requires parameterless initialization |
