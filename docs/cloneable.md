# Cloneable Generator

Generates `Clone()` and `DeepClone()` methods for classes and structs, implementing `ICloneable`. The `Clone()` method performs a shallow copy, while `DeepClone()` recursively deep clones reference-type properties that are also marked with `[GenerateCloneable]`, and creates independent copies of collection properties (`List<T>`, `Dictionary<K,V>`, `T[]`, `ImmutableArray<T>`, read-only collections).

[← Back to overview](../README.md) · [All generators](README.md)

## Why

`MemberwiseClone` only copies shallowly, serialization round-trips are slow, and hand-written clone methods break silently when new properties are added. This generator emits `Clone()` and `DeepClone()` at compile time. Deep cloning follows nested cloneable types and copies collections, so the copy is truly independent.

## Benefits

- Generates `Clone()` and `DeepClone()` methods at compile time with zero runtime overhead
- Replaces `MemberwiseClone` (shallow only), serialization round-trips (slow), and manual clone implementations (error-prone)
- Recursively deep clones reference-type properties marked with `[GenerateCloneable]`
- Automatically deep copies collection properties (`List<T>`, `Dictionary<K,V>`, `T[]`, `ImmutableArray<T>`, read-only collections)
- Deep clones collection elements when they are marked with `[GenerateCloneable]`
- Supports both classes and structs
- Implements `ICloneable` for framework compatibility
- Supports property exclusion for transient or computed fields

## Attributes

```csharp
[GenerateCloneable(params string[] excludeProperties)]
```

**Parameters:**

- `excludeProperties` - Optional list of property names to exclude from the generated clone methods

**Supported Types:**

- Classes (`partial class`)
- Structs (`partial struct`)

**Collection Deep Cloning:**

The following collection types are automatically deep copied during `DeepClone()`:

- `List<T>` — creates a new list; if `T` is marked with `[GenerateCloneable]`, elements are recursively deep cloned
- `Dictionary<TKey, TValue>` — creates a new dictionary; if `TValue` is marked with `[GenerateCloneable]`, values are recursively deep cloned
- `T[]` (arrays) — creates a new array copy; if `T` is marked with `[GenerateCloneable]`, elements are recursively deep cloned
- `ImmutableArray<T>` — value type, copied directly; if `T` is marked with `[GenerateCloneable]`, elements are recursively deep cloned into a new `ImmutableArray<T>`
- `ReadOnlyCollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>` — creates a new read-only collection; if `T` is marked with `[GenerateCloneable]`, elements are recursively deep cloned

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateCloneable]
public partial class UserProfile
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public double Score { get; set; }
    public Address? Address { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, int>? Scores { get; set; }
}

[GenerateCloneable]
public partial class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
}

// Exclude specific properties from cloning
[GenerateCloneable("CacheToken")]
public partial class Session
{
    public int Id { get; set; }
    public string? User { get; set; }
    public string? CacheToken { get; set; }
}

// Struct support
[GenerateCloneable]
public partial struct Coordinate
{
    public double X { get; set; }
    public double Y { get; set; }
    public List<string>? Labels { get; set; }
}
```

### Generated Code

The generator creates the following members on the partial class or struct:

- `Clone()` — creates a shallow copy by assigning all included public read/write properties (for structs, copies the value)
- `DeepClone()` — creates a deep copy; reference-type properties marked with `[GenerateCloneable]` are recursively deep cloned, collection properties are independently copied, while other properties are shallow copied
- Explicit `ICloneable.Clone()` — delegates to `DeepClone()`

### Usage Example

```csharp
var original = new UserProfile
{
    Id = 1,
    Name = "John Doe",
    Score = 95.5,
    Address = new Address { Street = "123 Main St", City = "Springfield" },
    Tags = new List<string> { "admin", "user" },
    Scores = new Dictionary<string, int> { ["math"] = 95, ["science"] = 88 }
};

// Shallow clone - Address and collections are the same reference
UserProfile shallow = original.Clone();
shallow.Address.City = "Shelbyville";
Console.WriteLine(original.Address.City); // "Shelbyville" (shared reference)

// Deep clone - Address and collections are new independent instances
UserProfile deep = original.DeepClone();
deep.Address.City = "Capital City";
deep.Tags.Add("editor");
deep.Scores["art"] = 72;
Console.WriteLine(original.Address.City); // "Shelbyville" (unaffected)
Console.WriteLine(original.Tags.Count);   // 2 (unaffected)
Console.WriteLine(original.Scores.Count); // 2 (unaffected)

// ICloneable is implemented (delegates to DeepClone)
ICloneable cloneable = original;
object copy = cloneable.Clone();

// Excluded properties are not copied
var session = new Session { Id = 1, User = "admin", CacheToken = "abc123" };
Session clonedSession = session.Clone();
Console.WriteLine(clonedSession.CacheToken); // null

// Struct cloning
var coord = new Coordinate { X = 1.0, Y = 2.0, Labels = new List<string> { "origin" } };
Coordinate clonedCoord = coord.DeepClone();
clonedCoord.Labels.Add("copy");
Console.WriteLine(coord.Labels.Count); // 1 (unaffected)
```
