# Developer Reference

[← Back to overview](../README.md) · [All generators](README.md)

## SourceBuilder

`SourceBuilder` is an internal helper used by all generators to emit indented C# source text. Each `Open*` method appends the declaration line and a `{`, increments the indent level, and returns `this` for chaining. Each `Close*` method is a semantic alias for `CloseBrace()`.

| Method                                                 | Emitted declaration                                  |
| ------------------------------------------------------ | ---------------------------------------------------- |
| `OpenNamespace("My.NS")`                               | `namespace My.NS`                                    |
| `OpenClass("public", "Foo")`                           | `public partial class Foo`                           |
| `OpenClass("public", "Foo", "IDisposable")`            | `public partial class Foo : IDisposable`             |
| `OpenSealedClass("public", "Foo")`                     | `public sealed class Foo`                            |
| `OpenSealedClass("public", "Foo", "IDisposable")`      | `public sealed class Foo : IDisposable`              |
| `OpenStruct("public", "Foo")`                          | `public partial struct Foo`                          |
| `OpenStruct("public", "Foo", "IEquatable<Foo>")`       | `public partial struct Foo : IEquatable<Foo>`        |
| `OpenRecord("public", "Bar")`                          | `public partial record Bar`                          |
| `OpenRecord("public", "Bar", "IDisposable")`           | `public partial record Bar : IDisposable`            |
| `OpenSealedRecord("public", "Bar")`                    | `public sealed partial record Bar`                   |
| `OpenSealedRecord("public", "Bar", "BaseRecord")`      | `public sealed partial record Bar : BaseRecord`      |
| `OpenRecordStruct("public", "Baz")`                    | `public partial record struct Baz`                   |
| `OpenRecordStruct("public", "Baz", "IEquatable<Baz>")` | `public partial record struct Baz : IEquatable<Baz>` |

Close methods: `CloseNamespace()`, `CloseClass()`, `CloseStruct()`, `CloseRecord()`, `CloseRecordStruct()` — all delegate to `CloseBrace()`.
