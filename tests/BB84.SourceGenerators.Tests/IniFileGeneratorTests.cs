// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Globalization;
using System.Text;

using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class IniFileGeneratorTests
{
	private readonly CancellationToken _testToken = CancellationToken.None;

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

	[TestMethod]
	public void ReadShouldIgnoreCaseByDefault()
	{
		string content =
			"[general]\r\n" +
			"appname=TestApp\r\n" +
			"version=3\r\n" +
			"enabled=true\r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("TestApp", result.General.AppName);
		Assert.AreEqual(3, result.General.Version);
		Assert.IsTrue(result.General.Enabled);
	}

	[TestMethod]
	public void ReadShouldRespectCaseSensitiveComparison()
	{
		string content =
			"[General]\r\n" +
			"AppName=TestApp\r\n" +
			"Version=3\r\n" +
			"Enabled=True\r\n";

		TestIniFileCaseSensitive result = TestIniFileCaseSensitive.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("TestApp", result.General.AppName);
		Assert.AreEqual(3, result.General.Version);
		Assert.IsTrue(result.General.Enabled);
	}

	[TestMethod]
	public void ReadShouldNotMatchWhenCaseSensitiveAndCaseDiffers()
	{
		string content =
			"[general]\r\n" +
			"appname=TestApp\r\n" +
			"version=3\r\n" +
			"enabled=true\r\n";

		TestIniFileCaseSensitive result = TestIniFileCaseSensitive.Read(content);

		Assert.IsNull(result.General);
	}

	[TestMethod]
	public void WriteShouldUseInvariantCultureForNumbers()
	{
		TestIniFileAllTypes instance = new()
		{
			AllTypes = new TestAllTypesSection
			{
				FloatValue = 3.14f,
				DoubleValue = 2.718281828,
				DecimalValue = 123.456m
			}
		};
		string result = TestIniFileAllTypes.Write(instance);
		Assert.Contains("FloatValue=3.14", result);
		Assert.Contains("DoubleValue=2.718281828", result);
		Assert.Contains("DecimalValue=123.456", result);
	}

	[TestMethod]
	public void ReadShouldSupportSectionNesting()
	{
		string content = "[Section]\r\n" +
			"Domain=example.com\r\n" +
			"[Section.SubSection]\r\n" +
			"Foo=Bar";

		TestIniFileWithNestedSection result = TestIniFileWithNestedSection.Read(content);

		Assert.IsNotNull(result.Section);
		Assert.AreEqual("example.com", result.Section.Domain);
		Assert.IsNotNull(result.Section.SubSection);
		Assert.AreEqual("Bar", result.Section.SubSection.Foo);
	}

	[TestMethod]
	public void WriteShouldSupportSectionNesting()
	{
		TestIniFileWithNestedSection instance = new()
		{
			Section = new TestSection
			{
				Domain = "example.com",
				SubSection = new TestSubSection { Foo = "Bar" }
			}
		};

		string result = TestIniFileWithNestedSection.Write(instance);

		Assert.Contains("[Section]", result);
		Assert.Contains("Domain=example.com", result);
		Assert.Contains("[Section.SubSection]", result);
		Assert.Contains("Foo=Bar", result);
	}

	[TestMethod]
	public void ReadShouldSupportCustomSectionDelimiter()
	{
		string content = "[Section]\r\n" +
			"Domain=example.com\r\n" +
			"[Section/SubSection]\r\n" +
			"Foo=Bar";

		TestIniFileWithCustomDelimiter result = TestIniFileWithCustomDelimiter.Read(content);

		Assert.IsNotNull(result.Section);
		Assert.AreEqual("example.com", result.Section.Domain);
		Assert.IsNotNull(result.Section.SubSection);
		Assert.AreEqual("Bar", result.Section.SubSection.Foo);
	}

	[TestMethod]
	public void WriteShouldSupportCustomSectionDelimiter()
	{
		TestIniFileWithCustomDelimiter instance = new()
		{
			Section = new TestSection
			{
				Domain = "example.com",
				SubSection = new TestSubSection
				{
					Foo = "Bar"
				}
			}
		};

		string result = TestIniFileWithCustomDelimiter.Write(instance);

		Assert.Contains("[Section/SubSection]", result);
		Assert.DoesNotContain("[Section.SubSection]", result);
	}
	[TestMethod]
	public void ReadShouldHandleEnumValues()
	{
		string content = "[App]\r\nLevel=Warning\r\n";

		TestIniFileWithEnums result = TestIniFileWithEnums.Read(content);

		Assert.IsNotNull(result.App);
		Assert.AreEqual(TestLogLevel.Warning, result.App.Level);
	}

	[TestMethod]
	public void WriteShouldHandleEnumValues()
	{
		TestIniFileWithEnums instance = new()
		{
			App = new TestAppSection { Level = TestLogLevel.Error }
		};

		string result = TestIniFileWithEnums.Write(instance);

		Assert.Contains("[App]", result);
		Assert.Contains("Level=Error", result);
	}

	[TestMethod]
	public void ReadShouldHandleFlagsEnumValues()
	{
		string content = "[App]\r\nPerms=Read Execute\r\n";

		TestIniFileWithFlagsEnum result = TestIniFileWithFlagsEnum.Read(content);

		Assert.IsNotNull(result.App);
		Assert.AreEqual(TestPermissions.Read | TestPermissions.Execute, result.App.Perms);
	}

	[TestMethod]
	public void WriteShouldHandleFlagsEnumValues()
	{
		TestIniFileWithFlagsEnum instance = new()
		{
			App = new TestFlagsAppSection { Perms = TestPermissions.Read | TestPermissions.Write }
		};

		string result = TestIniFileWithFlagsEnum.Write(instance);

		Assert.Contains("[App]", result);
		Assert.Contains("Perms=Read Write", result);
	}

	[TestMethod]
	public void ReadThenWriteShouldRoundTripEnumValues()
	{
		TestIniFileWithEnums original = new()
		{
			App = new TestAppSection { Level = TestLogLevel.Info }
		};

		string serialized = TestIniFileWithEnums.Write(original);
		TestIniFileWithEnums deserialized = TestIniFileWithEnums.Read(serialized);

		Assert.IsNotNull(deserialized.App);
		Assert.AreEqual(original.App.Level, deserialized.App.Level);
	}

	[TestMethod]
	public void ReadThenWriteShouldRoundTripFlagsEnumValues()
	{
		TestIniFileWithFlagsEnum original = new()
		{
			App = new TestFlagsAppSection { Perms = TestPermissions.Read | TestPermissions.Execute }
		};

		string serialized = TestIniFileWithFlagsEnum.Write(original);
		TestIniFileWithFlagsEnum deserialized = TestIniFileWithFlagsEnum.Read(serialized);

		Assert.IsNotNull(deserialized.App);
		Assert.AreEqual(original.App.Perms, deserialized.App.Perms);
	}

	[TestMethod]
	public void WriteShouldIncludeCommentsWhenSerializeCommentsIsTrue()
	{
		TestIniFileWithComments instance = new()
		{
			General = new TestCommentedGeneralSection
			{
				AppName = "MyApp",
				Version = 2
			}
		};

		string result = TestIniFileWithComments.Write(instance);

		Assert.Contains("; General application settings", result);
		Assert.Contains("[General]", result);
		Assert.Contains("; The display name of the application", result);
		Assert.Contains("AppName=MyApp", result);
		Assert.Contains("; The current version number", result);
		Assert.Contains("Version=2", result);
	}

	[TestMethod]
	public void WriteShouldNotIncludeCommentsWhenSerializeCommentsIsFalse()
	{
		TestIniFile instance = new()
		{
			General = new TestGeneralSection
			{
				AppName = "MyApp",
				Version = 1,
				Enabled = true
			}
		};

		string result = TestIniFile.Write(instance);

		Assert.DoesNotContain("; ", result);
	}

	[TestMethod]
	public void ReadShouldSkipCommentLinesFromSerializedOutput()
	{
		TestIniFileWithComments instance = new()
		{
			General = new TestCommentedGeneralSection
			{
				AppName = "TestApp",
				Version = 5
			}
		};

		string serialized = TestIniFileWithComments.Write(instance);
		TestIniFileWithComments deserialized = TestIniFileWithComments.Read(serialized);

		Assert.IsNotNull(deserialized.General);
		Assert.AreEqual("TestApp", deserialized.General.AppName);
		Assert.AreEqual(5, deserialized.General.Version);
	}

	[TestMethod]
	public void LoadShouldCopyValuesFromOtherInstance()
	{
		TestIniFile target = new()
		{
			General = new TestGeneralSection { AppName = "Old", Version = 1, Enabled = false },
			Database = new TestDatabaseSection { Host = "old-host", Port = 1234, Timeout = 10.0 }
		};

		TestIniFile source = new()
		{
			General = new TestGeneralSection { AppName = "New", Version = 2, Enabled = true },
			Database = new TestDatabaseSection { Host = "new-host", Port = 5678, Timeout = 30.0 }
		};

		target.Load(source);

		Assert.AreEqual("New", target.General.AppName);
		Assert.AreEqual(2, target.General.Version);
		Assert.IsTrue(target.General.Enabled);
		Assert.AreEqual("new-host", target.Database.Host);
		Assert.AreEqual(5678, target.Database.Port);
		Assert.AreEqual(30.0, target.Database.Timeout);
	}

	[TestMethod]
	public void LoadShouldCopyNestedSectionValues()
	{
		TestIniFileWithNestedSection target = new() { Section = new() { SubSection = new() } };
		target.Section.Domain = "old-domain";
		target.Section.SubSection.Foo = "old-foo";

		TestIniFileWithNestedSection source = new() { Section = new() { SubSection = new() } };
		source.Section.Domain = "new-domain";
		source.Section.SubSection.Foo = "new-foo";

		target.Load(source);

		Assert.AreEqual("new-domain", target.Section.Domain);
		Assert.AreEqual("new-foo", target.Section.SubSection.Foo);
	}

	[TestMethod]
	public void LoadShouldNotThrowWhenNestedParentIsNull()
	{
		TestIniFileWithNestedSection target = new();
		TestIniFileWithNestedSection source = new() { Section = new() { SubSection = new() } };

		target.Section = null!;
		source.Section.Domain = "new-domain";
		source.Section.SubSection.Foo = "new-foo";

		target.Load(source);

		Assert.IsNull(target.Section);
	}

	[TestMethod]
	public void ReadShouldHandleGuidValues()
	{
		string content = "[Extended]\r\nId=d3b07384-d9a0-4e9b-8c12-f7a8b2c1d3e5\r\n";

		TestIniFileExtendedTypes result = TestIniFileExtendedTypes.Read(content);

		Assert.IsNotNull(result.Extended);
		Assert.AreEqual(Guid.Parse("d3b07384-d9a0-4e9b-8c12-f7a8b2c1d3e5"), result.Extended.Id);
	}

	[TestMethod]
	public void ReadShouldHandleTimeSpanValues()
	{
		string content = "[Extended]\r\nDuration=01:30:00\r\n";

		TestIniFileExtendedTypes result = TestIniFileExtendedTypes.Read(content);

		Assert.IsNotNull(result.Extended);
		Assert.AreEqual(TimeSpan.Parse("01:30:00", CultureInfo.InvariantCulture), result.Extended.Duration);
	}

	[TestMethod]
	public void ReadShouldHandleDateTimeOffsetValues()
	{
		string content = "[Extended]\r\nTimestamp=01/15/2025 10:30:00 +02:00\r\n";

		TestIniFileExtendedTypes result = TestIniFileExtendedTypes.Read(content);

		Assert.IsNotNull(result.Extended);
		Assert.AreEqual(DateTimeOffset.Parse("01/15/2025 10:30:00 +02:00", CultureInfo.InvariantCulture), result.Extended.Timestamp);
	}

	[TestMethod]
	public void WriteShouldHandleGuidValues()
	{
		Guid id = Guid.Parse("d3b07384-d9a0-4e9b-8c12-f7a8b2c1d3e5");
		TestIniFileExtendedTypes instance = new()
		{
			Extended = new TestExtendedTypesSection { Id = id }
		};

		string result = TestIniFileExtendedTypes.Write(instance);

		Assert.Contains("Id=d3b07384-d9a0-4e9b-8c12-f7a8b2c1d3e5", result);
	}

	[TestMethod]
	public void WriteShouldHandleTimeSpanValues()
	{
		TestIniFileExtendedTypes instance = new()
		{
			Extended = new TestExtendedTypesSection { Duration = TimeSpan.FromHours(1.5) }
		};

		string result = TestIniFileExtendedTypes.Write(instance);

		Assert.Contains("Duration=01:30:00", result);
	}

	[TestMethod]
	public void WriteShouldHandleDateTimeOffsetValues()
	{
		DateTimeOffset ts = DateTimeOffset.Parse("01/15/2025 10:30:00 +02:00", CultureInfo.InvariantCulture);
		TestIniFileExtendedTypes instance = new()
		{
			Extended = new TestExtendedTypesSection { Timestamp = ts }
		};

		string result = TestIniFileExtendedTypes.Write(instance);

		Assert.Contains("Timestamp=" + ts.ToString(CultureInfo.InvariantCulture), result);
	}

	[TestMethod]
	public void ReadShouldHandleCommaSeparatedListValues()
	{
		string content = "[Collections]\r\nTags=one,two,three\r\nScores=10,20,30\r\n";

		TestIniFileCollections result = TestIniFileCollections.Read(content);

		Assert.IsNotNull(result.Collections);
		CollectionAssert.AreEqual(new List<string> { "one", "two", "three" }, result.Collections.Tags);
		CollectionAssert.AreEqual(new List<int> { 10, 20, 30 }, result.Collections.Scores);
	}

	[TestMethod]
	public void WriteShouldHandleCommaSeparatedListValues()
	{
		TestIniFileCollections instance = new()
		{
			Collections = new TestCollectionsSection
			{
				Tags = ["alpha", "beta"],
				Scores = [1, 2, 3]
			}
		};

		string result = TestIniFileCollections.Write(instance);

		Assert.Contains("Tags=alpha,beta", result);
		Assert.Contains("Scores=1,2,3", result);
	}

	[TestMethod]
	public void ReadShouldHandleArrayValues()
	{
		string content = "[Arrays]\r\nValues=1.1,2.2,3.3\r\n";
		double[] expected = [1.1, 2.2, 3.3];

		TestIniFileArrays result = TestIniFileArrays.Read(content);

		Assert.IsNotNull(result.Arrays);
		CollectionAssert.AreEqual(expected, result.Arrays.Values);
	}

	[TestMethod]
	public void WriteShouldHandleArrayValues()
	{
		TestIniFileArrays instance = new()
		{
			Arrays = new TestArraysSection { Values = [4.4, 5.5] }
		};

		string result = TestIniFileArrays.Write(instance);

		Assert.Contains("Values=4.4,5.5", result);
	}

	[TestMethod]
	public void TryReadShouldReturnTrueOnValidContent()
	{
		string content = "[General]\r\nAppName=TestApp\r\nVersion=1\r\nEnabled=True\r\n";

		bool success = TestIniFile.TryRead(content, out TestIniFile? result);

		Assert.IsTrue(success);
		Assert.IsNotNull(result);
		Assert.AreEqual("TestApp", result.General!.AppName);
	}

	[TestMethod]
	public void TryReadShouldReturnFalseOnInvalidContent()
	{
		string content = "[General]\r\nVersion=not_a_number\r\n";

		bool success = TestIniFile.TryRead(content, out TestIniFile? result);

		Assert.IsFalse(success);
		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task ReadAsyncShouldDeserializeFromTextReader()
	{
		string content = "[General]\r\nAppName=AsyncApp\r\nVersion=7\r\nEnabled=True\r\n";
		using StringReader reader = new(content);

		TestIniFile result = await TestIniFile
			.ReadAsync(reader, _testToken)
			.ConfigureAwait(false);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("AsyncApp", result.General.AppName);
		Assert.AreEqual(7, result.General.Version);
	}

	[TestMethod]
	public async Task ReadAsyncShouldDeserializeFromStream()
	{
		string content = "[General]\r\nAppName=StreamApp\r\nVersion=3\r\nEnabled=False\r\n";
		using MemoryStream stream = new(Encoding.UTF8.GetBytes(content));

		TestIniFile result = await TestIniFile
			.ReadAsync(stream, _testToken)
			.ConfigureAwait(false);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("StreamApp", result.General.AppName);
		Assert.AreEqual(3, result.General.Version);
	}

	[TestMethod]
	public async Task WriteAsyncShouldSerializeToTextWriter()
	{
		TestIniFile instance = new()
		{
			General = new TestGeneralSection { AppName = "WriterApp", Version = 5, Enabled = true }
		};
		using StringWriter writer = new();

		await TestIniFile
			.WriteAsync(instance, writer, _testToken)
			.ConfigureAwait(false);
		string result = writer.ToString();

		Assert.Contains("[General]", result);
		Assert.Contains("AppName=WriterApp", result);
	}

	[TestMethod]
	public async Task WriteAsyncShouldSerializeToStream()
	{
		TestIniFile instance = new()
		{
			General = new TestGeneralSection { AppName = "StreamWrite", Version = 9, Enabled = false }
		};
		using MemoryStream stream = new();

		await TestIniFile
			.WriteAsync(instance, stream, _testToken)
			.ConfigureAwait(false);
		stream.Position = 0;
		string result = new StreamReader(stream).ReadToEnd();

		Assert.Contains("[General]", result);
		Assert.Contains("AppName=StreamWrite", result);
	}
}

#region Test Types

[GenerateIniFile]
internal sealed partial class TestIniFile
{
	[GenerateIniFileSection]
	public TestGeneralSection General { get; set; } = new();

	[GenerateIniFileSection]
	public TestDatabaseSection Database { get; set; } = new();
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
	public TestCustomSection Stats { get; set; } = new();

	[GenerateIniFileSection]
	public TestMetricsSection Metrics { get; set; } = new();
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
	public TestAllTypesSection AllTypes { get; set; } = new();
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

[GenerateIniFile(StringComparison.Ordinal)]
internal sealed partial class TestIniFileCaseSensitive
{
	[GenerateIniFileSection]
	public TestGeneralSection General { get; set; } = new();
}

[GenerateIniFile]
internal sealed partial class TestIniFileWithNestedSection
{
	[GenerateIniFileSection]
	public TestSection Section { get; set; } = new();
}

public class TestSection
{
	[GenerateIniFileValue]
	public string? Domain { get; set; }

	[GenerateIniFileSection]
	public TestSubSection SubSection { get; set; } = new();

	[GenerateIniFileSection]
	public TestSubSection OtherSubSection { get; set; } = new();
}

public class TestSubSection
{
	[GenerateIniFileValue]
	public string? Foo { get; set; }
}

[GenerateIniFile(sectionDelimiter: "/")]
internal sealed partial class TestIniFileWithCustomDelimiter
{
	[GenerateIniFileSection]
	public TestSection Section { get; set; } = new TestSection();
}

public enum TestLogLevel { Debug, Info, Warning, Error }

[Flags]
public enum TestPermissions { None = 0, Read = 1, Write = 2, Execute = 4 }

[GenerateIniFile]
internal sealed partial class TestIniFileWithEnums
{
	[GenerateIniFileSection]
	public TestAppSection App { get; set; } = new();
}

public class TestAppSection
{
	[GenerateIniFileValue]
	public TestLogLevel Level { get; set; }
}

[GenerateIniFile]
internal sealed partial class TestIniFileWithFlagsEnum
{
	[GenerateIniFileSection]
	public TestFlagsAppSection App { get; set; } = new();
}

public class TestFlagsAppSection
{
	[GenerateIniFileValue]
	public TestPermissions Perms { get; set; }
}

[GenerateIniFile(SerializeComments = true)]
internal sealed partial class TestIniFileWithComments
{
	/// <summary>
	/// General application settings
	/// </summary>
	[GenerateIniFileSection]
	public TestCommentedGeneralSection General { get; set; } = new();
}

public class TestCommentedGeneralSection
{
	/// <summary>
	/// The display name of the application
	/// </summary>
	[GenerateIniFileValue]
	public string? AppName { get; set; }

	/// <summary>
	/// The current version number
	/// </summary>
	[GenerateIniFileValue]
	public int Version { get; set; }
}

[GenerateIniFile]
internal sealed partial class TestIniFileExtendedTypes
{
	[GenerateIniFileSection]
	public TestExtendedTypesSection Extended { get; set; } = new();
}

public class TestExtendedTypesSection
{
	[GenerateIniFileValue]
	public Guid Id { get; set; }

	[GenerateIniFileValue]
	public TimeSpan Duration { get; set; }

	[GenerateIniFileValue]
	public DateTimeOffset Timestamp { get; set; }
}

[GenerateIniFile]
internal sealed partial class TestIniFileCollections
{
	[GenerateIniFileSection]
	public TestCollectionsSection Collections { get; set; } = new();
}

public class TestCollectionsSection
{
	[GenerateIniFileValue]
	public List<string>? Tags { get; set; }

	[GenerateIniFileValue]
	public List<int>? Scores { get; set; }
}

[GenerateIniFile]
internal sealed partial class TestIniFileArrays
{
	[GenerateIniFileSection]
	public TestArraysSection Arrays { get; set; } = new();
}

public class TestArraysSection
{
	[GenerateIniFileValue]
	public double[]? Values { get; set; }
}

#endregion
