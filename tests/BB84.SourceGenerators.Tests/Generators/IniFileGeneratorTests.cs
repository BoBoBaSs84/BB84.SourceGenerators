// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Globalization;

using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests.Generators;

[TestClass]
public sealed class IniFileGeneratorTests
{
	[TestMethod]
	public void ReadShouldDeserializeSimpleIniContent()
	{
		string content = "[General]\r\nAppName=TestApp\r\nVersion=42\r\nEnabled=True\r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("TestApp", result.General.AppName);
		Assert.AreEqual(42, result.General.Version);
		Assert.IsTrue(result.General.Enabled);
	}

	[TestMethod]
	public void WriteShouldSerializeSimpleIniContent()
	{
		TestIniFile instance = new()
		{
			General = new TestGeneralSection
			{
				AppName = "TestApp",
				Version = 42,
				Enabled = true
			}
		};

		string result = TestIniFile.Write(instance);

		Assert.Contains("[General]", result);
		Assert.Contains("AppName=TestApp", result);
		Assert.Contains("Version=42", result);
		Assert.Contains("Enabled=True", result);
	}

	[TestMethod]
	public void ReadShouldHandleMultipleSections()
	{
		string content =
			"[General]\r\n" +
			"AppName=MyApp\r\n" +
			"Version=1\r\n" +
			"Enabled=False\r\n" +
			"\r\n" +
			"[Database]\r\n" +
			"Host=localhost\r\n" +
			"Port=5432\r\n" +
			"Timeout=30.5\r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("MyApp", result.General.AppName);
		Assert.AreEqual(1, result.General.Version);
		Assert.IsFalse(result.General.Enabled);

		Assert.IsNotNull(result.Database);
		Assert.AreEqual("localhost", result.Database.Host);
		Assert.AreEqual(5432, result.Database.Port);
		Assert.AreEqual(30.5, result.Database.Timeout);
	}

	[TestMethod]
	public void WriteShouldSerializeMultipleSections()
	{
		TestIniFile instance = new()
		{
			General = new TestGeneralSection
			{
				AppName = "MyApp",
				Version = 1,
				Enabled = false
			},
			Database = new TestDatabaseSection
			{
				Host = "localhost",
				Port = 5432,
				Timeout = 30.5
			}
		};

		string result = TestIniFile.Write(instance);

		Assert.Contains("[General]", result);
		Assert.Contains("AppName=MyApp", result);
		Assert.Contains("[Database]", result);
		Assert.Contains("Host=localhost", result);
		Assert.Contains("Port=5432", result);
		Assert.Contains("Timeout=30.5", result);
	}

	[TestMethod]
	public void ReadShouldSkipCommentLines()
	{
		string content =
			"; This is a comment\r\n" +
			"# This is also a comment\r\n" +
			"[General]\r\n" +
			"; Another comment\r\n" +
			"AppName=TestApp\r\n" +
			"Version=1\r\n" +
			"Enabled=True\r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("TestApp", result.General.AppName);
		Assert.AreEqual(1, result.General.Version);
		Assert.IsTrue(result.General.Enabled);
	}

	[TestMethod]
	public void ReadShouldSkipEmptyLines()
	{
		string content =
			"\r\n" +
			"\r\n" +
			"[General]\r\n" +
			"\r\n" +
			"AppName=TestApp\r\n" +
			"\r\n" +
			"Version=7\r\n" +
			"Enabled=False\r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("TestApp", result.General.AppName);
		Assert.AreEqual(7, result.General.Version);
		Assert.IsFalse(result.General.Enabled);
	}

	[TestMethod]
	public void ReadShouldHandleLinesWithoutEqualsSign()
	{
		string content =
			"[General]\r\n" +
			"InvalidLineWithoutEquals\r\n" +
			"AppName=TestApp\r\n" +
			"Version=1\r\n" +
			"Enabled=True\r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("TestApp", result.General.AppName);
	}

	[TestMethod]
	public void ReadShouldHandleLineFeedOnlyLineEndings()
	{
		string content =
			"[General]\n" +
			"AppName=TestApp\n" +
			"Version=99\n" +
			"Enabled=True\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("TestApp", result.General.AppName);
		Assert.AreEqual(99, result.General.Version);
		Assert.IsTrue(result.General.Enabled);
	}

	[TestMethod]
	public void ReadShouldHandleCustomSectionName()
	{
		string content = "[CustomSection]\r\nScore=99.9\r\n";

		TestIniFileWithCustomNames result = TestIniFileWithCustomNames.Read(content);

		Assert.IsNotNull(result.Stats);
		Assert.AreEqual(99.9f, result.Stats.Score);
	}

