# Environment Variables Generator

Generates static `Read` and `TryRead` methods that fill the public properties of a class or record class from environment variables named `<Prefix>_<PROPERTY_NAME>`, converting each value to the property type. Partial properties are implemented to read (and optionally write) their environment variable on every access.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

Apps running in containers, CI pipelines or as services are often configured through environment variables. Reading them by hand means repeating `Environment.GetEnvironmentVariable("MYAPP_TOKEN")` for every setting, plus `null` checks and type conversion, and the variable names are string literals that can drift away from the properties they fill. Configuration libraries such as `Microsoft.Extensions.Configuration.EnvironmentVariables` solve this with a runtime dependency, reflection and more setup than a small tool needs.

## Benefits

- Generates direct `Environment.GetEnvironmentVariable` calls and `TryParse` conversions at compile time - no reflection, no runtime dependency
- Variable names are derived from the property names, so they cannot drift apart
- Reports **all** missing and invalid variables at once instead of failing on the first one
- Error messages never contain the variable values, which may be secrets
- A `Func<string, string?>` lookup overload makes the type testable without touching the process environment
- Property initializers act as fallback values; nullable properties are optional
- Numbers and dates are parsed with the invariant culture, independent of the machine

## Attributes

```csharp
[GenerateEnvironmentVariables(string prefix)]
[EnvironmentVariableName(string name)]  // per-property
```

**Parameters:**

- `prefix` - The prefix of the variable names, for example `"MyApp"`. Required; it must not be empty or contain `=`.
- `Separator` - The separator between the prefix and the property name (named argument, default: `"_"`)
- `NamingStyle` - How the variable names are built (named argument, default: `EnvironmentVariableNamingStyle.UpperSnakeCase`)
- `ExcludeProperties` - The names of the properties that are not read (named argument, default: none)
- `IncludeInherited` - When `true`, the public properties of the base classes are read as well (named argument, default: `false`)
- `Writable` - When `true`, the setters of partial properties write the environment variable of the current process (named argument, default: `false`)

**Naming styles:**

| Style                        | Prefix `MyApp`, property `ConnectTimeoutMs` |
| ---------------------------- | ------------------------------------------- |
| `UpperSnakeCase` _(default)_ | `MYAPP_CONNECT_TIMEOUT_MS`                  |
| `AsIs`                       | `MyApp_ConnectTimeoutMs`                    |

`[EnvironmentVariableName("ENCRYPT")]` sets the full variable name of a property. The prefix, the separator and the naming style are not applied.

**Rules:**

| Property                                            | Missing or empty variable        |
| --------------------------------------------------- | -------------------------------- |
| Non-nullable, no initializer (`int Port`)           | Error (mandatory)                |
| Nullable (`int? Port`, `string? User`)              | `null`                           |
| With initializer (`int Port { get; init; } = 1433`) | The initializer value (fallback) |

- Empty and whitespace-only values count as missing.
- A value that cannot be converted is always an error, even for optional properties.
- Properties need an `init` or `set` accessor that the type can access, because the generated code uses an object initializer. `required` properties are supported. Computed properties (`=> ...` or a get accessor with a body) are skipped; get-only auto-properties are an error.
- An init-only or `required` property with an initializer needs a **constant** initializer (it is copied into the generated code). A property with a `set` accessor may use any initializer: it is only assigned when the variable is set.
- On abstract types, no `Read`/`TryRead` methods are generated (the type cannot be created); only partial properties are implemented.

**Supported types:** `string`, `bool` (`true`/`false` in any casing, `1`/`0`), `char`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, enums (member names in any casing or defined numeric values; `[Flags]` enums accept `A, B`) and the nullable versions of these types.

## Usage

### Reading settings

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateEnvironmentVariables("MyApp", ExcludeProperties = [nameof(Description)])]
public sealed partial record AppSettings
{
	public required string Server { get; init; }          // MYAPP_SERVER, mandatory
	public string? User { get; init; }                    // MYAPP_USER, optional
	public int Port { get; init; } = 1433;                // MYAPP_PORT, falls back to 1433

	[EnvironmentVariableName("ENCRYPT")]
	public bool Encrypt { get; init; } = true;            // ENCRYPT

