// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class EnumeratorExtensionsGeneratorTests
{
	[TestMethod]
	public void GetNamesFastShouldReturnCorrectNames()
	{
		List<string> expectedNames = [nameof(GeneratorTestType.None), nameof(GeneratorTestType.One), nameof(GeneratorTestType.Two), nameof(GeneratorTestType.Three)];
		List<string> actualNames = [.. GeneratorTestTypeExtensions.GetNamesFast()];
		CollectionAssert.AreEqual(expectedNames, actualNames);
	}

	[TestMethod]
	public void GetValuesFastShouldReturnCorrectValues()
	{
		List<GeneratorTestType> expectedValues = [GeneratorTestType.None, GeneratorTestType.One, GeneratorTestType.Two, GeneratorTestType.Three];
		List<GeneratorTestType> actualValues = [.. GeneratorTestTypeExtensions.GetValuesFast()];
		CollectionAssert.AreEqual(expectedValues, actualValues);
	}

	[TestMethod]
	public void IsDefinedFastShouldReturnTrueForDefinedValues()
	{
		Assert.IsTrue(GeneratorTestType.None.IsDefinedFast());
		Assert.IsTrue(GeneratorTestType.One.IsDefinedFast());
		Assert.IsTrue(GeneratorTestType.Two.IsDefinedFast());
		Assert.IsTrue(GeneratorTestType.Three.IsDefinedFast());
	}

	[TestMethod]
	public void IsDefinedFastShouldReturnFalseForUndefinedValues()
	{
		Assert.IsFalse(((GeneratorTestType)999).IsDefinedFast());
		Assert.IsFalse(((GeneratorTestType)(-1)).IsDefinedFast());
	}

	[TestMethod]
	public void IsDefinedFastShouldReturnTrueForDefinedNames()
	{
		Assert.IsTrue(GeneratorTestTypeExtensions.IsDefinedFast(nameof(GeneratorTestType.None)));
		Assert.IsTrue(GeneratorTestTypeExtensions.IsDefinedFast(nameof(GeneratorTestType.One)));
		Assert.IsTrue(GeneratorTestTypeExtensions.IsDefinedFast(nameof(GeneratorTestType.Two)));
		Assert.IsTrue(GeneratorTestTypeExtensions.IsDefinedFast(nameof(GeneratorTestType.Three)));
	}

	[TestMethod]
	public void IsDefinedFastShouldReturnFalseForUndefinedNames()
	{
		Assert.IsFalse(GeneratorTestTypeExtensions.IsDefinedFast("UndefinedValue"));
		Assert.IsFalse(GeneratorTestTypeExtensions.IsDefinedFast("AnotherUndefinedValue"));
	}

	[TestMethod]
	public void ToStringFastShouldReturnCorrectStringRepresentation()
	{
		Assert.AreEqual(nameof(GeneratorTestType.None), GeneratorTestType.None.ToStringFast());
		Assert.AreEqual(nameof(GeneratorTestType.One), GeneratorTestType.One.ToStringFast());
		Assert.AreEqual(nameof(GeneratorTestType.Two), GeneratorTestType.Two.ToStringFast());
		Assert.AreEqual(nameof(GeneratorTestType.Three), GeneratorTestType.Three.ToStringFast());
	}

	[TestMethod]
	public void ToStringFastShouldReturnDefaultStringForUndefinedValues()
	{
		Assert.AreEqual("999", ((GeneratorTestType)999).ToStringFast());
		Assert.AreEqual("-1", ((GeneratorTestType)(-1)).ToStringFast());
	}

	[TestMethod]
	public void GetDescriptionFastShouldReturnCorrectDescriptions()
	{
		Assert.AreEqual("No value", GeneratorTestType.None.GetDescriptionFast());
		Assert.AreEqual("One value", GeneratorTestType.One.GetDescriptionFast());
		Assert.AreEqual("Two value", GeneratorTestType.Two.GetDescriptionFast());
		Assert.AreEqual(nameof(GeneratorTestType.Three), GeneratorTestType.Three.GetDescriptionFast());
	}

	[TestMethod]
	[DataRow(nameof(GeneratorTestType.None), GeneratorTestType.None)]
	[DataRow(nameof(GeneratorTestType.One), GeneratorTestType.One)]
	[DataRow(nameof(GeneratorTestType.Two), GeneratorTestType.Two)]
	[DataRow(nameof(GeneratorTestType.Three), GeneratorTestType.Three)]
	public void TryParseFastShouldReturnTrueForDefinedNames(string value, GeneratorTestType expected)
	{
		bool success = GeneratorTestTypeExtensions.TryParseFast(value, out GeneratorTestType result);

		Assert.IsTrue(success);
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("Undefined")]
	[DataRow("one")]
	[DataRow(" One")]
	[DataRow("1")]
	[DataRow("One, Two")]
	public void TryParseFastShouldReturnFalseForInvalidNames(string? value)
	{
		bool success = GeneratorTestTypeExtensions.TryParseFast(value, out GeneratorTestType result);

		Assert.IsFalse(success);
		Assert.AreEqual(default, result);
	}

	[TestMethod]
	public void TryParseFastShouldRejectCombinationsForFlagsWithoutSeparators()
	{
		bool success = GeneratorTestFlagsExtensions.TryParseFast("Read, Write", out GeneratorTestFlags result);

		Assert.IsFalse(success);
		Assert.AreEqual(GeneratorTestFlags.None, result);
	}

	[TestMethod]
	[DataRow("Read|Write", "|", GeneratorTestFlags.Read | GeneratorTestFlags.Write)]
	[DataRow("Read,Execute", ",", GeneratorTestFlags.Read | GeneratorTestFlags.Execute)]
	[DataRow("Write;Execute", ";", GeneratorTestFlags.Write | GeneratorTestFlags.Execute)]
	[DataRow("Read Write Execute", " ", GeneratorTestFlags.All)]
	[DataRow("Read, Write", ",", GeneratorTestFlags.Read | GeneratorTestFlags.Write)]
	[DataRow("Read, ,Write", ",", GeneratorTestFlags.Read | GeneratorTestFlags.Write)]
	[DataRow("Read||Write|", "|", GeneratorTestFlags.Read | GeneratorTestFlags.Write)]
	[DataRow("Read|Read", "|", GeneratorTestFlags.Read)]
	[DataRow("All", "|", GeneratorTestFlags.All)]
	[DataRow("None", "|", GeneratorTestFlags.None)]
	[DataRow("Write", "|", GeneratorTestFlags.Write)]
	public void TryParseFastShouldCombineFlagsForSeparator(string value, string separator, GeneratorTestFlags expected)
	{
		bool success = GeneratorTestFlagsExtensions.TryParseFast(value, out GeneratorTestFlags result, separator);

		Assert.IsTrue(success);
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void TryParseFastShouldCombineFlagsForMultipleSeparators()
	{
		bool success = GeneratorTestFlagsExtensions.TryParseFast("Read | Write; Execute", out GeneratorTestFlags result, "|", ",", ";", " ");

		Assert.IsTrue(success);
		Assert.AreEqual(GeneratorTestFlags.All, result);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow(" | ")]
	[DataRow("Read|Unknown")]
	[DataRow("Read|1")]
	[DataRow("Read|write")]
	public void TryParseFastShouldReturnFalseForInvalidFlags(string? value)
	{
		bool success = GeneratorTestFlagsExtensions.TryParseFast(value, out GeneratorTestFlags result, "|");

		Assert.IsFalse(success);
		Assert.AreEqual(GeneratorTestFlags.None, result);
	}

	[TestMethod]
	public void TryParseFastShouldFallBackToExactNameWithoutSeparators()
	{
		Assert.IsTrue(GeneratorTestFlagsExtensions.TryParseFast("Write", out GeneratorTestFlags single, []));
		Assert.AreEqual(GeneratorTestFlags.Write, single);

		Assert.IsFalse(GeneratorTestFlagsExtensions.TryParseFast("Read|Write", out GeneratorTestFlags combined, null!));
		Assert.AreEqual(GeneratorTestFlags.None, combined);
	}

	[TestMethod]
	[DataRow("One", false, false, GeneratorTestType.One)]
	[DataRow(" Two ", false, false, GeneratorTestType.Two)]
	[DataRow("one", true, false, GeneratorTestType.One)]
	[DataRow(" tHREE ", true, false, GeneratorTestType.Three)]
	[DataRow("1", false, true, GeneratorTestType.One)]
	[DataRow(" 3 ", true, true, GeneratorTestType.Three)]
	[DataRow("0", true, true, GeneratorTestType.None)]
	public void TryParseFastWithOptionsShouldReturnTrueForValidValues(string value, bool ignoreCase, bool allowNumeric, GeneratorTestType expected)
	{
		bool success = GeneratorTestTypeExtensions.TryParseFast(value, ignoreCase, allowNumeric, out GeneratorTestType result);

		Assert.IsTrue(success);
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	[DataRow(null, true, true)]
	[DataRow("", true, true)]
	[DataRow("   ", true, true)]
	[DataRow("one", false, true)]
	[DataRow("1", true, false)]
	[DataRow("42", true, true)]
	[DataRow("-1", true, true)]
	[DataRow("1.0", true, true)]
	[DataRow("Undefined", true, true)]
	[DataRow("One, Two", true, true)]
	public void TryParseFastWithOptionsShouldReturnFalseForInvalidValues(string? value, bool ignoreCase, bool allowNumeric)
	{
		bool success = GeneratorTestTypeExtensions.TryParseFast(value, ignoreCase, allowNumeric, out GeneratorTestType result);

		Assert.IsFalse(success);
		Assert.AreEqual(default, result);
	}

	[TestMethod]
	[DataRow("3", GeneratorTestFlags.Read | GeneratorTestFlags.Write)]
	[DataRow("7", GeneratorTestFlags.All)]
	[DataRow("0", GeneratorTestFlags.None)]
	[DataRow("all", GeneratorTestFlags.All)]
	public void TryParseFastWithOptionsShouldAcceptDefinedBitsForFlags(string value, GeneratorTestFlags expected)
	{
		bool success = GeneratorTestFlagsExtensions.TryParseFast(value, true, true, out GeneratorTestFlags result);

		Assert.IsTrue(success);
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	[DataRow("8")]
	[DataRow("15")]
	[DataRow("read, write")]
	public void TryParseFastWithOptionsShouldRejectUndefinedBitsAndListsForFlags(string value)
	{
		bool success = GeneratorTestFlagsExtensions.TryParseFast(value, true, true, out GeneratorTestFlags result);

		Assert.IsFalse(success);
		Assert.AreEqual(GeneratorTestFlags.None, result);
	}

	[TestMethod]
	[DataRow("read, write", true, false, GeneratorTestFlags.Read | GeneratorTestFlags.Write)]
	[DataRow(" Read ,Execute ", false, false, GeneratorTestFlags.Read | GeneratorTestFlags.Execute)]
	[DataRow("1, 2", false, true, GeneratorTestFlags.Read | GeneratorTestFlags.Write)]
	[DataRow("3, execute", true, true, GeneratorTestFlags.All)]
	[DataRow("6", false, true, GeneratorTestFlags.Write | GeneratorTestFlags.Execute)]
	public void TryParseFastWithOptionsShouldCombineFlagsForSeparator(string value, bool ignoreCase, bool allowNumeric, GeneratorTestFlags expected)
	{
		bool success = GeneratorTestFlagsExtensions.TryParseFast(value, ignoreCase, allowNumeric, out GeneratorTestFlags result, ",");

		Assert.IsTrue(success);
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	[DataRow(null, true, true)]
	[DataRow(" , ", true, true)]
	[DataRow("read, write", false, true)]
	[DataRow("Read, 2", true, false)]
	[DataRow("Read, 8", true, true)]
	[DataRow("Read, Unknown", true, true)]
	public void TryParseFastWithOptionsShouldReturnFalseForInvalidFlags(string? value, bool ignoreCase, bool allowNumeric)
	{
		bool success = GeneratorTestFlagsExtensions.TryParseFast(value, ignoreCase, allowNumeric, out GeneratorTestFlags result, ",");

		Assert.IsFalse(success);
		Assert.AreEqual(GeneratorTestFlags.None, result);
	}

	[TestMethod]
	public void TryParseFastWithOptionsShouldFallBackToSingleValueWithoutSeparators()
	{
		Assert.IsTrue(GeneratorTestFlagsExtensions.TryParseFast(" write ", true, true, out GeneratorTestFlags single, []));
		Assert.AreEqual(GeneratorTestFlags.Write, single);

		Assert.IsFalse(GeneratorTestFlagsExtensions.TryParseFast("Read,Write", true, true, out GeneratorTestFlags combined, null!));
		Assert.AreEqual(GeneratorTestFlags.None, combined);
	}

	[TestMethod]
	public void TryParseFastWithOptionsShouldParseAliasNames()
	{
		Assert.IsTrue(GeneratorTestAliasTypeExtensions.TryParseFast("enabled", true, false, out GeneratorTestAliasType result));
		Assert.AreEqual(GeneratorTestAliasType.Active, result);
	}

	[TestMethod]
	[DataRow(GeneratorTestFlags.None, nameof(GeneratorTestFlags.None))]
	[DataRow(GeneratorTestFlags.Read, nameof(GeneratorTestFlags.Read))]
	[DataRow(GeneratorTestFlags.All, nameof(GeneratorTestFlags.All))]
	[DataRow(GeneratorTestFlags.High, nameof(GeneratorTestFlags.High))]
	public void ToStringFastWithSeparatorShouldReturnMemberNameForDefinedValues(GeneratorTestFlags value, string expected)
		=> Assert.AreEqual(expected, value.ToStringFast("|"));

	[TestMethod]
	[DataRow(GeneratorTestFlags.Read | GeneratorTestFlags.Write, "|", "Read|Write")]
	[DataRow(GeneratorTestFlags.Write | GeneratorTestFlags.Execute, ",", "Write,Execute")]
	[DataRow(GeneratorTestFlags.Execute | GeneratorTestFlags.Read, ";", "Read;Execute")]
	[DataRow(GeneratorTestFlags.Read | GeneratorTestFlags.High, " ", "Read High")]
	[DataRow(GeneratorTestFlags.All | GeneratorTestFlags.High, ", ", "Read, Write, Execute, High")]
	public void ToStringFastWithSeparatorShouldJoinSingleBitMembers(GeneratorTestFlags value, string separator, string expected)
		=> Assert.AreEqual(expected, value.ToStringFast(separator));

	[TestMethod]
	public void ToStringFastWithSeparatorShouldFallBackForUncoveredBits()
	{
		Assert.AreEqual("8", ((GeneratorTestFlags)8).ToStringFast("|"));
		Assert.AreEqual("9", (GeneratorTestFlags.Read | (GeneratorTestFlags)8).ToStringFast("|"));
	}

	[TestMethod]
	[DataRow("|")]
	[DataRow(",")]
	[DataRow(";")]
	[DataRow(" ")]
	[DataRow(", ")]
	public void ToStringFastWithSeparatorShouldRoundTripWithTryParseFast(string separator)
	{
		GeneratorTestFlags[] bits = [GeneratorTestFlags.Read, GeneratorTestFlags.Write, GeneratorTestFlags.Execute, GeneratorTestFlags.High];

		for (int mask = 0; mask < 1 << bits.Length; mask++)
		{
			GeneratorTestFlags value = GeneratorTestFlags.None;
			for (int i = 0; i < bits.Length; i++)
			{
				if ((mask & (1 << i)) != 0)
					value |= bits[i];
			}

			string text = value.ToStringFast(separator);

			Assert.IsTrue(GeneratorTestFlagsExtensions.TryParseFast(text, out GeneratorTestFlags parsed, separator), text);
			Assert.AreEqual(value, parsed, text);
		}
	}

	[TestMethod]
	public void ToStringFastShouldReturnFirstDeclaredNameForAliases()
	{
		Assert.AreEqual(nameof(GeneratorTestAliasType.Active), GeneratorTestAliasType.Active.ToStringFast());
		Assert.AreEqual(nameof(GeneratorTestAliasType.Active), GeneratorTestAliasType.Enabled.ToStringFast());
		Assert.AreEqual(nameof(GeneratorTestAliasType.Inactive), GeneratorTestAliasType.Inactive.ToStringFast());
	}

	[TestMethod]
	public void GetDescriptionFastShouldReturnFirstDeclaredDescriptionForAliases()
	{
		Assert.AreEqual("Active value", GeneratorTestAliasType.Active.GetDescriptionFast());
		Assert.AreEqual("Active value", GeneratorTestAliasType.Enabled.GetDescriptionFast());
		Assert.AreEqual(nameof(GeneratorTestAliasType.Inactive), GeneratorTestAliasType.Inactive.GetDescriptionFast());
	}

	[TestMethod]
	public void IsDefinedFastShouldReturnTrueForAliases()
	{
		Assert.IsTrue(GeneratorTestAliasType.Enabled.IsDefinedFast());
		Assert.IsTrue(GeneratorTestAliasTypeExtensions.IsDefinedFast(nameof(GeneratorTestAliasType.Enabled)));
		Assert.IsFalse(((GeneratorTestAliasType)3).IsDefinedFast());
	}

	[TestMethod]
	public void TryParseFastShouldParseAliasNames()
	{
		bool success = GeneratorTestAliasTypeExtensions.TryParseFast(nameof(GeneratorTestAliasType.Enabled), out GeneratorTestAliasType result);

		Assert.IsTrue(success);
		Assert.AreEqual(GeneratorTestAliasType.Active, result);
	}

	[TestMethod]
	public void GetNamesAndValuesFastShouldKeepAliases()
	{
		List<string> expectedNames = [nameof(GeneratorTestAliasType.Active), nameof(GeneratorTestAliasType.Enabled), nameof(GeneratorTestAliasType.Inactive)];
		List<GeneratorTestAliasType> expectedValues = [GeneratorTestAliasType.Active, GeneratorTestAliasType.Active, GeneratorTestAliasType.Inactive];

		CollectionAssert.AreEqual(expectedNames, GeneratorTestAliasTypeExtensions.GetNamesFast().ToList());
		CollectionAssert.AreEqual(expectedValues, GeneratorTestAliasTypeExtensions.GetValuesFast().ToList());
	}
}

[GenerateEnumeratorExtensions]
public enum GeneratorTestType
{
	[System.ComponentModel.Description("No value")]
	None = 0,
	[DescriptionAttribute("One value")]
	One = 1,
	[Description("Two value")]
	Two = 2,
	Three = 3
}

[Flags]
[GenerateEnumeratorExtensions]
public enum GeneratorTestFlags : long
{
	None = 0,
	Read = 1,
	Write = 2,
	Execute = 4,
	All = Read | Write | Execute,
	High = long.MinValue
}

[GenerateEnumeratorExtensions]
public enum GeneratorTestAliasType
{
	[Description("Active value")]
	Active = 1,
	[Description("Enabled value")]
	Enabled = Active,
	Inactive = 2
}