	[TestMethod]
	public void ReadShouldHandleCustomKeyName()
	{
		string content = "[Metrics]\r\nuser_score=88.8\r\n";

		TestIniFileWithCustomNames result = TestIniFileWithCustomNames.Read(content);

		Assert.IsNotNull(result.Metrics);
		Assert.AreEqual(88.8f, result.Metrics.Score);
	}

	[TestMethod]
	public void WriteShouldUseCustomSectionName()
	{
		TestIniFileWithCustomNames instance = new()
		{
			Stats = new TestCustomSection { Score = 99.9f }
		};

		string result = TestIniFileWithCustomNames.Write(instance);

		Assert.Contains("[CustomSection]", result);
		Assert.Contains("Score=99.9", result);
	}

	[TestMethod]
	public void WriteShouldUseCustomKeyName()
	{
		TestIniFileWithCustomNames instance = new()
		{
			Metrics = new TestMetricsSection { Score = 88.8f }
		};

		string result = TestIniFileWithCustomNames.Write(instance);

		Assert.Contains("[Metrics]", result);
		Assert.Contains("user_score=88.8", result);
	}

	[TestMethod]
	public void WriteShouldSkipNullSections()
	{
		TestIniFile instance = new()
		{
			General = new TestGeneralSection
			{
				AppName = "TestApp",
				Version = 1,
				Enabled = true
			},
			Database = null
		};

		string result = TestIniFile.Write(instance);

		Assert.Contains("[General]", result);
		Assert.DoesNotContain("[Database]", result);
	}

	[TestMethod]
	public void ReadThenWriteShouldRoundTrip()
	{
		TestIniFile original = new()
		{
			General = new TestGeneralSection
			{
				AppName = "RoundTrip",
				Version = 10,
				Enabled = true
			},
			Database = new TestDatabaseSection
			{
				Host = "db.example.com",
				Port = 3306,
				Timeout = 60.0
			}
		};

		string serialized = TestIniFile.Write(original);
		TestIniFile deserialized = TestIniFile.Read(serialized);

		Assert.IsNotNull(deserialized.General);
		Assert.AreEqual(original.General.AppName, deserialized.General.AppName);
		Assert.AreEqual(original.General.Version, deserialized.General.Version);
		Assert.AreEqual(original.General.Enabled, deserialized.General.Enabled);

		Assert.IsNotNull(deserialized.Database);
		Assert.AreEqual(original.Database.Host, deserialized.Database.Host);
		Assert.AreEqual(original.Database.Port, deserialized.Database.Port);
		Assert.AreEqual(original.Database.Timeout, deserialized.Database.Timeout);
	}

	[TestMethod]
	public void ReadShouldHandleAllSupportedTypes()
	{
		string content =
			"[AllTypes]\r\n" +
			"StringValue=Hello\r\n" +
			"IntValue=42\r\n" +
			"LongValue=9999999999\r\n" +
			"FloatValue=3.14\r\n" +
			"DoubleValue=2.718281828\r\n" +
			"BoolValue=True\r\n" +
			"DecimalValue=123.456\r\n" +
			"DateTimeValue=01/15/2025 10:30:00\r\n";

		TestIniFileAllTypes result = TestIniFileAllTypes.Read(content);

		Assert.IsNotNull(result.AllTypes);
		Assert.AreEqual("Hello", result.AllTypes.StringValue);
		Assert.AreEqual(42, result.AllTypes.IntValue);
		Assert.AreEqual(9999999999L, result.AllTypes.LongValue);
		Assert.AreEqual(3.14f, result.AllTypes.FloatValue);
		Assert.AreEqual(2.718281828, result.AllTypes.DoubleValue);
		Assert.IsTrue(result.AllTypes.BoolValue);
		Assert.AreEqual(123.456m, result.AllTypes.DecimalValue);
		Assert.AreEqual(DateTime.Parse("01/15/2025 10:30:00", CultureInfo.InvariantCulture), result.AllTypes.DateTimeValue);
	}

