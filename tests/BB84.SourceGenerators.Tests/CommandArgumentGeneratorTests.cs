// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Text;

using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class CommandArgumentGeneratorTests
{
	private static readonly string[] AllErrors =
	[
		"Option '--lines' has an invalid Int32 value.",
		"Unknown option '--unknown'.",
		"Option '--verbose' is specified more than once.",
		"Option '--encoding' requires a value.",
		"Missing required option '--filePath'.",
	];

	private static readonly string[] MissingValueErrors = ["Option '--filePath' requires a value."];

	private static readonly string[] ExcludedOptionErrors = ["Unknown option '--cache'.", "Unknown option 'x'."];

	[TestMethod]
	public void ParseShouldReadAllOptions()
	{
		string[] args = ["--filePath", "data.txt", "--encoding", "utf-8", "--lines", "5", "--partCount", "3", "--verbose"];

		CommandToolOptions? options = CommandToolOptions.Parse(args);

		Assert.IsNotNull(options);
		Assert.AreEqual("data.txt", options.FilePath);
		Assert.AreEqual(Encoding.UTF8.WebName, options.Encoding?.WebName);
		Assert.AreEqual(5, options.Lines);
		Assert.AreEqual(3, options.PartCount);
		Assert.IsTrue(options.Verbose);
	}

	[TestMethod]
	public void ParseShouldUseDefaultsForMissingOptionalOptions()
	{
		CommandToolOptions? options = CommandToolOptions.Parse(["--filePath", "data.txt"]);

		Assert.IsNotNull(options);
		Assert.IsNull(options.Encoding);
		Assert.IsNull(options.Lines);
		Assert.AreEqual(10, options.PartCount);
		Assert.IsFalse(options.Verbose);
	}

	[TestMethod]
	public void ParseShouldAcceptOptionsInAnyOrder()
	{
		CommandToolOptions? options = CommandToolOptions.Parse(["--verbose", "--lines", "-5", "--filePath", "data.txt"]);

		Assert.IsNotNull(options);
		Assert.AreEqual("data.txt", options.FilePath);
		Assert.AreEqual(-5, options.Lines);
		Assert.IsTrue(options.Verbose);
	}

	[TestMethod]
	public void ParseShouldAcceptEncodingCodePage()
	{
		CommandToolOptions? options = CommandToolOptions.Parse(["--filePath", "data.txt", "--encoding", "65001"]);

		Assert.IsNotNull(options);
		Assert.AreEqual(Encoding.UTF8.WebName, options.Encoding?.WebName);
	}

	[TestMethod]
	[DataRow(new string[0], DisplayName = "Missing mandatory option")]
	[DataRow(new[] { "--filePath" }, DisplayName = "Missing value")]
	[DataRow(new[] { "--filePath", "a", "--other", "b" }, DisplayName = "Unknown option")]
	[DataRow(new[] { "--filePath", "a", "--filePath", "b" }, DisplayName = "Repeated option")]
	[DataRow(new[] { "--filePath", "a", "--lines", "many" }, DisplayName = "Invalid number")]
	[DataRow(new[] { "--filePath", "a", "--encoding", "no-such-encoding" }, DisplayName = "Invalid encoding")]
	[DataRow(new[] { "--filePath", "a", "--help" }, DisplayName = "Help is unknown")]
	[DataRow(new[] { "--FilePath", "a" }, DisplayName = "Option names are case-sensitive")]
	public void ParseShouldReturnNullForInvalidArguments(string[] args)
		=> Assert.IsNull(CommandToolOptions.Parse(args));

	[TestMethod]
	public void TryParseShouldCollectAllErrors()
	{
		string[] args = ["--lines", "many", "--unknown", "--verbose", "--verbose", "--encoding"];

		bool success = CommandToolOptions.TryParse(args, out CommandToolOptions? result, out IReadOnlyList<string> errors);

		Assert.IsFalse(success);
		Assert.IsNull(result);
		CollectionAssert.AreEqual(AllErrors, errors.ToArray());
	}

	[TestMethod]
	public void TryParseShouldNotTakeAnOptionAsValue()
	{
		bool success = CommandToolOptions.TryParse(["--filePath", "--lines", "5"], out _, out IReadOnlyList<string> errors);

		Assert.IsFalse(success);
		CollectionAssert.AreEqual(MissingValueErrors, errors.ToArray());
	}

	[TestMethod]
	public void TryParseShouldReturnNoErrorsOnSuccess()
	{
		bool success = CommandToolOptions.TryParse(["--filePath", "data.txt"], out CommandToolOptions? result, out IReadOnlyList<string> errors);

		Assert.IsTrue(success);
		Assert.IsNotNull(result);
		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void TryParseShouldThrowForNullArguments()
		=> Assert.ThrowsExactly<ArgumentNullException>(() => CommandToolOptions.TryParse(null!, out _, out _));

	[TestMethod]
	public void UsageShouldDescribeAllOptions()
	{
		string expected =
			"Usage: BB84.SourceGenerators.Tests [options]\n" +
			"  --filePath <filePath>     The file to process.\n" +
			"  --encoding [encoding]     The encoding of the file. Optional.\n" +
			"  --lines [lines]           The number of lines to read. Optional.\n" +
			"  --partCount [partCount]   The number of parts to read. Default: 10.\n" +
			"  --verbose                 Writes verbose output.";

		string actual = CommandToolOptions.Usage;

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void ParseShouldIgnoreExcludedProperties()
	{
		CommandExcludingOptions? options = CommandExcludingOptions.Parse(["--name", "tool"]);

		Assert.IsNotNull(options);
		Assert.AreEqual("tool", options.Name);
		Assert.IsNull(options.Cache);
	}

	[TestMethod]
	public void ParseShouldRejectExcludedPropertyAsUnknownOption()
	{
		bool success = CommandExcludingOptions.TryParse(["--name", "tool", "--cache", "x"], out _, out IReadOnlyList<string> errors);

		Assert.IsFalse(success);
		CollectionAssert.AreEqual(ExcludedOptionErrors, errors.ToArray());
	}

	[TestMethod]
	public void UsageShouldNotDescribeExcludedProperties()
	{
		string actual = CommandExcludingOptions.Usage;

		Assert.AreEqual("Usage: BB84.SourceGenerators.Tests [options]\n  --name <name>", actual);
	}

	[TestMethod]
	public void ParseShouldConvertAllSupportedTypes()
	{
		Guid guid = Guid.NewGuid();
		string[] args =
		[
			"--text", "hello",
			"--switch", "1",
			"--letter", "y",
			"--byte", "7",
			"--long", "-42",
			"--double", "1.5",
			"--decimal", "2.25",
			"--dateTime", "2024-01-02T03:04:05Z",
			"--timeSpan", "00:00:05",
			"--guid", guid.ToString(),
			"--level", "high",
			"--features", "A, B",
		];

		CommandAllTypes? options = CommandAllTypes.Parse(args);

		Assert.IsNotNull(options);
		Assert.AreEqual("hello", options.Text);
		Assert.IsTrue(options.Switch);
		Assert.AreEqual('y', options.Letter);
		Assert.AreEqual((byte)7, options.Byte);
		Assert.AreEqual(-42L, options.Long);
		Assert.AreEqual(1.5, options.Double);
		Assert.AreEqual(2.25m, options.Decimal);
		Assert.AreEqual(new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc), options.DateTime);
		Assert.AreEqual(TimeSpan.FromSeconds(5), options.TimeSpan);
		Assert.AreEqual(guid, options.Guid);
		Assert.AreEqual(CommandLevel.High, options.Level);
		Assert.AreEqual(CommandFeatures.A | CommandFeatures.B, options.Features);
	}

	[TestMethod]
	public void ParseShouldUseConstantDefaultsOfAllTypes()
	{
		CommandAllTypes? options = CommandAllTypes.Parse(["--byte", "1"]);

		Assert.IsNotNull(options);
		Assert.IsNull(options.Text);
		Assert.IsNull(options.Switch);
		Assert.AreEqual('x', options.Letter);
		Assert.AreEqual(2L, options.Long);
		Assert.AreEqual(CommandLevel.Low, options.Level);
		Assert.IsNull(options.Features);
	}

	[TestMethod]
	[DataRow("--level", "42")]
	[DataRow("--level", "Unknown")]
	[DataRow("--letter", "xy")]
	[DataRow("--switch", "maybe")]
	public void ParseShouldRejectInvalidValues(string option, string value)
		=> Assert.IsNull(CommandAllTypes.Parse(["--byte", "1", option, value]));
}

/// <summary>
/// The command line options of the tool.
/// </summary>
[GenerateCommandArgument]
internal sealed partial class CommandToolOptions
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
}

[GenerateCommandArgument(ExcludeProperties = [nameof(Cache)])]
internal sealed partial class CommandExcludingOptions
{
	public string Name { get; }

	public object? Cache { get; set; }
}

internal enum CommandLevel
{
	Low,
	High,
}

[Flags]
internal enum CommandFeatures
{
	None = 0,
	A = 1,
	B = 2,
}

[GenerateCommandArgument]
internal sealed partial record CommandAllTypes
{
	public string? Text { get; init; }
	public bool? Switch { get; init; }
	public char Letter { get; init; } = 'x';
	public byte Byte { get; init; }
	public long Long { get; init; } = 2;
	public double Double { get; init; } = 2.5;
	public decimal Decimal { get; init; } = 3.5m;
	public DateTime? DateTime { get; init; }
	public TimeSpan? TimeSpan { get; init; }
	public Guid? Guid { get; init; }
	public CommandLevel Level { get; init; } = CommandLevel.Low;
	public CommandFeatures? Features { get; init; }
}
