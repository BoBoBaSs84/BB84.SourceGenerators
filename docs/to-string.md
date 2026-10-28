# ToString Generator

Generates a `ToString()` override for classes, returning a formatted string containing the class name and all (or selected) public readable property values in the format `ClassName { Prop1 = val1, Prop2 = val2 }`. Collection properties (lists, arrays, dictionaries) are rendered with configurable formatting instead of the default type name.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

A good `ToString()` makes debugging, logging and test output much easier, but writing and maintaining one for every model is tedious. Reflection-based helpers avoid the typing but are slow and hard to customize. This generator emits a direct `ToString()` (or `PrintMembers` for records) with options for exclusions, ordering, formatting, collections and null handling.

## Benefits

- Generates a direct property-formatting `ToString()` override at compile time
- Replaces runtime reflection approaches (`typeof(T).GetProperties().Select(...)`) with zero-overhead generated code
- Automatically stays in sync with the class definition - no manual maintenance required
- Supports property exclusion for sensitive or verbose fields

## Attributes

```csharp
[GenerateToString(params string[] excludeProperties)]
[ToStringFormat(string format)]  // per-property
[ToStringOrder(int order)]       // per-property
```

**Parameters:**

- `excludeProperties` - Optional list of property names to exclude from the generated `ToString()` output
- `CollectionFormat` - Controls how collection properties are rendered (named argument, default: `CollectionFormat.Count`)
- `Separator` - Custom separator string between properties (named argument, default: `", "`)
- `Formattable` - When `true`, generates an `IFormattable` implementation so the class participates in composite formatting with custom `IFormatProvider` support (named argument, default: `false`)
- `NullPlaceholder` - When set, nullable properties with a `null` value render this string instead of an empty string (named argument, default: `null` — empty string behavior). Common values: `"null"`, `"<null>"`
- `IncludeInherited` - When `true`, public properties from base classes (up to but not including `object`) are also included in the output after the declared properties. The `excludeProperties` list applies to inherited members as well (named argument, default: `false`)

**Per-Property Formatting:**

- `[ToStringFormat("format")]` - Applied to individual properties to specify a custom format string. The format is passed to the property's `ToString(string)` method at runtime (e.g., `"yyyy-MM-dd"` for dates, `"C2"` for currency, `"F2"` for fixed-point).
- `[ToStringOrder(n)]` - Controls the position of a property in the generated output. Properties with an explicit order appear first in ascending order, followed by unordered properties in their natural declaration order. Can be combined with `[ToStringFormat]` and works alongside `excludeProperties`.

**Collection Format Modes:**

| Mode                                 | Description                                                             | Example output                 |
| ------------------------------------ | ----------------------------------------------------------------------- | ------------------------------ |
| `CollectionFormat.Count` _(default)_ | Shows element count only                                                | `Count = 3`                    |
| `CollectionFormat.Elements`          | Inline elements for lists/arrays; `{key: value}` pairs for dictionaries | `[Alice, Bob]` / `{Alice: 10}` |
| `CollectionFormat.TypeAndCount`      | Shows runtime type name and element count                               | `List\`1 (Count = 3)`          |

`null` collections are always rendered as `null` regardless of the format mode.

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateToString]
public partial class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
    public bool InStock { get; set; }
}

// Exclude sensitive or verbose properties
[GenerateToString("PasswordHash", "InternalNotes")]
public partial class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public string InternalNotes { get; set; }
}

// Collection properties — explicit Elements mode for full display
[GenerateToString(CollectionFormat = CollectionFormat.Elements)]
public partial class Team
{
    public string Name { get; set; }
    public List<string> Members { get; set; }
    public Dictionary<string, int> Scores { get; set; }
}

// Collection properties — Count mode
[GenerateToString(CollectionFormat = CollectionFormat.Count)]
public partial class TeamSummary
{
    public string Name { get; set; }
    public List<string> Members { get; set; }
}

// Per-property format specifiers
[GenerateToString]
public partial class Order
{
    public int Id { get; set; }

    [ToStringFormat("yyyy-MM-dd")]
    public DateTime CreatedAt { get; set; }

    [ToStringFormat("C2")]
    public decimal Total { get; set; }
}

// Custom property separator
[GenerateToString(Separator = " | ")]
public partial class Config
{
    public string Host { get; set; }
    public int Port { get; set; }
}

// IFormattable implementation for culture-aware formatting
[GenerateToString(Formattable = true)]
public partial class Invoice
{
    public string Customer { get; set; }
    public decimal Total { get; set; }
    public DateTime IssuedAt { get; set; }
}

// Explicit null rendering for nullable properties
[GenerateToString(NullPlaceholder = "null")]
public partial class Person
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? Age { get; set; }
}

// Explicit property ordering — ordered properties first, then unordered in declaration order
[GenerateToString]
public partial class Contact
{
    [ToStringOrder(2)]
    public string? LastName { get; set; }

    [ToStringOrder(1)]
    public string? FirstName { get; set; }

    public int Age { get; set; }
}

// Include inherited properties from base classes
public class EntityBase
{
    public int Id { get; set; }
    public string? CreatedBy { get; set; }
}

