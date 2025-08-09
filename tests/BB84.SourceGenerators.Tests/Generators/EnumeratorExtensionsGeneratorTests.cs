// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests.Generators;

[TestClass]
public sealed class EnumeratorExtensionsGeneratorTests
{
	[TestMethod]
	public void GetNamesFastShouldReturnCorrectNames()
	{
		List<string> expectedNames = [nameof(GeneratorTestType.None), nameof(GeneratorTestType.One), nameof(GeneratorTestType.Two), nameof(GeneratorTestType.Three)];
		List<string> actualNames = [.. GeneratorTestType.None.GetNamesFast()];
		CollectionAssert.AreEqual(expectedNames, actualNames);
	}

	[TestMethod]
	public void GetValuesFastShouldReturnCorrectValues()
	{
		List<GeneratorTestType> expectedValues = [GeneratorTestType.None, GeneratorTestType.One, GeneratorTestType.Two, GeneratorTestType.Three];
		List<GeneratorTestType> actualValues = [.. GeneratorTestType.None.GetValuesFast()];
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
		Assert.IsTrue(nameof(GeneratorTestType.None).IsDefinedFast());
		Assert.IsTrue(nameof(GeneratorTestType.One).IsDefinedFast());
		Assert.IsTrue(nameof(GeneratorTestType.Two).IsDefinedFast());
		Assert.IsTrue(nameof(GeneratorTestType.Three).IsDefinedFast());
	}

	[TestMethod]
	public void IsDefinedFastShouldReturnFalseForUndefinedNames()
	{
		Assert.IsFalse("UndefinedValue".IsDefinedFast());
		Assert.IsFalse("AnotherUndefinedValue".IsDefinedFast());
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
}

[GenerateEnumeratorExtensions]
public enum GeneratorTestType
{
	None = 0,
	One = 1,
	Two = 2,
	Three = 3
}
