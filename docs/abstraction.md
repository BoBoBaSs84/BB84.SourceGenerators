# Abstraction Generator

Generates interface and implementation classes for static classes, making them testable through dependency injection.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

Static classes such as `System.IO.File` or `System.Environment` cannot be mocked or injected, which makes code that uses them hard to unit test. The usual fix is a hand-written interface plus a wrapper class that forwards every call. This wrapper is tedious to write and quickly falls out of date. This generator creates the interface and the forwarding implementation for you, so static APIs become injectable with one attribute.

## Benefits

- Makes static APIs (e.g. `File`, `Environment`) injectable and mockable
- Generates the interface and the forwarding implementation at compile time
- Stays in sync with the target type - no hand-written wrapper to maintain
- Selective exclusion of methods and properties via `ExcludeMethods` / `ExcludeProperties`
- Generated members carry `<inheritdoc>` documentation

## Attributes

```csharp
[GenerateAbstraction(Type targetType, Type abstractionType, Type implementationType)]
```

**Parameters:**

- `targetType` - The static class to generate an abstraction for
- `abstractionType` - The interface type to generate
- `implementationType` - The implementation class type to generate

**Properties:**

- `ExcludeMethods` - Optional array of method names to exclude from generation
- `ExcludeProperties` - Optional array of property names to exclude from generation

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

// Generate abstraction for System.IO.File
[GenerateAbstraction(typeof(File), typeof(IFileProvider), typeof(FileProvider))]
public partial class FileProvider
{ }

public partial interface IFileProvider
{ }

// Generate abstraction with exclusions
[GenerateAbstraction(typeof(Environment), typeof(IEnvironmentProvider), typeof(EnvironmentProvider),
    ExcludeMethods = ["Exit", "FailFast"],
    ExcludeProperties = ["ExitCode"])]
public partial class EnvironmentProvider
{ }

public partial interface IEnvironmentProvider
{ }
```

### Generated Code

The generator creates:

- A complete interface (`IFileProvider`) with all public static methods from the target type
- An implementation class (`FileProvider`) that implements the interface and delegates to the static class
- XML documentation comments using `<inheritdoc>`

### Usage Example

```csharp
// In your service
public class DocumentService
{
    private readonly IFileProvider _fileProvider;

    public DocumentService(IFileProvider fileProvider)
        => _fileProvider = fileProvider;

    public string ReadDocument(string path)
        => _fileProvider.ReadAllText(path);
}

// In your DI container setup
services.AddSingleton<IFileProvider, FileProvider>();

// In tests, you can mock IFileProvider
var mockFileProvider = new Mock<IFileProvider>();
mockFileProvider.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("test content");
```
