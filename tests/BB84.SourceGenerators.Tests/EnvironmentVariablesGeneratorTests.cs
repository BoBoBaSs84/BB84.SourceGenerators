// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class EnvironmentVariablesGeneratorTests
{
	[TestMethod]
	public void TryReadShouldReadAllVariables()
	{
		Dictionary<string, string> variables = new()
		{
			["EXPORTER_SERVER"] = "db.local",
			["EXPORTER_USER"] = "sa",
			["EXPORTER_PORT"] = "1434",
			["ENCRYPT"] = "0",
			["EXPORTER_CONNECT_TIMEOUT"] = "00:00:05",
		};

		bool success = ExporterOptions.TryRead(Lookup(variables), out ExporterOptions? result, out IReadOnlyList<string> errors);

		Assert.IsTrue(success);
		Assert.IsEmpty(errors);
		Assert.IsNotNull(result);
		Assert.AreEqual("db.local", result.Server);
		Assert.AreEqual("sa", result.User);
		Assert.AreEqual(1434, result.Port);
		Assert.IsFalse(result.Encrypt);
		Assert.AreEqual(TimeSpan.FromSeconds(5), result.ConnectTimeout);
	}

	[TestMethod]
	public void TryReadShouldUseInitializersAsFallback()
	{
		Dictionary<string, string> variables = new()
		{
			["EXPORTER_SERVER"] = "db.local",
			["EXPORTER_PORT"] = "   ",
		};

		bool success = ExporterOptions.TryRead(Lookup(variables), out ExporterOptions? result, out _);

		Assert.IsTrue(success);
		Assert.IsNotNull(result);
		Assert.IsNull(result.User);
		Assert.AreEqual(1433, result.Port);
		Assert.IsTrue(result.Encrypt);
		Assert.AreEqual(TimeSpan.FromSeconds(15), result.ConnectTimeout);
	}

	[TestMethod]
	public void TryReadShouldCollectAllErrorsWithoutEchoingValues()
	{
		Dictionary<string, string> variables = new()
		{
			["EXPORTER_PORT"] = "secret-port",
			["ENCRYPT"] = "maybe",
		};

		bool success = ExporterOptions.TryRead(Lookup(variables), out ExporterOptions? result, out IReadOnlyList<string> errors);

		Assert.IsFalse(success);
		Assert.IsNull(result);
		Assert.HasCount(3, errors);
		Assert.AreEqual("Missing required environment variable 'EXPORTER_SERVER'.", errors[0]);
		Assert.AreEqual("Environment variable 'EXPORTER_PORT' has an invalid Int32 value.", errors[1]);
		Assert.AreEqual("Environment variable 'ENCRYPT' has an invalid Boolean value.", errors[2]);
		Assert.IsFalse(errors.Any(e => e.Contains("secret-port")));
	}

	[TestMethod]
	public void ReadShouldThrowWithAllErrors()
	{
		InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => ExporterOptions.Read(Lookup([])));

		Assert.Contains("EXPORTER_SERVER", exception.Message);
	}

	[TestMethod]
	public void ReadShouldThrowForNullLookup()
		=> Assert.ThrowsExactly<ArgumentNullException>(() => ExporterOptions.Read(null!));

	[TestMethod]
	public void ReadShouldReadProcessEnvironment()
	{
		const string variable = "BB84ENVTEST_PROCESS_SERVER";
		Environment.SetEnvironmentVariable(variable, "from-process");

		try
		{
			ProcessSettings settings = ProcessSettings.Read();

			Assert.AreEqual("from-process", settings.Server);
		}
		finally
		{
			Environment.SetEnvironmentVariable(variable, null);
		}
	}

	[TestMethod]
	[DataRow("true", true)]
	[DataRow("FALSE", false)]
	[DataRow(" 1 ", true)]
	[DataRow("0", false)]
	public void TryReadShouldParseBooleans(string value, bool expected)
	{
		bool success = AllTypesSettings.TryRead(Lookup(new() { ["ALL_FLAG"] = value }), out AllTypesSettings? result, out _);

		Assert.IsTrue(success);
		Assert.AreEqual(expected, result!.Flag);
	}

	[TestMethod]
	public void TryReadShouldParseAllTypesWithInvariantCulture()
	{
		Dictionary<string, string> variables = new()
		{
			["ALL_TEXT"] = "text",
			["ALL_LETTER"] = "x",
			["ALL_BYTE"] = "255",
			["ALL_LONG"] = "-9000000000",
			["ALL_U_LONG"] = "18000000000000000000",
			["ALL_DOUBLE"] = "1,234.5",
			["ALL_DECIMAL"] = "0.1",
			["ALL_DATE_TIME"] = "2026-11-14T12:30:00Z",
			["ALL_DATE_TIME_OFFSET"] = "2026-11-14T12:30:00+02:00",
			["ALL_TIME_SPAN"] = "1.02:03:04",
			["ALL_GUID"] = "6f9619ff-8b86-d011-b42d-00cf4fc964ff",
			["ALL_LEVEL"] = "high",
			["ALL_FEATURES"] = "A, B",
		};

		bool success = AllTypesSettings.TryRead(Lookup(variables), out AllTypesSettings? result, out IReadOnlyList<string> errors);

		Assert.IsTrue(success, string.Join(Environment.NewLine, errors));
		Assert.IsNotNull(result);
		Assert.AreEqual("text", result.Text);
		Assert.AreEqual('x', result.Letter);
		Assert.AreEqual((byte)255, result.Byte);
		Assert.AreEqual(-9000000000L, result.Long);
		Assert.AreEqual(18000000000000000000UL, result.ULong);
		Assert.AreEqual(1234.5, result.Double);
		Assert.AreEqual(0.1m, result.Decimal);
		Assert.AreEqual(new DateTime(2026, 11, 14, 12, 30, 0, DateTimeKind.Utc), result.DateTime);
		Assert.AreEqual(new DateTimeOffset(2026, 11, 14, 12, 30, 0, TimeSpan.FromHours(2)), result.DateTimeOffset);
		Assert.AreEqual(new TimeSpan(1, 2, 3, 4), result.TimeSpan);
		Assert.AreEqual(new Guid("6f9619ff-8b86-d011-b42d-00cf4fc964ff"), result.Guid);
		Assert.AreEqual(EnvironmentLevel.High, result.Level);
		Assert.AreEqual(EnvironmentFeatures.A | EnvironmentFeatures.B, result.Features);
	}

	[TestMethod]
	public void TryReadShouldUseConstantFallbacks()
	{
		bool success = AllTypesSettings.TryRead(Lookup([]), out AllTypesSettings? result, out _);

		Assert.IsTrue(success);
		Assert.IsNotNull(result);
		Assert.AreEqual(2.5f, result.Float);
		Assert.AreEqual(EnvironmentLevel.Low, result.Level);
		Assert.IsNull(result.Features);
		Assert.IsNull(result.Text);
	}

	[TestMethod]
	[DataRow("ALL_LEVEL", "42", "EnvironmentLevel")]
	[DataRow("ALL_LETTER", "xy", "Char")]
	[DataRow("ALL_BYTE", "256", "Byte")]
	[DataRow("ALL_GUID", "no-guid", "Guid")]
	public void TryReadShouldRejectInvalidValues(string variable, string value, string typeName)
	{
		bool success = AllTypesSettings.TryRead(Lookup(new() { [variable] = value }), out _, out IReadOnlyList<string> errors);

		Assert.IsFalse(success);
		Assert.AreEqual($"Environment variable '{variable}' has an invalid {typeName} value.", errors.Single());
	}

	[TestMethod]
	public void TryReadShouldIncludeInheritedProperties()
	{
		Dictionary<string, string> variables = new()
		{
			["DERIVED_SERVER"] = "base-server",
			["DERIVED_DATABASE"] = "db",
		};

		bool success = DerivedEnvironmentSettings.TryRead(Lookup(variables), out DerivedEnvironmentSettings? result, out _);

		Assert.IsTrue(success);
		Assert.IsNotNull(result);
		Assert.AreEqual("base-server", result.Server);
		Assert.AreEqual("db", result.Database);
	}

	[TestMethod]
	public void TryReadShouldSkipExcludedProperties()
	{
		bool success = ExcludingSettings.TryRead(Lookup(new() { ["EXCLUDING_NAME"] = "n", ["EXCLUDING_CACHE"] = "c" }), out ExcludingSettings? result, out _);

		Assert.IsTrue(success);
		Assert.AreEqual("n", result!.Name);
		Assert.IsNull(result.Cache);
	}

	[TestMethod]
	public void TryReadShouldUseAsIsNamingStyle()
	{
		bool success = AsIsSettings.TryRead(Lookup(new() { ["MyApp__ConnectTimeoutMs"] = "100" }), out AsIsSettings? result, out _);

		Assert.IsTrue(success);
		Assert.AreEqual(100, result!.ConnectTimeoutMs);
	}

	[TestMethod]
	public void PartialPropertiesShouldReadAndWriteProcessEnvironment()
	{
		try
		{
			Assert.IsNull(LiveSettings.Token);
			Assert.IsNull(LiveSettings.Verbose);
			Assert.ThrowsExactly<InvalidOperationException>(() => _ = LiveSettings.Port);

			LiveSettings.Token = "abc";
			LiveSettings.Port = 8080;
			LiveSettings.Verbose = true;

			Assert.AreEqual("abc", Environment.GetEnvironmentVariable("BB84ENVTEST_LIVE_TOKEN"));
			Assert.AreEqual("8080", Environment.GetEnvironmentVariable("BB84ENVTEST_LIVE_PORT"));
			Assert.AreEqual("abc", LiveSettings.Token);
			Assert.AreEqual(8080, LiveSettings.Port);
			Assert.IsTrue(LiveSettings.Verbose);

			LiveSettings.Verbose = null;

			Assert.IsNull(Environment.GetEnvironmentVariable("BB84ENVTEST_LIVE_VERBOSE"));

			Environment.SetEnvironmentVariable("BB84ENVTEST_LIVE_PORT", "invalid");

			InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => _ = LiveSettings.Port);
			Assert.AreEqual("Environment variable 'BB84ENVTEST_LIVE_PORT' has an invalid Int32 value.", exception.Message);
		}
		finally
		{
			Environment.SetEnvironmentVariable("BB84ENVTEST_LIVE_TOKEN", null);
			Environment.SetEnvironmentVariable("BB84ENVTEST_LIVE_PORT", null);
			Environment.SetEnvironmentVariable("BB84ENVTEST_LIVE_VERBOSE", null);
		}
	}

	[TestMethod]
	public void AbstractTypeShouldImplementPartialProperties()
	{
		Environment.SetEnvironmentVariable("BB84ENVTEST_ABSTRACT_TOKEN", "token");

		try
		{
			Assert.AreEqual("token", new ConcreteLiveSettings().Token);
		}
		finally
		{
			Environment.SetEnvironmentVariable("BB84ENVTEST_ABSTRACT_TOKEN", null);
		}
	}

	private static Func<string, string?> Lookup(Dictionary<string, string> variables)
		=> name => variables.TryGetValue(name, out string? value) ? value : null;
}

