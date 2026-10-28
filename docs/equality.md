# Equality Generator

Generates `Equals(object)`, `Equals(T)`, `GetHashCode()`, `operator ==`, and `operator !=` for classes, implementing `IEquatable<T>`. This replaces tedious and error-prone manual equality implementations with zero-overhead generated code.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

Correct value equality needs `Equals(object)`, `Equals(T)`, `GetHashCode()` and both operators, all kept consistent with each other. Hand-written versions often miss a property, handle `null` wrongly, or compare collections by reference. This generator emits all members from the class definition and updates them when properties change.

## Benefits

- Generates correct `Equals`, `GetHashCode`, `==`, and `!=` implementations at compile time
- Eliminates tedious and error-prone manual equality boilerplate
- Replaces runtime reflection approaches with zero-overhead generated code
- Properly handles null references and value type properties
- Supports property exclusion for volatile or non-significant fields

## Attributes

```csharp
[GenerateEquality(params string[] excludeProperties)]
```

**Parameters:**

- `excludeProperties` - Optional list of property names to exclude from the generated equality comparison
- `IncludeInherited` - When `true`, public properties from base classes (up to but not including `object`) are also included in the equality comparison and hash code. The `excludeProperties` list applies to inherited members as well (named argument, default: `true`)

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateEquality]
public partial class Product
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public double Price { get; set; }
    public bool InStock { get; set; }
}

// Exclude volatile or non-significant properties
[GenerateEquality("CachedHash", "LastAccessed")]
public partial class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? CachedHash { get; set; }
    public DateTime LastAccessed { get; set; }
}
```

### Generated Code

The generator creates the following members on the partial class:

- `Equals(object?)` override — delegates to the typed `Equals(T?)` method
- `Equals(T?)` implementing `IEquatable<T>` — compares all (or selected) public properties
- `GetHashCode()` override — produces a consistent hash from all (or selected) public properties
- `operator ==` and `operator !=` — delegates to `Equals`

**Collection Properties:**

Sequence-like collection properties (`T[]`, `List<T>`, `ImmutableArray<T>`, `ReadOnlyCollection<T>`,
`IReadOnlyList<T>`, `IReadOnlyCollection<T>`) are compared **element-wise** via `SequenceEqual`, and
`Dictionary<TKey, TValue>` properties are compared **order-independently** by key/value. `GetHashCode()`
derives its contribution from the collection contents. `null` and reference-equal collections are
handled. Two instances holding distinct-but-equal collections therefore compare as equal.

### Usage Example

```csharp
var a = new Product { Id = 1, Name = "Widget", Price = 9.99, InStock = true };
var b = new Product { Id = 1, Name = "Widget", Price = 9.99, InStock = true };
var c = new Product { Id = 2, Name = "Gadget", Price = 19.99, InStock = false };

// Typed equality
bool equal = a.Equals(b);    // true
bool different = a.Equals(c); // false

// Operator equality
bool op = a == b;  // true
bool neq = a != c; // true

// Consistent hash codes
bool sameHash = a.GetHashCode() == b.GetHashCode(); // true

// Works with collections that use equality
var set = new HashSet<Product> { a };
bool contains = set.Contains(b); // true

// IEquatable<T> is implemented
IEquatable<Product> equatable = a;
```
