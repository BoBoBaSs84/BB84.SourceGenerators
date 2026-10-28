# Enumerator Extensions Generator

Generates high-performance extension methods for enumerations, providing faster alternatives to `Enum.ToString()`, `Enum.IsDefined()`, `Enum.GetNames()`, and `Enum.GetValues()`.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

The built-in `Enum` helpers (`ToString()`, `IsDefined()`, `GetNames()`, `GetValues()`) rely on reflection and boxing. On hot paths such as logging, serialization or UI binding this adds measurable allocations and CPU time. Reading `[Description]` attributes by hand needs even more reflection code. This generator emits plain `switch`-based extension methods at compile time, so every call is a direct lookup without reflection.

## Benefits

The generated enum extension methods provide significant performance improvements over reflection-based `Enum` methods:

- **ToStringFast()** - Avoids boxing and uses switch expressions
- **IsDefinedFast()** - Compile-time switch instead of runtime reflection
- **GetNamesFast()/GetValuesFast()** - Returns pre-allocated arrays instead of reflection

## Attributes

```csharp
[GenerateEnumeratorExtensions]
```

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateEnumeratorExtensions]
public enum Status
{
    [System.ComponentModel.Description("Pending approval")]
    Pending = 0,
    Active = 1,
    [System.ComponentModel.Description("Temporarily inactive")]
    Inactive = 2,
    Deleted = 3
}
```

### Generated Methods

The generator creates the following extension methods:

- `ToStringFast()` - Returns the name of the enum value as a string
- `IsDefinedFast(this TEnum value)` - Checks if an enum value is defined
- `IsDefinedFast(string name)` - Checks if an enum name is defined
- `GetNamesFast()` - Returns all enum names as an `IEnumerable<string>` (static method)
- `GetValuesFast()` - Returns all enum values as an `IEnumerable<TEnum>` (static method)
- `GetDescriptionFast()` - Returns the description from `[Description]` attribute, or the name if not present

### Usage Example

```csharp
var status = Status.Pending;

// Fast string conversion
string name = status.ToStringFast(); // "Pending"

// Check if defined
bool isDefined = status.IsDefinedFast(); // true
bool isNameDefined = StatusExtensions.IsDefinedFast("Active"); // true

// Get description
string description = status.GetDescriptionFast(); // "Pending approval"

// Get all names and values
IEnumerable<string> names = StatusExtensions.GetNamesFast();
IEnumerable<Status> values = StatusExtensions.GetValuesFast();
```
