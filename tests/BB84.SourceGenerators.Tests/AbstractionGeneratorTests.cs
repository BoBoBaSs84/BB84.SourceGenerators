// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

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

	[TestMethod]
	public void PropertyGenerationTest()
	{
		IEnvironmentProvider? provider;

		provider = new EnvironmentProvider();

		Assert.IsNotNull(provider);
		Assert.IsInstanceOfType<IEnvironmentProvider>(provider);

		string machineName = provider.MachineName;
		Assert.IsNotNull(machineName);
		Assert.AreEqual(Environment.MachineName, machineName);
	}

	[TestMethod]
	public void ExcludePropertiesTest()
	{
		IPathProvider? provider;

		provider = new PathProvider();

		Assert.IsNotNull(provider);
		Assert.IsInstanceOfType<IPathProvider>(provider);

		string randomFileName = provider.GetRandomFileName();
		Assert.IsNotNull(randomFileName);
	}

	[TestMethod]
	public void OutParameterTest()
	{
		IOutParamProvider provider = new OutParamProvider();

		bool result = provider.TryParse("42", out int value);

		Assert.IsTrue(result);
		Assert.AreEqual(42, value);
	}
}

[GenerateAbstraction(typeof(File), typeof(IFileProvider), typeof(FileProvider))]
internal sealed partial class FileProvider
{ }

public partial interface IFileProvider
{ }

[GenerateAbstraction(typeof(Environment), typeof(IEnvironmentProvider), typeof(EnvironmentProvider))]
internal sealed partial class EnvironmentProvider
{ }

public partial interface IEnvironmentProvider
{ }

[GenerateAbstraction(typeof(Path), typeof(IPathProvider), typeof(PathProvider), ExcludeProperties = new string[] { nameof(Path.EndsInDirectorySeparator) })]
internal sealed partial class PathProvider
{ }

public partial interface IPathProvider
{ }

public static class OutParamHelper
{
	public static bool TryParse(string input, out int result)
		=> int.TryParse(input, out result);
}

[GenerateAbstraction(typeof(OutParamHelper), typeof(IOutParamProvider), typeof(OutParamProvider))]
internal sealed partial class OutParamProvider
{ }

public partial interface IOutParamProvider
{ }
