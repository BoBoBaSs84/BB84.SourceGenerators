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
	All = Read | Write | Execute
}
