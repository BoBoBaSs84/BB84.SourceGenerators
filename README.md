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

This package provides eight powerful source generators:

- **Enumerator Extensions Generator** - Fast, allocation-free extension methods for enums
- **Notification Properties Generator** - Automatic INotifyPropertyChanged/INotifyPropertyChanging implementation
- **Abstraction Generator** - Interface and implementation generation for static classes
- **INI File Generator** - Compile-time INI file serialization and deserialization
- **Builder Generator** - Fluent builder pattern generation for classes
- **ToString Generator** - Compile-time `ToString()` override generation
- **Validator Generator** - Compile-time data annotation validation
- **Equality Generator** - Compile-time `Equals`, `GetHashCode`, and operator generation

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
{ }

public partial interface IFileProvider
{ }
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

### 4. INI File Generator

Generates static `Read` and `Write` methods for classes, enabling compile-time INI file serialization and deserialization based on decorated properties.

#### Attributes

```csharp
[GenerateIniFile(StringComparison stringComparison = StringComparison.OrdinalIgnoreCase, string sectionDelimiter = ".")]
[GenerateIniFileSection(string? name = null)]
[GenerateIniFileValue(string? name = null)]
```

**Parameters:**

- `GenerateIniFile` - Marks a class for INI file code generation. The optional `stringComparison` parameter controls how section and key names are compared during deserialization (default: `OrdinalIgnoreCase`). The optional `sectionDelimiter` parameter specifies the delimiter for nested section names (default: `"."`). The optional `SerializeComments` property, when set to `true`, includes XML documentation `<summary>` comments from section and value properties as INI comment lines (prefixed with `; `) in the generated `Write` output (default: `false`)
- `GenerateIniFileSection` - Marks a property as an INI file section. The optional `name` parameter specifies the section name; if omitted, the property name is used. Can also be applied to properties within section types to create nested sections
- `GenerateIniFileValue` - Marks a property as a key-value pair within an INI section. The optional `name` parameter specifies the key name; if omitted, the property name is used

**Supported Value Types:**
`string`, `int`, `long`, `float`, `double`, `bool`, `decimal`, `DateTime`

#### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateIniFile]
public partial class AppConfig
{
    [GenerateIniFileSection("General")]
    public GeneralSection General { get; set; }

    [GenerateIniFileSection("Database")]
    public DatabaseSection Database { get; set; }
}

public class GeneralSection
{
    [GenerateIniFileValue("AppName")]
    public string AppName { get; set; }

    [GenerateIniFileValue("Version")]
    public int Version { get; set; }

    [GenerateIniFileValue("Debug")]
    public bool Debug { get; set; }
}

public class DatabaseSection
{
    [GenerateIniFileValue("Server")]
    public string Server { get; set; }

    [GenerateIniFileValue("Port")]
    public int Port { get; set; }

    [GenerateIniFileValue("Timeout")]
    public double Timeout { get; set; }
}
```

#### Generated Methods

The generator creates the following static methods on the decorated class:

- `Read(string content)` - Parses an INI file string and returns a deserialized instance
- `Write(TClass instance)` - Serializes an instance into an INI file string

#### Usage Example

```csharp
// Reading an INI file
string iniContent = File.ReadAllText("config.ini");
AppConfig config = AppConfig.Read(iniContent);

Console.WriteLine(config.General.AppName); // "MyApp"
Console.WriteLine(config.Database.Port);   // 5432

// Modifying and writing back
config.General.Debug = false;
config.Database.Timeout = 60.0;

string output = AppConfig.Write(config);
File.WriteAllText("config.ini", output);
```

**Output:**

```ini
[General]
AppName=MyApp
Version=1
Debug=False

[Database]
Server=localhost
Port=5432
Timeout=60
```

**Case-Sensitive Matching:**

By default, section and key names are compared case-insensitively (`OrdinalIgnoreCase`). To use case-sensitive matching:

```csharp
[GenerateIniFile(StringComparison.Ordinal)]
public partial class StrictConfig
{
    [GenerateIniFileSection("General")]
    public GeneralSection General { get; set; }
}
```

**Nested Sections:**

The generator supports nested sections by applying `[GenerateIniFileSection]` to properties within section types. Nested sections are represented with dotted names (e.g., `[Server.Database]`), and nesting is supported up to 8 levels deep:

```csharp
[GenerateIniFile]
public partial class Config
{
    [GenerateIniFileSection]
    public ServerSection Server { get; set; }
}

