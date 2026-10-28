# Documentation

[← Back to overview](../README.md)

## Generators

| Generator                                                       | Description                                                                                                                 |
| --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| [Abstraction Generator](abstraction.md)                         | Interface and implementation generation for static classes                                                                  |
| [ActionCommand Generator](action-command.md)                    | Compile-time `ICommand` property generation (sync, async, parameterized, cancellable) for MVVM                              |
| [Assembly Information Generator](assembly-information.md)       | Compile-time assembly metadata constants without reflection                                                                 |
| [AutoMapper Generator](auto-mapper.md)                          | Compile-time property-to-property mapping method generation                                                                 |
| [Builder Generator](builder.md)                                 | Fluent builder pattern generation for classes                                                                               |
| [Cloneable Generator](cloneable.md)                             | Compile-time `Clone()` and `DeepClone()` method generation with collection and struct support                               |
| [Decorator Generator](decorator.md)                             | Compile-time decorator pattern generation with full interface delegation, generic methods, events, and partial method hooks |
| [Disposable Generator](disposable.md)                           | Compile-time `IDisposable` / `IAsyncDisposable` pattern generation with ordered resource cleanup                            |
| [Enumerator Extensions Generator](enumerator-extensions.md)     | Fast, allocation-free extension methods for enums                                                                           |
| [Equality Generator](equality.md)                               | Compile-time `Equals`, `GetHashCode`, and operator generation                                                               |
| [Factory Generator](factory.md)                                 | Compile-time factory pattern generation with automatic implementation discovery                                             |
| [INI File Generator](ini-file.md)                               | Compile-time INI file serialization and deserialization                                                                     |
| [Notification Properties Generator](notification-properties.md) | Configurable `INotifyPropertyChanged`/`INotifyPropertyChanging` implementation                                              |
| [Singleton Generator](singleton.md)                             | Thread-safe singleton pattern generation with lazy or eager initialization                                                  |
| [ToString Generator](to-string.md)                              | Compile-time `ToString()` override generation                                                                               |
| [Validator Generator](validator.md)                             | Compile-time data annotation validation                                                                                     |

## Contributors

- [Developer Reference](developer-reference.md) - internal helpers used by the generators