[GenerateEnvironmentVariables("Exporter")]
internal sealed partial record ExporterOptions
{
	public required string Server { get; init; }

	public string? User { get; init; }

	public int Port { get; init; } = 1433;

	[EnvironmentVariableName("ENCRYPT")]
	public bool Encrypt { get; init; } = true;

	public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(15);
}

[GenerateEnvironmentVariables("BB84EnvTest_Process")]
internal sealed partial class ProcessSettings
{
	public string Server { get; set; } = string.Empty;
}

internal enum EnvironmentLevel
{
	Low,
	High,
}

[Flags]
internal enum EnvironmentFeatures
{
	None = 0,
	A = 1,
	B = 2,
}

[GenerateEnvironmentVariables("All")]
internal sealed partial class AllTypesSettings
{
	public string? Text { get; set; }
	public bool Flag { get; set; } = true;
	public char Letter { get; set; } = '-';
	public byte Byte { get; init; } = 1;
	public long Long { get; init; } = 1;
	public ulong ULong { get; init; } = 2;
	public float Float { get; init; } = 2.5f;
	public double Double { get; init; } = 1.5;
	public decimal Decimal { get; init; } = 3.5m;
	public DateTime? DateTime { get; init; }
	public DateTimeOffset? DateTimeOffset { get; init; }
	public TimeSpan? TimeSpan { get; init; }
	public Guid? Guid { get; init; }
	public EnvironmentLevel Level { get; init; } = EnvironmentLevel.Low;
	public EnvironmentFeatures? Features { get; init; }
}

internal abstract record BaseEnvironmentSettings
{
	public string? Server { get; init; }
}

[GenerateEnvironmentVariables("Derived", IncludeInherited = true)]
internal sealed partial record DerivedEnvironmentSettings : BaseEnvironmentSettings
{
	public string? Database { get; init; }
}

[GenerateEnvironmentVariables("Excluding", ExcludeProperties = [nameof(Cache)])]
internal sealed partial class ExcludingSettings
{
	public string? Name { get; set; }

	public string? Cache { get; set; }
}

[GenerateEnvironmentVariables("MyApp", NamingStyle = EnvironmentVariableNamingStyle.AsIs, Separator = "__")]
internal sealed partial class AsIsSettings
{
	public int? ConnectTimeoutMs { get; set; }
}

[GenerateEnvironmentVariables("BB84EnvTest_Live", Writable = true)]
internal static partial class LiveSettings
{
	public static partial string? Token { get; set; }

	public static partial int Port { get; set; }

	public static partial bool? Verbose { get; set; }
}

[GenerateEnvironmentVariables("BB84EnvTest_Abstract")]
internal abstract partial class AbstractLiveSettings
{
	public partial string? Token { get; }
}

internal sealed class ConcreteLiveSettings : AbstractLiveSettings;
