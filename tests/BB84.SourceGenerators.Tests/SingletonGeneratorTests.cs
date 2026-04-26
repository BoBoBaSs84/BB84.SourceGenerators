// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class SingletonGeneratorTests
{
	[TestMethod]
	public void InstanceShouldReturnSameInstanceLazy()
	{
		LazySingletonModel first = LazySingletonModel.Instance;
		LazySingletonModel second = LazySingletonModel.Instance;

		Assert.IsNotNull(first);
		Assert.AreSame(first, second);
	}

	[TestMethod]
	public void InstanceShouldReturnSameInstanceNonLazy()
	{
		NonLazySingletonModel first = NonLazySingletonModel.Instance;
		NonLazySingletonModel second = NonLazySingletonModel.Instance;

		Assert.IsNotNull(first);
		Assert.AreSame(first, second);
	}

	[TestMethod]
	public void InstanceShouldReturnInterfaceTypeLazy()
	{
		ISingletonService instance = InterfaceSingletonModel.Instance;

		Assert.IsNotNull(instance);
	}

	[TestMethod]
	public void InstanceShouldReturnSameInterfaceInstanceLazy()
	{
		ISingletonService first = InterfaceSingletonModel.Instance;
		ISingletonService second = InterfaceSingletonModel.Instance;

		Assert.AreSame(first, second);
	}

	[TestMethod]
	public void InstanceShouldReturnInterfaceTypeNonLazy()
	{
		ISingletonService instance = NonLazyInterfaceSingletonModel.Instance;

		Assert.IsNotNull(instance);
	}

	[TestMethod]
	public void InstanceShouldReturnSameInterfaceInstanceNonLazy()
	{
		ISingletonService first = NonLazyInterfaceSingletonModel.Instance;
		ISingletonService second = NonLazyInterfaceSingletonModel.Instance;

		Assert.AreSame(first, second);
	}

	[TestMethod]
	public void InternalSingletonShouldWork()
	{
		InternalSingletonModel first = InternalSingletonModel.Instance;
		InternalSingletonModel second = InternalSingletonModel.Instance;

		Assert.IsNotNull(first);
		Assert.AreSame(first, second);
	}
}

public interface ISingletonService
{ }

[GenerateSingleton]
public partial class LazySingletonModel
{ }

[GenerateSingleton(useLazy: false)]
public partial class NonLazySingletonModel
{ }

[GenerateSingleton]
public partial class InterfaceSingletonModel : ISingletonService
{ }

[GenerateSingleton(useLazy: false)]
public partial class NonLazyInterfaceSingletonModel : ISingletonService
{ }

[GenerateSingleton]
internal sealed partial class InternalSingletonModel
{ }
