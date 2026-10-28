# Builder Generator

Generates a fluent builder class for classes, providing `With{PropertyName}(value)` methods for each public settable property and a `Build()` method that creates the instance.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

Builders make object creation readable, especially in tests and for types with many optional properties. Writing them by hand means one `With...` method per property, and every new property needs a matching builder change. This generator creates the complete fluent builder from the class definition and keeps it in sync automatically.

## Benefits

- Generates a complete fluent builder class at compile time
- Eliminates hand-written builder boilerplate that must be kept in sync with the target class
- Replaces reflection-based or expression-tree-based builder libraries with zero-overhead generated code
- Full nullable reference type support for type-safe builder APIs

## Attributes

```csharp
[GenerateBuilder]
```

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateBuilder]
public partial class UserProfile
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
}
```

### Generated Code

The generator creates a `{ClassName}Builder` class with:

- A `With{PropertyName}(value)` fluent method for each public settable property
- A `Build()` method that creates the instance via object initializer
- Proper nullable reference type annotations
- XML documentation comments

### Usage Example

```csharp
// Create an instance using the fluent builder
UserProfile profile = new UserProfileBuilder()
    .WithId(1)
    .WithName("John Doe")
    .WithEmail("john@example.com")
    .WithAge(30)
    .WithIsActive(true)
    .Build();

// Only set the properties you need - others use default values
UserProfile minimal = new UserProfileBuilder()
    .WithName("Jane Doe")
    .Build();

// Builders can be reused to create multiple instances
var builder = new UserProfileBuilder()
    .WithName("Template User")
    .WithIsActive(true);

UserProfile first = builder.WithId(1).Build();
UserProfile second = builder.WithId(2).Build();
```
