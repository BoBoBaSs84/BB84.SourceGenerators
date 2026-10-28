# Factory Generator

Generates a factory class that discovers all concrete implementations of a target interface at compile time and emits a `Create(string key)` method that maps keys to concrete types. This eliminates runtime assembly scanning and manual factory maintenance entirely.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

Key-based factories usually either scan assemblies with reflection at runtime or keep a hand-maintained `switch` that must be updated for every new implementation. This generator finds all implementations of an interface at compile time and emits the `switch` for you, with duplicate-key checks.

## Benefits

- Discovers all concrete implementations of a target interface at compile time
- Eliminates runtime assembly scanning via `Assembly.GetTypes()` and reflection
- Generates a `Create(string key)` method using switch expressions for zero-overhead dispatch
- Supports custom keys via `[GenerateFactoryKey]` attribute
- Compile-time detection of duplicate keys prevents runtime surprises
- Automatically stays in sync as implementations are added or removed

## Attributes

```csharp
[GenerateFactory(Type interfaceType)]
[GenerateFactoryKey(string key)]
```

**Parameters:**

- `GenerateFactory` - Marks a partial class for factory code generation. The `interfaceType` parameter specifies the interface whose implementations should be discovered.
- `GenerateFactoryKey` - Optional attribute placed on implementation classes to specify a custom key. When not present, the class name is used as the key.

**Constraints:**

- The `interfaceType` must be an interface. A compile-time error (`BB84SG0004`) will be emitted if a non-interface type is specified.
- Duplicate factory keys among implementations will produce a compile-time error (`BB84SG0005`).
- Only concrete classes with a public parameterless constructor are included.

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

public interface IAnimal
{
    string Speak();
}

public class Dog : IAnimal
{
    public string Speak() => "Woof!";
}

public class Cat : IAnimal
{
    public string Speak() => "Meow!";
}

[GenerateFactory(typeof(IAnimal))]
public partial class AnimalFactory
{ }
```

### Generated Code

The generator creates the following static methods on the partial class:

- `Create(string key)` - Creates an instance of the target interface based on the specified key using a switch expression. Throws `ArgumentException` for unknown keys.
- `GetKeys()` - Returns an `IEnumerable<string>` of all registered factory keys.

### Usage Example

```csharp
// Create instances by type name (default key)
IAnimal dog = AnimalFactory.Create("Dog");
IAnimal cat = AnimalFactory.Create("Cat");

Console.WriteLine(dog.Speak()); // "Woof!"
Console.WriteLine(cat.Speak()); // "Meow!"

// Unknown keys throw ArgumentException
try
{
    AnimalFactory.Create("Fish");
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message); // "No implementation registered for key 'Fish'."
}

// Enumerate all registered keys
foreach (string key in AnimalFactory.GetKeys())
{
    Console.WriteLine(key); // "Dog", "Cat"
}

// Use custom keys with [GenerateFactoryKey]
public interface IVehicle
{
    string Type { get; }
}

[GenerateFactoryKey("sedan")]
public class Car : IVehicle
{
    public string Type => "Car";
}

[GenerateFactoryKey("pickup")]
public class Truck : IVehicle
{
    public string Type => "Truck";
}

[GenerateFactory(typeof(IVehicle))]
public partial class VehicleFactory
{ }

// Create using custom keys
IVehicle vehicle = VehicleFactory.Create("sedan");
Console.WriteLine(vehicle.Type); // "Car"
```

## Diagnostics

| ID           | Severity | Description                                |
| ------------ | -------- | ------------------------------------------ |
| `BB84SG0004` | Error    | GenerateFactory requires an interface type |
| `BB84SG0005` | Error    | Duplicate factory key detected             |
