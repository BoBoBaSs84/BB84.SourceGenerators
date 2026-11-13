# Enumerator Extensions Generator

Generates high-performance extension methods for enumerations, providing faster alternatives to `Enum.ToString()`, `Enum.IsDefined()`, `Enum.TryParse()`, `Enum.GetNames()`, and `Enum.GetValues()`.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

The built-in `Enum` helpers (`ToString()`, `IsDefined()`, `TryParse()`, `GetNames()`, `GetValues()`) rely on reflection and boxing. On hot paths such as logging, serialization or UI binding this adds measurable allocations and CPU time. Reading `[Description]` attributes by hand needs even more reflection code. This generator emits plain `switch`-based extension methods at compile time, so every call is a direct lookup without reflection.

## Benefits

The generated enum extension methods provide significant performance improvements over reflection-based `Enum` methods:

- **ToStringFast()** - Avoids boxing and uses switch expressions
- **IsDefinedFast()** - Compile-time switch instead of runtime reflection
- **TryParseFast()** - Compile-time switch over member names; rejects numeric strings and unintended flag combinations
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
- `TryParseFast(string? value, out TEnum result)` - Parses an exact member name (ordinal, case-sensitive) into the enum value (static method)
- `TryParseFast(string? value, out TEnum result, params string[] separators)` - Parses delimited member names into a combined value; only generated for `[Flags]` enums (static method)
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

// Parse a member name
bool parsed = StatusExtensions.TryParseFast("Active", out Status active); // true, Status.Active
bool numeric = StatusExtensions.TryParseFast("1", out _); // false, numeric strings are rejected

// Get description
string description = status.GetDescriptionFast(); // "Pending approval"

// Get all names and values
IEnumerable<string> names = StatusExtensions.GetNamesFast();
IEnumerable<Status> values = StatusExtensions.GetValuesFast();
```

### Flags Enums

For enumerations decorated with `[Flags]`, an additional `TryParseFast` overload parses member names delimited by any of the given separators and combines them with a bitwise OR. Each token is trimmed and empty tokens are skipped. The call fails, and `result` is set to `default`, if any token is not an exact member name or if no token remains. Without separators (`null` or empty) the overload behaves like the exact-name `TryParseFast`.

```csharp
[Flags]
[GenerateEnumeratorExtensions]
public enum Permissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    All = Read | Write | Execute
}

PermissionsExtensions.TryParseFast("Read|Write", out Permissions a, "|");             // true, Read | Write
PermissionsExtensions.TryParseFast("Read, Execute", out Permissions b, ",");          // true, Read | Execute
PermissionsExtensions.TryParseFast("Read | Write; Execute", out Permissions c, "|", ";"); // true, All
PermissionsExtensions.TryParseFast("Read|Unknown", out Permissions d, "|");           // false, None
PermissionsExtensions.TryParseFast("Read, Write", out Permissions e);                 // false, combinations need separators
```
