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
		string[] expectedNames = [nameof(TestEnum.None), nameof(TestEnum.One), nameof(TestEnum.Two), nameof(TestEnum.Three)];

		string[] actualNames = TestEnum.None.GetNames();
		
		CollectionAssert.AreEqual(expectedNames, actualNames);
	}
}

[GenerateEnumeratorExtensions]
public enum TestEnum
{
	None = 0,
	One = 1,
	Two = 2,
	Three = 3
}
