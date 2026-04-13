// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
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
		IDirectoryProvider? provider;

		provider = new DirectoryProvider();

		Assert.IsNotNull(provider);
		Assert.IsInstanceOfType<IDirectoryProvider>(provider);

		string currentDirectory = provider.GetCurrentDirectory();
		Assert.IsNotNull(currentDirectory);
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

[GenerateAbstraction(typeof(Directory), typeof(IDirectoryProvider), typeof(DirectoryProvider), ExcludeProperties = new string[] { "Dummy" })]
internal sealed partial class DirectoryProvider
{ }

public partial interface IDirectoryProvider
{ }
