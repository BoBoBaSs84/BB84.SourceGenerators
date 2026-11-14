# Command Argument Generator

Generates a private constructor, static `Parse` and `TryParse` methods and a `Usage` constant for a class or record class, parsing command line arguments of the form `--propertyName value` into its public properties.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

Small tools, spikes and console apps often need a few command line options. Writing them by hand means the same parsing loop every time: a `for` over `args`, a `switch` on the option names, value conversion, checks for missing, repeated and unknown options, and a usage text that has to be kept in sync with the properties manually. Libraries such as `System.CommandLine` solve this with a runtime dependency, reflection and more setup than a small tool needs.

## Benefits

- Generates a plain `for`/`switch` loop and `TryParse` conversions at compile time - no reflection, no runtime dependency
- Option names and the usage text are derived from the properties and their XML `<summary>` comments, so they cannot drift apart
- Reports **all** unknown, repeated, missing and invalid options at once instead of failing on the first one
- Get-only properties are supported: the generated private constructor assigns them, and `Parse` is the only way to create an instance
- Property initializers act as default values; nullable properties are optional
- Numbers and dates are parsed with the invariant culture, independent of the machine

## Attributes

```csharp
[GenerateCommandArgument]
```

The attribute applies to `partial` classes and `partial` record classes.

**Parameters:**

- `ExcludeProperties` - The names of the properties that are not command line options (named argument, default: none)

**Rules:**

| Property                                     | Option                | Usage                 | When the option is missing |
| -------------------------------------------- | --------------------- | --------------------- | -------------------------- |
| Non-nullable, no initializer (`string Path`) | `--path value`        | `--path <path>`       | Error (mandatory)          |
| Nullable (`int? Lines`, `string? User`)      | `--lines value`       | `--lines [lines]`     | `null`                     |
| With constant initializer (`int Parts = 10`) | `--parts value`       | `--parts [parts]`     | The initializer (default)  |
| Non-nullable `bool` (`bool Verbose`)         | `--verbose` (a flag)  | `--verbose`           | `false`                    |
| Nullable `bool` (`bool? Cache`)              | `--cache true\|false` | `--cache [cache]`     | `null`                     |

- The option name is `--` followed by the property name in camel case (`FilePath` becomes `--filePath`). Option names are case-sensitive.
- Each option takes exactly one value, except non-nullable `bool` flags, which take none.
- A value that starts with `--` is not taken as a value: `--filePath --lines 5` reports that `--filePath` requires a value.
- A value that cannot be converted is always an error, even for optional options.
- Only public instance properties declared on the type itself are options. Computed properties (`=> ...` or a get accessor with a body) are skipped.
- Excluded properties are not options: they are not checked for a supported type or a constant initializer, they do not appear in `Usage`, and the generated constructor does not assign them. Passing them on the command line is an unknown option.
- `--help` and `-h` are not recognized; they are reported as unknown options, so `Parse` returns `null` and the caller can print `Usage`.
- The type must not declare a constructor that is not private (including the primary constructor of a positional record). `required` properties are supported; the generated constructor is marked with `[SetsRequiredMembers]`.
- On abstract and static types, nothing is generated.

**Supported types:** `string`, `bool` (`true`/`false` in any casing, `1`/`0`), `char`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, enums (member names in any casing or defined numeric values; `[Flags]` enums accept `A, B`), `System.Text.Encoding` (a name such as `utf-8` or a code page such as `65001`) and the nullable versions of these types.

## Usage

```csharp
using System.Text;
using BB84.SourceGenerators.Attributes;

/// <summary>
/// The command line options of the tool.
/// </summary>
[GenerateCommandArgument(ExcludeProperties = [nameof(Cache)])]
public sealed partial class ToolOptions
{
	/// <summary>
	/// The file to process.
	/// </summary>
	public string FilePath { get; }

	/// <summary>
	/// The encoding of the file.
	/// </summary>
	public Encoding? Encoding { get; }

	/// <summary>
	/// The number of lines to read.
	/// </summary>
	public int? Lines { get; }

	/// <summary>
	/// The number of parts to read.
	/// </summary>
	public int PartCount { get; } = 10;

	/// <summary>
	/// Writes verbose output.
	/// </summary>
	public bool Verbose { get; }

	public object? Cache { get; set; }   // excluded, not an option
}

// MyTool --filePath data.txt --lines 5 --verbose
ToolOptions? options = ToolOptions.Parse(args);
if (options is null)
{
	Console.Error.WriteLine(ToolOptions.Usage);
	return 1;
}

// Or with the error messages
if (!ToolOptions.TryParse(args, out ToolOptions? result, out IReadOnlyList<string> errors))
{
	foreach (string error in errors)
		Console.Error.WriteLine(error);   // e.g. "Missing required option '--filePath'."

	Console.Error.WriteLine(ToolOptions.Usage);
	return 1;
}
```

`ToolOptions.Usage` contains:

```text
Usage: MyTool [options]
  --filePath <filePath>     The file to process.
  --encoding [encoding]     The encoding of the file. Optional.
  --lines [lines]           The number of lines to read. Optional.
  --partCount [partCount]   The number of parts to read. Default: 10.
  --verbose                 Writes verbose output.
```

The first line uses the assembly name. The lines are separated by `\n`.

### Generated code

```csharp
public sealed partial class ToolOptions
{
	public const string Usage =
		"Usage: MyTool [options]\n" +
		"  --filePath <filePath>     The file to process.\n" +
		// ... one line per option
		"  --verbose                 Writes verbose output.";

	private ToolOptions(string filePath, Encoding? encoding, int? lines, int partCount, bool verbose)
	{
		FilePath = filePath;
		// ...
	}

	public static ToolOptions? Parse(string[] args)
		=> TryParse(args, out ToolOptions? result, out _) ? result : null;

	public static bool TryParse(string[] args, out ToolOptions? result, out IReadOnlyList<string> errors)
	{
		List<string> errorList = new List<string>();
		int? linesValue = null;
		bool linesFound = false;
		// ... one value and one found flag per option

		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i])
			{
				case "--lines":
					if (linesFound)
						errorList.Add("Option '--lines' is specified more than once.");
					linesFound = true;
					if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
					{
						errorList.Add("Option '--lines' requires a value.");
						break;
					}

					string linesRaw = args[++i];
					if (!int.TryParse(linesRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int linesParsed))
						errorList.Add("Option '--lines' has an invalid Int32 value.");
					else
						linesValue = linesParsed;
					break;
				// ... one case per option
				default:
					errorList.Add("Unknown option '" + args[i] + "'.");
					break;
			}
		}

		if (!filePathFound)
			errorList.Add("Missing required option '--filePath'.");

		if (errorList.Count > 0)
		{
			result = null;
			errors = errorList;
			return false;
		}

		result = new ToolOptions(filePathValue, encodingValue, linesValue, partCountValue, verboseValue);
		errors = Array.Empty<string>();
		return true;
	}
}
```

## Diagnostics

| ID           | Severity | Description                                                                         |
| ------------ | -------- | ----------------------------------------------------------------------------------- |
| `BB84SG0035` | Error    | GenerateCommandArgument requires a partial class or record class                    |
| `BB84SG0036` | Error    | The type declares a constructor that is not private (e.g. a positional record)      |
| `BB84SG0037` | Error    | The property type cannot be read from a command line argument                       |
| `BB84SG0038` | Error    | Two properties map to the same option name                                          |
| `BB84SG0039` | Error    | A property has an initializer that is not a constant                                |
