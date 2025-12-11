# BB84.SourceGenerators

A collection of C# source generators that automatically generate boilerplate code at compile time, reducing manual coding and improving code maintainability.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

## Features

This package provides four powerful source generators:

- **Enumerator Extensions Generator** - Fast, allocation-free extension methods for enums
- **Notification Properties Generator** - Automatic INotifyPropertyChanged/INotifyPropertyChanging implementation
- **Abstraction Generator** - Interface and implementation generation for static classes

## Installation

Install the package via NuGet:

```bash
dotnet add package BB84.SourceGenerators
```

Or via Package Manager Console:

```powershell
Install-Package BB84.SourceGenerators
```

## Usage

### 1. Enumerator Extensions Generator

Generates high-performance extension methods for enumerations, providing faster alternatives to `Enum.ToString()`, `Enum.IsDefined()`, `Enum.GetNames()`, and `Enum.GetValues()`.

#### Attribute

```csharp
[GenerateEnumeratorExtensions]
```

#### Example

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

#### Generated Methods

The generator creates the following extension methods:

- `ToStringFast()` - Returns the name of the enum value as a string
- `IsDefinedFast(this TEnum value)` - Checks if an enum value is defined
- `IsDefinedFast(this string name)` - Checks if an enum name is defined
- `GetNamesFast()` - Returns all enum names as an `IEnumerable<string>`
- `GetValuesFast()` - Returns all enum values as an `IEnumerable<TEnum>`
- `GetDescriptionFast()` - Returns the description from `[Description]` attribute, or the name if not present

#### Usage Example

```csharp
var status = Status.Pending;

// Fast string conversion
string name = status.ToStringFast(); // "Pending"

// Check if defined
bool isDefined = status.IsDefinedFast(); // true
bool isNameDefined = "Active".IsDefinedFast(); // true

// Get description
string description = status.GetDescriptionFast(); // "Pending approval"

// Get all names and values
IEnumerable<string> names = status.GetNamesFast();
IEnumerable<Status> values = status.GetValuesFast();
```

### 2. Notification Properties Generator

Automatically generates properties with `INotifyPropertyChanged` and `INotifyPropertyChanging` support for private fields in a class.

#### Attribute

```csharp
[GenerateNotifications(bool isChanged = false)]
```

**Parameters:**
- `isChanged` - When `true`, generates an additional `IsChanged` boolean property that is set to `true` when any property changes

#### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateNotifications(isChanged: true)]
public partial class Person
{
    private int _id;
    private string _name;
    private string _email;
    private DateTime _createdAt;
    private DateTime? _updatedAt;

    public Person(int id, string name, string email)
    {
        _id = id;
        _name = name;
        _email = email;
        _createdAt = DateTime.UtcNow;
    }
}
```

#### Generated Code

The generator creates:

- Public properties for each private field with change notification
- Implementation of `INotifyPropertyChanged` and `INotifyPropertyChanging` interfaces
- `PropertyChanged` and `PropertyChanging` events
- `RaisePropertyChanged()` and `RaisePropertyChanging()` protected virtual methods
- Optional `IsChanged` property (when `isChanged` parameter is `true`)

#### Usage Example

```csharp
var person = new Person(1, "John Doe", "john@example.com");

// Subscribe to change notifications
person.PropertyChanging += (s, e) => Console.WriteLine($"Property {e.PropertyName} is changing");
person.PropertyChanged += (s, e) => Console.WriteLine($"Property {e.PropertyName} has changed");

// Change a property - events will fire automatically
person.Name = "Jane Doe";
person.Email = "jane@example.com";

// Check if object has been modified (when isChanged: true)
if (person.IsChanged)
{
    Console.WriteLine("Person has been modified");
}
```

### 3. Abstraction Generator

Generates interface and implementation classes for static classes, making them testable through dependency injection.

#### Attribute

```csharp
[GenerateAbstraction(Type targetType, Type abstractionType, Type implementationType, params string[] excludeMethods)]
```

**Parameters:**
- `targetType` - The static class to generate an abstraction for
- `abstractionType` - The interface type to generate
- `implementationType` - The implementation class type to generate
- `excludeMethods` - Optional array of method names to exclude from generation

#### Example

```csharp
using BB84.SourceGenerators.Attributes;

// Generate abstraction for System.IO.File
[GenerateAbstraction(typeof(File), typeof(IFileProvider), typeof(FileProvider))]
public partial class FileProvider
{
}

public partial interface IFileProvider
{
}
```

#### Generated Code

The generator creates:

- A complete interface (`IFileProvider`) with all public static methods from the target type
- An implementation class (`FileProvider`) that implements the interface and delegates to the static class
- XML documentation comments using `<inheritdoc>`

#### Usage Example

```csharp
// In your service
public class DocumentService
{
    private readonly IFileProvider _fileProvider;

    public DocumentService(IFileProvider fileProvider)
    {
        _fileProvider = fileProvider;
    }

    public string ReadDocument(string path)
    {
        return _fileProvider.ReadAllText(path);
    }
}

// In your DI container setup
services.AddSingleton<IFileProvider, FileProvider>();

// In tests, you can mock IFileProvider
var mockFileProvider = new Mock<IFileProvider>();
mockFileProvider.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("test content");
```

#### Excluding Methods

```csharp
// Exclude specific methods from generation
[GenerateAbstraction(
    typeof(File), 
    typeof(IFileProvider), 
    typeof(FileProvider),
    "Delete", "Move", "Replace")]
public partial class FileProvider
{
}
```

## Requirements

- .NET Standard 2.0 or higher
- C# 7.3 or higher
- Supports .NET Framework 4.7.2+, .NET Core 2.0+, .NET 5+, .NET 6+, .NET 7+, .NET 8+

## Performance Benefits

### Enum Extensions
The generated enum extension methods provide significant performance improvements over reflection-based `Enum` methods:

- **ToStringFast()** - Avoids boxing and uses switch expressions
- **IsDefinedFast()** - Compile-time switch instead of runtime reflection
- **GetNamesFast()/GetValuesFast()** - Returns pre-allocated arrays instead of reflection

### Notification Properties
- Generates optimized property setters with inline equality checks
- Avoids reflection overhead of PropertyChanged.Fody or similar tools
- Compile-time code generation means zero runtime overhead

## How It Works

Source generators run during compilation and generate additional C# source files that are compiled alongside your code. This means:

- **Zero runtime overhead** - All code is generated at compile time
- **IntelliSense support** - Generated code appears in Visual Studio IntelliSense
- **Debuggable** - You can step through generated code during debugging
- **No reflection** - Generated code uses direct method calls

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Robert Peter Meyer (BoBoBaSs84)**

- GitHub: [@BoBoBaSs84](https://github.com/BoBoBaSs84)
- Repository: [BB84.SourceGenerators](https://github.com/BoBoBaSs84/BB84.SourceGenerators)

## Changelog

See [releases](https://github.com/BoBoBaSs84/BB84.SourceGenerators/releases) for version history and changelog.
