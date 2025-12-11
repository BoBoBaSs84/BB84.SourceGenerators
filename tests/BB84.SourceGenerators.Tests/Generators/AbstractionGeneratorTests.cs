using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests.Generators;

[TestClass]
public sealed class AbstractionGeneratorTests
{
	[TestMethod]
	public void TestMethod()
	{
		IFileProvider? fileProvider;

		fileProvider = new FileProvider();

		Assert.IsNotNull(fileProvider);
		Assert.IsInstanceOfType<IFileProvider>(fileProvider);
	}
}

[GenerateAbstraction(typeof(File), typeof(IFileProvider), typeof(FileProvider))]
public partial class FileProvider
{ }

public partial interface IFileProvider
{ }
