# INI File Generator

Generates static `Read` and `Write` methods and an instance `Load` method for classes, enabling compile-time INI file serialization, deserialization, and instance-to-instance value copying based on decorated properties.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

INI files are still common for application and tool configuration, but .NET has no built-in, strongly-typed INI serializer. The alternatives are hand-written parsers or third-party libraries that use reflection at runtime. This generator emits the parse and write code for your configuration classes at compile time, with typed values, nested sections, collections, comments and async I/O.

## Benefits

- Generates direct string parsing and formatting code at compile time
- Avoids runtime reflection or third-party INI parsing libraries
- Uses `CultureInfo.InvariantCulture` for consistent cross-platform formatting
- Supports `Guid`, `TimeSpan`, `DateTimeOffset`, and collection types (`List<T>`, `T[]`)
- Provides `TryRead` for safe parsing without exceptions
- Provides async `ReadAsync`/`WriteAsync` for non-blocking I/O with file path (line-by-line via `ReadLineAsync`), `Stream`, and `TextReader`/`TextWriter`

## Attributes

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
`string`, `int`, `long`, `float`, `double`, `bool`, `decimal`, `DateTime`, `Guid`, `TimeSpan`, `DateTimeOffset`

**Collection Support:**
`List<T>` and `T[]` are supported for any of the above value types. Values are serialized and deserialized as comma-separated strings.

## Usage

### Example

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
    public string ApplicationName { get; set; }

    [GenerateIniFileValue("Version")]
    public int ApplicationVersion { get; set; }

    [GenerateIniFileValue("Debug")]
    public bool IsDebug { get; set; }
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

### Generated Methods

The generator creates the following methods on the decorated class:

- `Read(string content)` - Static method that parses an INI file string and returns a deserialized instance
- `TryRead(string content, out T? result)` - Static method that attempts to parse an INI file string, returning `true` on success or `false` on failure without throwing exceptions
- `Write(TClass instance)` - Static method that serializes an instance into an INI file string
- `ReadAsync(string filePath)` - Static async method that reads a file line-by-line via `ReadLineAsync` and deserializes INI content; truly non-blocking I/O
- `ReadAsync(TextReader reader)` - Static async method that reads and deserializes INI content from a `TextReader`
- `ReadAsync(Stream stream)` - Static async method that reads and deserializes INI content from a `Stream`
- `WriteAsync(TClass instance, TextWriter writer)` - Static async method that serializes an instance and writes it to a `TextWriter`
- `WriteAsync(TClass instance, Stream stream)` - Static async method that serializes an instance and writes it to a `Stream`
- `Load(TClass other)` - Instance method that copies all section/value properties from another instance into this instance (null-safe per section)

### Usage Example

```csharp
// Reading an INI file
string iniContent = File.ReadAllText("config.ini");
AppConfig config = AppConfig.Read(iniContent);

Console.WriteLine(config.General.ApplicationName); // "MyApp"
Console.WriteLine(config.Database.Port);   // 5432

// Modifying and writing back
config.General.Debug = false;
config.Database.Timeout = 60.0;

string output = AppConfig.Write(config);
File.WriteAllText("config.ini", output);

// Loading values from another instance
string fileContent = File.ReadAllText("updated.ini");
AppConfig newConfig = AppConfig.Read(fileContent);
config.Load(newConfig); // copies all section values from newConfig into config

// Safe parsing with TryRead (no exceptions)
if (AppConfig.TryRead(iniContent, out AppConfig? parsed))
{
    Console.WriteLine(parsed.General.ApplicationName);
}
else
{
    Console.WriteLine("Failed to parse INI content");
}

// Async reading from a file path (line-by-line, truly non-blocking)
AppConfig asyncConfig = await AppConfig.ReadAsync("config.ini");

// Async reading from a stream
using FileStream fs = File.OpenRead("config.ini");
AppConfig asyncConfigFromStream = await AppConfig.ReadAsync(fs);

// Async writing to a stream
using FileStream outFs = File.Create("output.ini");
await AppConfig.WriteAsync(asyncConfig, outFs);
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

**Enumeration Support:**

Regular enums will be serialized as their string name. Enum flags will be serialized as a space-delimited string of individual flag names.

```csharp
public enum LogLevel { Debug, Info, Warning, Error }

[Flags]
public enum Permissions { None = 0, Read = 1, Write = 2, Execute = 4 }

public class AppSection
{
    [GenerateIniFileValue]
    public LogLevel Level { get; set; }

    [GenerateIniFileValue]
    public Permissions Perms { get; set; }
}
```

This produces:

```ini
[App]
Level=Warning
Perms=Read Write Execute
```

**Collection Support:**

`List<T>` and `T[]` properties are serialized as comma-separated values:

```csharp
public class SettingsSection
{
    [GenerateIniFileValue]
    public List<string>? Tags { get; set; }

    [GenerateIniFileValue]
    public int[]? Ports { get; set; }

    [GenerateIniFileValue]
    public List<Guid>? Identifiers { get; set; }
}
```

This produces:

```ini
[Settings]
Tags=web,api,backend
Ports=8080,8443,9090
Identifiers=d3b07384-d9a0-4e9b-8c12-f7a8b2c1d3e5,a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

## Diagnostics

| ID           | Severity | Description                                        |
| ------------ | -------- | -------------------------------------------------- |
| `BB84SG0011` | Error    | IniFile section property must not be nullable      |
| `BB84SG0012` | Error    | IniFile section property must be initialized       |
| `BB84SG0013` | Error    | IniFile section property must have a public getter |