	[TestMethod]
	public void WriteShouldHandleAllSupportedTypes()
	{
		DateTime testDate = DateTime.Parse("01/15/2025 10:30:00", CultureInfo.InvariantCulture);
		TestIniFileAllTypes instance = new()
		{
			AllTypes = new TestAllTypesSection
			{
				StringValue = "Hello",
				IntValue = 42,
				LongValue = 9999999999L,
				FloatValue = 3.14f,
				DoubleValue = 2.718281828,
				BoolValue = true,
				DecimalValue = 123.456m,
				DateTimeValue = testDate
			}
		};

		string result = TestIniFileAllTypes.Write(instance);

		Assert.Contains("[AllTypes]", result);
		Assert.Contains("StringValue=Hello", result);
		Assert.Contains("IntValue=42", result);
		Assert.Contains("LongValue=9999999999", result);
		Assert.Contains("FloatValue=3.14", result);
		Assert.Contains("DoubleValue=2.718281828", result);
		Assert.Contains("BoolValue=True", result);
		Assert.Contains("DecimalValue=123.456", result);
		Assert.Contains("DateTimeValue=" + testDate.ToString(CultureInfo.InvariantCulture), result);
	}

	[TestMethod]
	public void ReadShouldHandleEmptyContent()
	{
		string content = "";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result);
		Assert.IsNull(result.General);
		Assert.IsNull(result.Database);
	}

	[TestMethod]
	public void ReadShouldHandleInitializedProperties()
	{
		string content = "[Settings]\r\nValue=CustomValue\r\n";

		TestIniFileWithInit result = TestIniFileWithInit.Read(content);

		Assert.IsNotNull(result.Settings);
		Assert.AreEqual("CustomValue", result.Settings.Value);
	}

	[TestMethod]
	public void WriteShouldHandleInitializedProperties()
	{
		TestIniFileWithInit instance = new();

		string result = TestIniFileWithInit.Write(instance);

		Assert.Contains("[Settings]", result);
		Assert.Contains("Value=Default", result);
	}

	[TestMethod]
	public void ReadShouldTrimWhitespaceFromKeysAndValues()
	{
		string content =
			"[General]\r\n" +
			"  AppName  =  TestApp  \r\n" +
			"  Version  =  5  \r\n" +
			"  Enabled  =  True  \r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("TestApp", result.General.AppName);
		Assert.AreEqual(5, result.General.Version);
		Assert.IsTrue(result.General.Enabled);
	}
}

#region Test Types

[GenerateIniFile]
internal sealed partial class TestIniFile
{
	[GenerateIniFileSection]
	public TestGeneralSection? General { get; set; }

	[GenerateIniFileSection]
	public TestDatabaseSection? Database { get; set; }
}

public class TestGeneralSection
{
	[GenerateIniFileValue]
	public string? AppName { get; set; }

	[GenerateIniFileValue]
	public int Version { get; set; }

	[GenerateIniFileValue]
	public bool Enabled { get; set; }
}

public class TestDatabaseSection
{
	[GenerateIniFileValue]
	public string? Host { get; set; }

	[GenerateIniFileValue]
	public int Port { get; set; }

	[GenerateIniFileValue]
	public double Timeout { get; set; }
}

[GenerateIniFile]
internal sealed partial class TestIniFileWithCustomNames
{
	[GenerateIniFileSection("CustomSection")]
	public TestCustomSection? Stats { get; set; }

	[GenerateIniFileSection]
	public TestMetricsSection? Metrics { get; set; }
}

public class TestCustomSection
{
	[GenerateIniFileValue]
	public float Score { get; set; }
}

public class TestMetricsSection
{
	[GenerateIniFileValue("user_score")]
	public float Score { get; set; }
}

[GenerateIniFile]
internal sealed partial class TestIniFileAllTypes
{
	[GenerateIniFileSection]
	public TestAllTypesSection? AllTypes { get; set; }
}

public class TestAllTypesSection
{
	[GenerateIniFileValue]
	public string? StringValue { get; set; }

	[GenerateIniFileValue]
	public int IntValue { get; set; }

	[GenerateIniFileValue]
	public long LongValue { get; set; }

	[GenerateIniFileValue]
	public float FloatValue { get; set; }

	[GenerateIniFileValue]
	public double DoubleValue { get; set; }

	[GenerateIniFileValue]
	public bool BoolValue { get; set; }

	[GenerateIniFileValue]
	public decimal DecimalValue { get; set; }

	[GenerateIniFileValue]
	public DateTime DateTimeValue { get; set; }
}

[GenerateIniFile]
internal sealed partial class TestIniFileWithInit
{
	[GenerateIniFileSection]
	public TestSettingsSection Settings { get; set; } = new TestSettingsSection();
}

public class TestSettingsSection
{
	[GenerateIniFileValue]
	public string Value { get; set; } = "Default";
}

#endregion
