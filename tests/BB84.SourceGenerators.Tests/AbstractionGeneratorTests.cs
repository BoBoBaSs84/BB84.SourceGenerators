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

	[TestMethod]
	public void ExcludeMethodsAndPropertiesTest()
	{
		IFilteredEnvironmentProvider? provider;

		provider = new FilteredEnvironmentProvider();

		Assert.IsNotNull(provider);
		Assert.IsInstanceOfType<IFilteredEnvironmentProvider>(provider);
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

[GenerateAbstraction(typeof(Path), typeof(IPathProvider), typeof(PathProvider), ExcludeProperties = new string[] {
#if NET5_0_OR_GREATER
	nameof(Path.EndsInDirectorySeparator)
#else	
	"EndsInDirectorySeparator"
#endif
})]
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

[GenerateAbstraction(typeof(Environment), typeof(IFilteredEnvironmentProvider), typeof(FilteredEnvironmentProvider),
	ExcludeMethods = [nameof(Environment.Exit), "FailFast"],
	ExcludeProperties = [nameof(Environment.ExitCode), "StackTrace"])]
internal sealed partial class FilteredEnvironmentProvider
{ }

public partial interface IFilteredEnvironmentProvider
{ }
