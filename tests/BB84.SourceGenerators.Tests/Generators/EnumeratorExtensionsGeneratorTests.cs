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
	public void GetNamesShouldReturnCorrectNames()
	{
		string[] expectedNames = [nameof(GeneratorTestType.None), nameof(GeneratorTestType.One), nameof(GeneratorTestType.Two), nameof(GeneratorTestType.Three)];

		string[] actualNames = GeneratorTestType.None.GetNames();
		
		CollectionAssert.AreEqual(expectedNames, actualNames);
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
