# Assembly Information Generator

Generates compile-time constant properties for assembly metadata (title, version, company, copyright, etc.), eliminating the need for runtime reflection via `Assembly.GetCustomAttribute<T>()`.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

Reading assembly metadata (version, title, copyright) at runtime needs `Assembly.GetCustomAttribute<T>()` and reflection. The values are also not usable where a constant is required. This generator turns the assembly attributes into `const string` members at compile time.

## Benefits

- Generates `const string` properties at compile time for all standard assembly attributes
- Eliminates runtime reflection via `Assembly.GetCustomAttribute<T>()`
- Constants can be used in attribute arguments, switch expressions, and other compile-time contexts
- Automatically stays in sync with project file properties (`<AssemblyTitle>`, `<Version>`, etc.)

## Attributes

```csharp
[GenerateAssemblyInformation]
```

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateAssemblyInformation]
public partial class AppInfo
{ }
```

### Generated Code

The generator creates `public const string` properties for each standard assembly attribute:

- `Title` — from `AssemblyTitleAttribute`
- `Description` — from `AssemblyDescriptionAttribute`
- `Company` — from `AssemblyCompanyAttribute`
- `Product` — from `AssemblyProductAttribute`
- `Copyright` — from `AssemblyCopyrightAttribute`
- `Trademark` — from `AssemblyTrademarkAttribute`
- `Configuration` — from `AssemblyConfigurationAttribute`
- `Version` — from `AssemblyVersionAttribute`
- `FileVersion` — from `AssemblyFileVersionAttribute`
- `InformationalVersion` — from `AssemblyInformationalVersionAttribute`

### Usage Example

```csharp
Console.WriteLine(AppInfo.Title);                // "MyApp"
Console.WriteLine(AppInfo.Version);              // "1.0.0.0"
Console.WriteLine(AppInfo.Company);              // "My Company"
Console.WriteLine(AppInfo.Copyright);            // "Copyright © 2025"
Console.WriteLine(AppInfo.InformationalVersion); // "1.0.0+abc123"
```