public class ServerSection
{
    [GenerateIniFileValue]
    public string Host { get; set; }

    [GenerateIniFileSection]
    public DatabaseSection Database { get; set; }
}

public class DatabaseSection
{
    [GenerateIniFileValue]
    public int Port { get; set; }
}
```

This produces:

```ini
[Server]
Host=localhost

[Server.Database]
Port=5432
```

To use a custom delimiter (e.g., `/`):

```csharp
[GenerateIniFile(sectionDelimiter: "/")]
public partial class Config { ... }
// Produces: [Server/Database]
```

**Serializing Comments:**

When `SerializeComments` is set to `true`, XML documentation `<summary>` comments on section and value properties are emitted as INI comment lines (prefixed with `; `) in the `Write` output. The `Read` method automatically skips these comment lines during deserialization:

```csharp
[GenerateIniFile(SerializeComments = true)]
public partial class AppConfig
{
    /// <summary>
    /// General application settings
    /// </summary>
    [GenerateIniFileSection]
    public GeneralSection General { get; set; }
}

public class GeneralSection
{
    /// <summary>
    /// The display name of the application
    /// </summary>
    [GenerateIniFileValue]
    public string AppName { get; set; }

    /// <summary>
    /// The current version number
    /// </summary>
    [GenerateIniFileValue]
    public int Version { get; set; }
}
```

This produces:

```ini
; General application settings
[General]
; The display name of the application
AppName=MyApp
; The current version number
Version=1
```

### 5. Builder Generator

Generates a fluent builder class for classes, providing `With{PropertyName}(value)` methods for each public settable property and a `Build()` method that creates the instance.

#### Attribute

```csharp
[GenerateBuilder]
```

#### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateBuilder]
public partial class UserProfile
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
}
```

#### Generated Code

The generator creates a `{ClassName}Builder` class with:

- A `With{PropertyName}(value)` fluent method for each public settable property
- A `Build()` method that creates the instance via object initializer
- Proper nullable reference type annotations
- XML documentation comments

#### Usage Example

```csharp
// Create an instance using the fluent builder
UserProfile profile = new UserProfileBuilder()
    .WithId(1)
    .WithName("John Doe")
    .WithEmail("john@example.com")
    .WithAge(30)
    .WithIsActive(true)
    .Build();

// Only set the properties you need - others use default values
UserProfile minimal = new UserProfileBuilder()
    .WithName("Jane Doe")
    .Build();

// Builders can be reused to create multiple instances
var builder = new UserProfileBuilder()
    .WithName("Template User")
    .WithIsActive(true);

UserProfile first = builder.WithId(1).Build();
UserProfile second = builder.WithId(2).Build();
```

### 6. ToString Generator

Generates a `ToString()` override for classes, returning a formatted string containing the class name and all (or selected) public readable property values in the format `ClassName { Prop1 = val1, Prop2 = val2 }`.

#### Attribute

```csharp
[GenerateToString(params string[] excludeProperties)]
```

**Parameters:**

- `excludeProperties` - Optional list of property names to exclude from the generated `ToString()` output

#### Example

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
```

#### Generated Code

The generator creates a `ToString()` override on the partial class that:

- Includes all public readable properties by default
- Excludes properties specified in the attribute parameter
- Formats output as `ClassName { Prop1 = val1, Prop2 = val2 }`
- Returns `ClassName { }` when all properties are excluded

#### Usage Example

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
```

### 7. Validator Generator

Generates a `Validate()` method for classes, scanning properties for data annotation attributes at compile time and emitting direct validation checks. This replaces runtime reflection-based `Validator.TryValidateObject()` with zero-overhead generated code.

#### Attribute

```csharp
[GenerateValidator]
```

**Supported Data Annotations:**

- `[Required]` - Validates that the property is not null (or not null/empty for strings)
- `[Range(min, max)]` - Validates that a numeric value falls within a specified range, or that a collection has between min and max elements
- `[StringLength(max, MinimumLength = min)]` - Validates string length within bounds
- `[MinLength(length)]` - Validates minimum length of a string or collection
- `[MaxLength(length)]` - Validates maximum length of a string or collection
- `[RegularExpression(pattern)]` - Validates that a string matches a regex pattern

#### Example

```csharp
using System.ComponentModel.DataAnnotations;
using BB84.SourceGenerators.Attributes;

[GenerateValidator]
public partial class UserRegistration
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }

    [Required]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    public string? Email { get; set; }

    [Range(18, 120)]
    public int Age { get; set; }

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string? Password { get; set; }

		[Range(1, 10)]
		public List<int>? Skills { get; set; }
}
```

