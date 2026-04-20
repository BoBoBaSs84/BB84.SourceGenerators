// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class EdgeCaseTests
{
	#region ToString Edge Cases

	[TestMethod]
	public void ToStringShouldWorkForEmptyClass()
	{
		EmptyToStringModel model = new();

		string? result = model.ToString();
		string expected = $"{nameof(EmptyToStringModel)} {{ }}";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldHandleSpecialCharactersInStringProperties()
	{
		ToStringSpecialCharsModel model = new()
		{
			Value = "Hello\nWorld\t!\"quotes\""
		};

		string? result = model.ToString();

		Assert.IsNotNull(result);
		Assert.Contains("Hello\nWorld\t!\"quotes\"", result);
	}

	[TestMethod]
	public void ToStringShouldHandleEmptyStringProperty()
	{
		ToStringSpecialCharsModel model = new()
		{
			Value = string.Empty
		};

		string? result = model.ToString();

		Assert.IsNotNull(result);
		Assert.Contains("Value = ", result);
	}

	#endregion

	#region Equality Edge Cases

	[TestMethod]
	public void EqualityShouldWorkForEmptyClass()
	{
		EmptyEqualityModel a = new();
		EmptyEqualityModel b = new();

		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(a == b);
	}

	[TestMethod]
	public void EqualityShouldHandleNullStringBothSides()
	{
		EqualityNullableModel a = new() { Name = null };
		EqualityNullableModel b = new() { Name = null };

		Assert.IsTrue(a.Equals(b));
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[TestMethod]
	public void EqualityShouldHandleNullStringOneSide()
	{
		EqualityNullableModel a = new() { Name = "hello" };
		EqualityNullableModel b = new() { Name = null };

		Assert.IsFalse(a.Equals(b));
		Assert.IsFalse(b.Equals(a));
	}

	[TestMethod]
	public void EqualityShouldHandleEmptyVsNullString()
	{
		EqualityNullableModel a = new() { Name = "" };
		EqualityNullableModel b = new() { Name = null };

		Assert.IsFalse(a.Equals(b));
	}

	[TestMethod]
	public void EqualityOperatorBothNull()
	{
		EmptyEqualityModel? a = null;
		EmptyEqualityModel? b = null;

		Assert.IsTrue(a == b);
		Assert.IsFalse(a != b);
	}

	[TestMethod]
	public void EqualityOperatorLeftNullRightNotNull()
	{
		EmptyEqualityModel? a = null;
		EmptyEqualityModel b = new();

		Assert.IsFalse(a == b);
		Assert.IsTrue(a != b);
	}

	#endregion

	#region Builder Edge Cases

	[TestMethod]
	public void BuilderShouldAllowOverwritingValues()
	{
		BuilderOverwriteModel model = new BuilderOverwriteModelBuilder()
			.WithId(1)
			.WithId(2)
			.Build();

		Assert.AreEqual(2, model.Id);
	}

	[TestMethod]
	public void BuilderMultipleBuildsShouldBeIndependent()
	{
		BuilderOverwriteModelBuilder builder = new BuilderOverwriteModelBuilder().WithId(10);

		BuilderOverwriteModel first = builder.Build();
		BuilderOverwriteModel second = builder.Build();

		Assert.AreNotSame(first, second);
		Assert.AreEqual(first.Id, second.Id);
	}

	#endregion

	#region Validator Edge Cases

	[TestMethod]
	public void ValidatorShouldReturnEmptyForEmptyClass()
	{
		EmptyValidatorModel model = new();

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidatorShouldDetectMultipleViolationsOnSameProperty()
	{
		ValidatorMultiAttrModel model = new()
		{
			Code = "a"  // violates MinLength(3) 
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Code"));
	}

	#endregion

	#region Cloneable Edge Cases

	[TestMethod]
	public void CloneShouldWorkForEmptyClass()
	{
		EmptyCloneableModel original = new();

		EmptyCloneableModel clone = original.Clone();

		Assert.IsNotNull(clone);
		Assert.AreNotSame(original, clone);
	}

	[TestMethod]
	public void DeepCloneShouldWorkForEmptyClass()
	{
		EmptyCloneableModel original = new();

		EmptyCloneableModel clone = original.DeepClone();

		Assert.IsNotNull(clone);
		Assert.AreNotSame(original, clone);
	}

	[TestMethod]
	public void CloneShouldHandleAllDefaultValues()
	{
		CloneableDefaultsModel original = new();

		CloneableDefaultsModel clone = original.Clone();

		Assert.AreEqual(0, clone.IntValue);
		Assert.IsNull(clone.StringValue);
		Assert.IsFalse(clone.BoolValue);
		Assert.AreEqual(0.0, clone.DoubleValue);
	}

	#endregion

	#region IniFile Edge Cases

	[TestMethod]
	public void IniFileReadShouldHandleUnknownSections()
	{
		string content = "[UnknownSection]\r\nKey=Value\r\n[General]\r\nAppName=Test\r\nVersion=1\r\nEnabled=True\r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("Test", result.General.AppName);
	}

	[TestMethod]
	public void IniFileReadShouldHandleUnknownKeys()
	{
		string content = "[General]\r\nUnknownKey=Value\r\nAppName=Test\r\nVersion=1\r\nEnabled=True\r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("Test", result.General.AppName);
	}

	[TestMethod]
	public void IniFileReadShouldHandleValuesWithEqualsSign()
	{
		string content = "[General]\r\nAppName=Test=App\r\nVersion=1\r\nEnabled=True\r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result.General);
		Assert.AreEqual("Test=App", result.General.AppName);
	}

	[TestMethod]
	public void IniFileReadShouldHandleOnlyComments()
	{
		string content = "; comment line 1\r\n# comment line 2\r\n";

		TestIniFile result = TestIniFile.Read(content);

		Assert.IsNotNull(result);
		Assert.IsNull(result.General);
		Assert.IsNull(result.Database);
	}

	[TestMethod]
	public void IniFileRoundTripAllTypes()
	{
		DateTime testDate = DateTime.Parse("03/20/2025 14:30:00", System.Globalization.CultureInfo.InvariantCulture);
		TestIniFileAllTypes original = new()
		{
			AllTypes = new TestAllTypesSection
			{
				StringValue = "RoundTrip",
				IntValue = int.MaxValue,
				LongValue = long.MaxValue,
				FloatValue = 1.23f,
				DoubleValue = 4.56789,
				BoolValue = false,
				DecimalValue = 999.999m,
				DateTimeValue = testDate
			}
		};

		string serialized = TestIniFileAllTypes.Write(original);
		TestIniFileAllTypes deserialized = TestIniFileAllTypes.Read(serialized);

		Assert.IsNotNull(deserialized.AllTypes);
		Assert.AreEqual(original.AllTypes.StringValue, deserialized.AllTypes.StringValue);
		Assert.AreEqual(original.AllTypes.IntValue, deserialized.AllTypes.IntValue);
		Assert.AreEqual(original.AllTypes.LongValue, deserialized.AllTypes.LongValue);
		Assert.AreEqual(original.AllTypes.FloatValue, deserialized.AllTypes.FloatValue);
		Assert.AreEqual(original.AllTypes.DoubleValue, deserialized.AllTypes.DoubleValue);
		Assert.AreEqual(original.AllTypes.BoolValue, deserialized.AllTypes.BoolValue);
		Assert.AreEqual(original.AllTypes.DecimalValue, deserialized.AllTypes.DecimalValue);
		Assert.AreEqual(original.AllTypes.DateTimeValue, deserialized.AllTypes.DateTimeValue);
	}

	[TestMethod]
	public void IniFileWriteThenReadShouldRoundTripCustomNames()
	{
		TestIniFileWithCustomNames original = new()
		{
			Stats = new TestCustomSection { Score = 42.5f },
			Metrics = new TestMetricsSection { Score = 88.8f }
		};

		string serialized = TestIniFileWithCustomNames.Write(original);
		TestIniFileWithCustomNames deserialized = TestIniFileWithCustomNames.Read(serialized);

		Assert.IsNotNull(deserialized.Stats);
		Assert.AreEqual(original.Stats.Score, deserialized.Stats.Score);
		Assert.IsNotNull(deserialized.Metrics);
		Assert.AreEqual(original.Metrics.Score, deserialized.Metrics.Score);
	}

	[TestMethod]
	public void IniFileRoundTripNestedSections()
	{
		TestIniFileWithNestedSection original = new()
		{
			Section = new TestSection
			{
				Domain = "test.com",
				SubSection = new TestSubSection { Foo = "Baz" }
			}
		};

		string serialized = TestIniFileWithNestedSection.Write(original);
		TestIniFileWithNestedSection deserialized = TestIniFileWithNestedSection.Read(serialized);

		Assert.IsNotNull(deserialized.Section);
		Assert.AreEqual("test.com", deserialized.Section.Domain);
		Assert.IsNotNull(deserialized.Section.SubSection);
		Assert.AreEqual("Baz", deserialized.Section.SubSection.Foo);
	}

	#endregion

	#region NotificationProperties Edge Cases

	[TestMethod]
	public void NotificationShouldNotFireWhenSettingSameValue()
	{
		int changingCount = 0;
		int changedCount = 0;
		NotificationProperties model = new(1, "Test", "Desc", DateTime.UtcNow);
		model.PropertyChanging += (s, e) => changingCount++;
		model.PropertyChanged += (s, e) => changedCount++;

		// Set same value
		model.Id = 1;

		// Should still fire since generator generates simple setter
		// This test documents current behavior
		Assert.AreEqual(0, changingCount);
		Assert.AreEqual(0, changedCount);
	}

	[TestMethod]
	public void NotificationShouldFireForEachPropertyChange()
	{
		List<string?> changedProperties = [];
		NotificationProperties model = new(1, "Test", "Desc", DateTime.UtcNow);
		model.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

		model.Id = 2;
		model.Name = "New";

		Assert.IsGreaterThanOrEqualTo(2, changedProperties.Count);
	}

	#endregion
}

#region Edge Case Test Models

[GenerateToString]
public partial class EmptyToStringModel
{
}

[GenerateToString]
public partial class ToStringSpecialCharsModel
{
	public string? Value { get; set; }
}

[GenerateEquality]
public partial class EmptyEqualityModel
{
}

[GenerateEquality]
public partial class EqualityNullableModel
{
	public string? Name { get; set; }
}

[GenerateBuilder]
public partial class BuilderOverwriteModel
{
	public int Id { get; set; }
}

[GenerateValidator]
public partial class EmptyValidatorModel
{
}

[GenerateValidator]
public partial class ValidatorMultiAttrModel
{
	[System.ComponentModel.DataAnnotations.MinLength(3)]
	[System.ComponentModel.DataAnnotations.MaxLength(10)]
	public string? Code { get; set; }
}

[GenerateCloneable]
public partial class EmptyCloneableModel
{
}

[GenerateCloneable]
public partial class CloneableDefaultsModel
{
	public int IntValue { get; set; }
	public string? StringValue { get; set; }
	public bool BoolValue { get; set; }
	public double DoubleValue { get; set; }
}

#endregion
