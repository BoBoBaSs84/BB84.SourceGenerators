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
