# BB84.SourceGenerators

A collection of C# source generators that automatically generate boilerplate code at compile time, reducing manual coding and improving code maintainability.

[![CI](https://github.com/BoBoBaSs84/BB84.SourceGenerators/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/BoBoBaSs84/BB84.SourceGenerators/actions/workflows/ci.yml)
[![CD](https://github.com/BoBoBaSs84/BB84.SourceGenerators/actions/workflows/cd.yml/badge.svg?event=push)](https://github.com/BoBoBaSs84/BB84.SourceGenerators/actions/workflows/cd.yml)
[![CodeQL](https://github.com/BoBoBaSs84/BB84.SourceGenerators/actions/workflows/github-code-scanning/codeql/badge.svg?branch=main)](https://github.com/BoBoBaSs84/BB84.SourceGenerators/actions/workflows/github-code-scanning/codeql)
[![Dependabot](https://github.com/BoBoBaSs84/BB84.SourceGenerators/actions/workflows/dependabot/dependabot-updates/badge.svg?branch=main)](https://github.com/BoBoBaSs84/BB84.SourceGenerators/actions/workflows/dependabot/dependabot-updates)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![C#](https://img.shields.io/badge/C%23-13.0-239120)](https://github.com/BoBoBaSs84/BB84.SourceGenerators)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-5C2D91)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![Issues](https://img.shields.io/github/issues/BoBoBaSs84/BB84.SourceGenerators)](https://github.com/BoBoBaSs84/BB84.SourceGenerators/issues)
[![LastCommit](https://img.shields.io/github/last-commit/BoBoBaSs84/BB84.SourceGenerators)](https://github.com/BoBoBaSs84/BB84.SourceGenerators/commit/main)
[![PullRequests](https://img.shields.io/github/issues-pr/BoBoBaSs84/BB84.SourceGenerators)](https://github.com/BoBoBaSs84/BB84.SourceGenerators/pulls)
[![RepoSize](https://img.shields.io/github/repo-size/BoBoBaSs84/BB84.SourceGenerators)](https://github.com/BoBoBaSs84/BB84.SourceGenerators)
[![NuGet](https://img.shields.io/nuget/v/BB84.SourceGenerators.svg?logo=nuget&logoColor=white)](https://www.nuget.org/packages/BB84.SourceGenerators)

## Features

This package provides sixteen powerful source generators:

- **[Abstraction Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/abstraction.md)** - Interface and implementation generation for static classes
- **[ActionCommand Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/action-command.md)** - Compile-time `ICommand` property generation (sync, async, parameterized, cancellable) for MVVM
- **[AutoMapper Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/auto-mapper.md)** - Compile-time property-to-property mapping method generation
- **[Assembly Information Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/assembly-information.md)** - Compile-time assembly metadata constants without reflection
- **[Builder Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/builder.md)** - Fluent builder pattern generation for classes
- **[Cloneable Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/cloneable.md)** - Compile-time `Clone()` and `DeepClone()` method generation with collection and struct support
- **[Decorator Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/decorator.md)** - Compile-time decorator pattern generation with full interface delegation, generic methods, events, and partial method hooks
- **[Disposable Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/disposable.md)** - Compile-time `IDisposable` / `IAsyncDisposable` pattern generation with ordered resource cleanup
- **[Enumerator Extensions Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/enumerator-extensions.md)** - Fast, allocation-free extension methods for enums
- **[Equality Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/equality.md)** - Compile-time `Equals`, `GetHashCode`, and operator generation
- **[Factory Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/factory.md)** - Compile-time factory pattern generation with automatic implementation discovery
- **[INI File Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/ini-file.md)** - Compile-time INI file serialization and deserialization
- **[Notification Properties Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/notification-properties.md)** - Configurable `INotifyPropertyChanged`/`INotifyPropertyChanging` implementation
- **[Singleton Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/singleton.md)** - Thread-safe singleton pattern generation with lazy or eager initialization
- **[ToString Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/to-string.md)** - Compile-time `ToString()` override generation
- **[Validator Generator](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/validator.md)** - Compile-time data annotation validation

Each generator has its own page covering why to use it, its benefits and usage examples. See the [documentation index](https://github.com/BoBoBaSs84/BB84.SourceGenerators/blob/main/docs/README.md) for an overview.

## Installation

Install the package via NuGet:

```bash
dotnet add package BB84.SourceGenerators
```

Or via Package Manager Console:

```powershell
Install-Package BB84.SourceGenerators
```

In the project where you want to use the generators, add a reference to the package and include the appropriate `using` directive for the attributes:

```csharp
using BB84.SourceGenerators.Attributes;
```

If the attributes are not recognized, make sure that the `EmitCompilerGeneratedFiles` property is set to `true` in the project file:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

## Requirements

- .NET Standard 2.0 or higher
- C# 7.3 or higher
- Supports .NET Framework 4.7.2+, .NET Core 2.0+, .NET 5+, .NET 6+, .NET 7+, .NET 8+

## How It Works

Source generators run during compilation and generate additional C# source files that are compiled alongside your code. This means:

- **Zero runtime overhead** - All code is generated at compile time
- **IntelliSense support** - Generated code appears in Visual Studio IntelliSense
- **Debuggable** - You can step through generated code during debugging
- **No reflection** - Generated code uses direct method calls

## Contributing

Contributions are welcome! If you have an idea for a new feature, improvement, or bug fix, please follow these steps:

1. Have a look at the [Issues](https://github.com/BoBoBaSs84/BB84.SourceGenerators/issues) to see if your idea has already been discussed.
2. If you want to work on an existing issue, please comment on the issue to let others know you're working on it.
3. Fork the repository and create a new branch for your contribution.
4. Make your changes and commit them with clear and descriptive messages.
5. Push your changes to your forked repository and submit a pull request to the main repository.

## Code of Conduct

We expect all contributors to adhere to the [Code of Conduct](CODE_OF_CONDUCT.md).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Robert Peter Meyer (BoBoBaSs84)**

- GitHub: [@BoBoBaSs84](https://github.com/BoBoBaSs84)
- Repository: [BB84.SourceGenerators](https://github.com/BoBoBaSs84/BB84.SourceGenerators)

## Changelog

See [releases](https://github.com/BoBoBaSs84/BB84.SourceGenerators/releases) for version history and changelog.
