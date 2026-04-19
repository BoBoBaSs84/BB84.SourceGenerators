// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class EqualityGeneratorTests
{
	[TestMethod]
	public void EqualsShouldReturnTrueForEqualInstances()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };
		EqualityTestModel b = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(b.Equals(a));
	}

	[TestMethod]
	public void EqualsShouldReturnFalseForDifferentInstances()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };
		EqualityTestModel b = new() { Id = 2, Name = "Jane", Price = 19.99, IsActive = false };

		Assert.IsFalse(a.Equals(b));
		Assert.IsFalse(b.Equals(a));
	}

	[TestMethod]
	public void EqualsShouldReturnFalseForNull()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

		Assert.IsFalse(a.Equals(null));
	}

	[TestMethod]
	public void EqualsShouldReturnTrueForSameReference()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

		Assert.IsTrue(a.Equals(a));
	}

	[TestMethod]
	public void EqualsObjectShouldReturnTrueForEqualInstances()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };
		object b = new EqualityTestModel() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

		Assert.IsTrue(a.Equals(b));
	}

	[TestMethod]
	public void EqualsObjectShouldReturnFalseForDifferentType()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };
		object b = "not an EqualityTestModel";

		Assert.IsFalse(a.Equals(b));
	}

	[TestMethod]
	public void GetHashCodeShouldBeEqualForEqualInstances()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };
		EqualityTestModel b = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[TestMethod]
	public void GetHashCodeShouldDifferForDifferentInstances()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };
		EqualityTestModel b = new() { Id = 2, Name = "Jane", Price = 19.99, IsActive = false };

		Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
	}

	[TestMethod]
	public void OperatorEqualsShouldReturnTrueForEqualInstances()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };
		EqualityTestModel b = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

		Assert.IsTrue(a == b);
	}

	[TestMethod]
	public void OperatorNotEqualsShouldReturnTrueForDifferentInstances()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };
		EqualityTestModel b = new() { Id = 2, Name = "Jane", Price = 19.99, IsActive = false };

		Assert.IsTrue(a != b);
	}

	[TestMethod]
	public void OperatorEqualsShouldHandleNulls()
	{
		EqualityTestModel? a = null;
		EqualityTestModel? b = null;

		Assert.IsTrue(a == b);
	}

	[TestMethod]
	public void OperatorEqualsShouldReturnFalseForNullAndNonNull()
	{
		EqualityTestModel a = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };
		EqualityTestModel? b = null;

		Assert.IsFalse(a == b);
		Assert.IsTrue(a != b);
	}

	[TestMethod]
	public void EqualsShouldHandleNullPropertyValues()
	{
		EqualityTestModel a = new() { Id = 1, Name = null, Price = 0.0, IsActive = false };
		EqualityTestModel b = new() { Id = 1, Name = null, Price = 0.0, IsActive = false };

		Assert.IsTrue(a.Equals(b));
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[TestMethod]
	public void EqualsShouldExcludeSpecifiedProperties()
	{
		EqualityExcludeTestModel a = new() { Id = 1, Name = "John", Secret = "abc" };
		EqualityExcludeTestModel b = new() { Id = 1, Name = "John", Secret = "xyz" };

		Assert.IsTrue(a.Equals(b));
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[TestMethod]
	public void EqualsShouldReturnTrueWhenAllPropertiesExcluded()
	{
		EqualityAllExcludedTestModel a = new() { Value = "abc" };
		EqualityAllExcludedTestModel b = new() { Value = "xyz" };

		Assert.IsTrue(a.Equals(b));
	}

	[TestMethod]
	public void ImplementsIEquatable()
	{
		EqualityTestModel model = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

		Assert.IsInstanceOfType<IEquatable<EqualityTestModel>>(model);
	}
}

[GenerateEquality]
public partial class EqualityTestModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public double Price { get; set; }
	public bool IsActive { get; set; }
}

[GenerateEquality("Secret")]
public partial class EqualityExcludeTestModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? Secret { get; set; }
}

[GenerateEquality("Value")]
public partial class EqualityAllExcludedTestModel
{
	public string? Value { get; set; }
}