#### Generated Code

The generator creates a `Validate()` method on the partial class that:

- Returns a `List<string>` of validation error messages
- Contains direct `if`-checks for each data annotation rule
- Supports custom error messages via `ErrorMessage` property
- Returns an empty list when the instance is valid

#### Usage Example

```csharp
var registration = new UserRegistration
{
    Name = "J",
    Email = "not-an-email",
    Age = 15,
    Password = "short"
};

List<string> errors = registration.Validate();

if (errors.Count > 0)
{
    foreach (string error in errors)
    {
        Console.WriteLine(error);
    }
    // Output:
    // The field Name must be a string with a minimum length of 2 and a maximum length of 100.
    // The field Email must match the regular expression '^[^@\s]+@[^@\s]+\.[^@\s]+$'.
    // The field Age must be between 18 and 120.
    // The field Password must be a string or collection with a minimum length of 8.
}
else
{
    Console.WriteLine("Registration is valid!");
}

// Custom error messages
[GenerateValidator]
public partial class LoginModel
{
    [Required(ErrorMessage = "Username is required.")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Password cannot be empty.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string? Password { get; set; }
}
```

### 8. Equality Generator

Generates `Equals(object)`, `Equals(T)`, `GetHashCode()`, `operator ==`, and `operator !=` for classes, implementing `IEquatable<T>`. This replaces tedious and error-prone manual equality implementations with zero-overhead generated code.

#### Attribute

```csharp
[GenerateEquality(params string[] excludeProperties)]
```

**Parameters:**

- `excludeProperties` - Optional list of property names to exclude from the generated equality comparison

#### Example

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

#### Generated Code

The generator creates the following members on the partial class:

- `Equals(object?)` override — delegates to the typed `Equals(T?)` method
- `Equals(T?)` implementing `IEquatable<T>` — compares all (or selected) public properties
- `GetHashCode()` override — produces a consistent hash from all (or selected) public properties
- `operator ==` and `operator !=` — delegates to `Equals`

#### Usage Example

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

### INI File Serialization

- Generates direct string parsing and formatting code at compile time
- Avoids runtime reflection or third-party INI parsing libraries
- Uses `CultureInfo.InvariantCulture` for consistent cross-platform formatting

### Builder Pattern

- Generates a complete fluent builder class at compile time
- Eliminates hand-written builder boilerplate that must be kept in sync with the target class
- Replaces reflection-based or expression-tree-based builder libraries with zero-overhead generated code
- Full nullable reference type support for type-safe builder APIs

### ToString Generation

- Generates a direct property-formatting `ToString()` override at compile time
- Replaces runtime reflection approaches (`typeof(T).GetProperties().Select(...)`) with zero-overhead generated code
- Automatically stays in sync with the class definition - no manual maintenance required
- Supports property exclusion for sensitive or verbose fields

### Validation

- Generates direct `if`-checks at compile time for each data annotation rule
- Replaces `Validator.TryValidateObject()` which uses `TypeDescriptor` and reflection at runtime
- Provides compile-time discovery of validation attributes - no runtime attribute scanning
- Supports custom error messages for localization and user-friendly feedback

### Equality

- Generates correct `Equals`, `GetHashCode`, `==`, and `!=` implementations at compile time
- Eliminates tedious and error-prone manual equality boilerplate
- Replaces runtime reflection approaches with zero-overhead generated code
- Properly handles null references and value type properties
- Supports property exclusion for volatile or non-significant fields

## How It Works

Source generators run during compilation and generate additional C# source files that are compiled alongside your code. This means:

- **Zero runtime overhead** - All code is generated at compile time
- **IntelliSense support** - Generated code appears in Visual Studio IntelliSense
- **Debuggable** - You can step through generated code during debugging
- **No reflection** - Generated code uses direct method calls

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request but see the [Conduct](CODE_OF_CONDUCT.md) first.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Robert Peter Meyer (BoBoBaSs84)**

- GitHub: [@BoBoBaSs84](https://github.com/BoBoBaSs84)
- Repository: [BB84.SourceGenerators](https://github.com/BoBoBaSs84/BB84.SourceGenerators)

## Changelog

See [releases](https://github.com/BoBoBaSs84/BB84.SourceGenerators/releases) for version history and changelog.