	public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30); // MYAPP_TIMEOUT

	public string? Description { get; set; }               // excluded

	public string ConnectionString => $"Server={Server},{Port}";  // computed, skipped
}

// Throws an InvalidOperationException listing every missing or invalid variable
AppSettings settings = AppSettings.Read();

// Or without exceptions
if (!AppSettings.TryRead(out AppSettings? result, out IReadOnlyList<string> errors))
{
	foreach (string error in errors)
		Console.Error.WriteLine(error);   // e.g. "Missing required environment variable 'MYAPP_SERVER'."
}

// In tests, read from a dictionary instead of the process environment
Dictionary<string, string> variables = new() { ["MYAPP_SERVER"] = "localhost" };
AppSettings fromTest = AppSettings.Read(name => variables.TryGetValue(name, out string? value) ? value : null);
```

### Generated code

```csharp
public sealed partial record AppSettings
{
	public static AppSettings Read() => Read(Environment.GetEnvironmentVariable);
	public static AppSettings Read(Func<string, string?> lookup) { /* throws with all errors */ }
	public static bool TryRead(out AppSettings? result, out IReadOnlyList<string> errors) => TryRead(Environment.GetEnvironmentVariable, out result, out errors);

	public static bool TryRead(Func<string, string?> lookup, out AppSettings? result, out IReadOnlyList<string> errors)
	{
		List<string> errorList = new List<string>();

		string? portRaw = lookup("MYAPP_PORT");
		int portValue = 1433;
		if (!string.IsNullOrWhiteSpace(portRaw))
		{
			if (!int.TryParse(portRaw!, NumberStyles.Integer, CultureInfo.InvariantCulture, out int portParsed))
				errorList.Add("Environment variable 'MYAPP_PORT' has an invalid Int32 value.");
			else
				portValue = portParsed;
		}
		// ... one block per property

		if (errorList.Count > 0)
		{
			result = null;
			errors = errorList;
			return false;
		}

		result = new AppSettings { Server = serverValue, User = userValue, Port = portValue, Encrypt = encryptValue };
		if (timeoutFound)
			result.Timeout = timeoutValue;

		errors = Array.Empty<string>();
		return true;
	}
}
```

### Inheritance

```csharp
public abstract record DatabaseSettings
{
	public string? Server { get; init; }
}

[GenerateEnvironmentVariables("MyApp", IncludeInherited = true)]
public sealed partial record ExporterSettings : DatabaseSettings
{
	public int? Port { get; init; }   // MYAPP_PORT; Server is read from MYAPP_SERVER
}
```

Inherited properties need an `init` or `set` accessor the derived type can access (`public`, `protected` or `internal`).

### Partial properties

Partial properties (C# 13) are implemented to read the variable on every access. With `Writable = true`, their setters write the variable of the current process (`null` removes it):

```csharp
[GenerateEnvironmentVariables("MyApp", Writable = true)]
public static partial class RuntimeSettings
{
	public static partial string? LogLevel { get; set; }   // MYAPP_LOG_LEVEL
	public static partial int Port { get; }                // throws when missing or invalid
}

RuntimeSettings.LogLevel = "Debug";   // Environment.SetEnvironmentVariable("MYAPP_LOG_LEVEL", "Debug")
```

A non-nullable partial property throws an `InvalidOperationException` when the variable is missing; a nullable one returns `null`. Partial properties are not part of `Read`/`TryRead`.

## Diagnostics

| ID           | Severity | Description                                                                |
| ------------ | -------- | -------------------------------------------------------------------------- |
| `BB84SG0026` | Error    | GenerateEnvironmentVariables requires a partial class or record class      |
| `BB84SG0027` | Error    | The prefix or a variable name is empty or contains `=`                     |
| `BB84SG0028` | Error    | The property type cannot be read from an environment variable              |
| `BB84SG0029` | Error    | The property has no `init` or `set` accessor the type can access           |
| `BB84SG0030` | Error    | The type has no parameterless constructor (e.g. a positional record)       |
| `BB84SG0031` | Error    | Two properties map to the same variable name (compared case-insensitively) |
| `BB84SG0032` | Error    | A partial property declares a setter, but `Writable` is not `true`         |
| `BB84SG0033` | Error    | A required member is excluded or not read                                  |
| `BB84SG0034` | Error    | An init-only or required property has a non-constant initializer           |
