using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests.Generators;

[TestClass]
public sealed class EnumExtensionsGeneratorTests
{
	[TestMethod]
	public void InitializeTest()
	{
		string[] val = TestEnum.None.GetNames();
	}
}

[GenerateEnumExtensions]
public enum TestEnum
{
	None = 0,
	One = 1,
	Two = 2,
	Three = 3
}