[GenerateToString(IncludeInherited = true)]
public partial class Order : EntityBase
{
    public decimal Total { get; set; }
    public string? Reference { get; set; }
}
```

### Generated Code

The generator creates a `ToString()` override on the partial class that:

- Includes all public readable properties by default
- Excludes properties specified in the attribute parameter
- Formats output as `ClassName { Prop1 = val1, Prop2 = val2 }`
- Returns `ClassName { }` when all properties are excluded
- For properties decorated with `[ToStringFormat]`, uses the format specifier in the interpolated string (e.g., `{CreatedAt:yyyy-MM-dd}`)
- For collection properties, emits inline formatting based on `CollectionFormat`:
  - `Count`: `Count = N`
  - `Elements`: `[item1, item2]` for arrays/lists, `{key1: val1, key2: val2}` for dictionaries
  - `TypeAndCount`: `TypeName (Count = N)`
- For dictionary properties in `Elements` mode, emits a private generic helper `DictionaryToString<TKey, TValue>`
- When `Formattable = true`, implements `IFormattable` with `ToString(string?, IFormatProvider?)` — properties whose types implement `IFormattable` receive the `formatProvider`; others use their default `ToString()`
- When `NullPlaceholder` is set, nullable properties with a `null` value emit `Property?.ToString() ?? "placeholder"` instead of the raw interpolation slot; `[ToStringFormat]` is preserved as an explicit `.ToString("fmt")` call
- When any property carries `[ToStringOrder(n)]`, properties are reordered: those with an explicit order appear first (ascending by value), followed by unordered properties in their original declaration order
- When `IncludeInherited = true`, public readable properties from all base classes (up to but not including `object`) are appended after the declared properties; `excludeProperties` applies to inherited members as well

### Usage Example

```csharp
var product = new Product
{
    Id = 1,
    Name = "Widget",
    Price = 9.99,
    InStock = true
};

Console.WriteLine(product.ToString());
// Output: Product { Id = 1, Name = Widget, Price = 9.99, InStock = True }

var user = new User
{
    Id = 42,
    Name = "John Doe",
    PasswordHash = "abc123hash",
    InternalNotes = "VIP customer"
};

Console.WriteLine(user.ToString());
// Output: User { Id = 42, Name = John Doe }

var team = new Team
{
    Name = "Alpha",
    Members = new List<string> { "Alice", "Bob", "Charlie" },
    Scores = new Dictionary<string, int> { ["Alice"] = 10, ["Bob"] = 20 }
};

Console.WriteLine(team.ToString());
// Output: Team { Name = Alpha, Members = [Alice, Bob, Charlie], Scores = {Alice: 10, Bob: 20} }

var summary = new TeamSummary
{
    Name = "Beta",
    Members = new List<string> { "Alice", "Bob", "Charlie" }
};

Console.WriteLine(summary.ToString());
// Output: TeamSummary { Name = Beta, Members = Count = 3 }

var order = new Order
{
    Id = 1,
    CreatedAt = new DateTime(2025, 1, 15),
    Total = 1234.56m
};

Console.WriteLine(order.ToString());
// Output: Order { Id = 1, CreatedAt = 2025-01-15, Total = $1,234.56 }

var config = new Config
{
    Host = "localhost",
    Port = 8080
};

Console.WriteLine(config.ToString());
// Output: Config { Host = localhost | Port = 8080 }

var invoice = new Invoice
{
    Customer = "Acme Corp",
    Total = 1234.56m,
    IssuedAt = new DateTime(2025, 1, 15)
};

// IFormattable: pass a custom format provider
Console.WriteLine(invoice.ToString(null, CultureInfo.InvariantCulture));
// Output: Invoice { Customer = Acme Corp, Total = 1234.56, IssuedAt = 01/15/2025 00:00:00 }

// Works with string.Format and interpolated strings
Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}", invoice));

var person = new Person { Id = 1, Name = null, Age = null };
Console.WriteLine(person.ToString());
// Output: Person { Id = 1, Name = null, Age = null }

var personWithValues = new Person { Id = 2, Name = "John", Age = 30 };
Console.WriteLine(personWithValues.ToString());
// Output: Person { Id = 2, Name = John, Age = 30 }

var contact = new Contact { LastName = "Doe", FirstName = "John", Age = 30 };
Console.WriteLine(contact.ToString());
// Output: Contact { FirstName = John, LastName = Doe, Age = 30 }
```

### Record Support

`[GenerateToString]` also targets `record` (record class) declarations, including positional records. By default (`Formattable = false`, the common case) the generator overrides the record's compiler-synthesized `PrintMembers(StringBuilder)` hook instead of `ToString()` directly. This keeps the record's own compiler-generated `ToString()` wrapper (`"TypeName { " + members + " }"`) and its normal `PrintMembers` inheritance chain — a derived record's generated `PrintMembers` calls `base.PrintMembers(builder)` first, exactly like a hand-written record override would:

```csharp
[GenerateToString]
public partial record Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

[GenerateToString]
public partial record PersonSummary(int Id, string DisplayName);

[GenerateToString]
public partial record Employee : Person
{
    public string Department { get; set; }
}

// Person { FirstName = John, LastName = Doe }
// PersonSummary { Id = 1, DisplayName = J. Doe }
// Employee { FirstName = John, LastName = Doe, Department = Sales }  <- chains PrintMembers from Person
```

When `Formattable = true`, the generator falls back to directly overriding `ToString()` and `ToString(string?, IFormatProvider?)` — the same code path used for classes — because `PrintMembers` has no way to carry a format string or `IFormatProvider`. This is an intentional trade-off: a formattable record no longer benefits from the compiler-synthesized `ToString()` wrapper.
