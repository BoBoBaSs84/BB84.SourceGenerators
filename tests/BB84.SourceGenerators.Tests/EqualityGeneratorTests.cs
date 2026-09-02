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

	[TestMethod]
	public void EqualsShouldIgnoreInheritedPropertiesByDefault()
	{
		EqualityDerivedModel a = new() { BaseId = 1, Name = "John" };
		EqualityDerivedModel b = new() { BaseId = 2, Name = "John" };

		Assert.IsTrue(a.Equals(b));
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[TestMethod]
	public void EqualsShouldIncludeInheritedPropertiesWhenRequested()
	{
		EqualityInheritedModel a = new() { BaseId = 1, Name = "John" };
		EqualityInheritedModel b = new() { BaseId = 2, Name = "John" };
		EqualityInheritedModel c = new() { BaseId = 1, Name = "John" };

		Assert.IsFalse(a.Equals(b));
		Assert.IsTrue(a.Equals(c));
		Assert.AreEqual(a.GetHashCode(), c.GetHashCode());
	}

	[TestMethod]
	public void EqualsShouldExcludeInheritedPropertyByName()
	{
		EqualityInheritedExcludeModel a = new() { BaseId = 1, Name = "John" };
		EqualityInheritedExcludeModel b = new() { BaseId = 2, Name = "John" };

		Assert.IsTrue(a.Equals(b));
	}

	[TestMethod]
	public void EqualsShouldCompareCollectionContentsNotReferences()
	{
		EqualityCollectionModel a = new()
		{
			Tags = ["a", "b"],
			Codes = [1, 2, 3],
			Scores = new() { ["x"] = 1, ["y"] = 2 },
		};
		EqualityCollectionModel b = new()
		{
			Tags = ["a", "b"],
			Codes = [1, 2, 3],
			Scores = new() { ["y"] = 2, ["x"] = 1 },
		};

		Assert.IsTrue(a.Equals(b));
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[TestMethod]
	public void EqualsShouldReturnFalseForDifferentCollectionContents()
	{
		EqualityCollectionModel a = new() { Tags = ["a", "b"], Codes = [1, 2], Scores = new() };
		EqualityCollectionModel b = new() { Tags = ["a", "c"], Codes = [1, 2], Scores = new() };
		EqualityCollectionModel c = new() { Tags = ["a", "b"], Codes = [1, 2], Scores = new() { ["x"] = 1 } };

		Assert.IsFalse(a.Equals(b));
		Assert.IsFalse(a.Equals(c));
	}

	[TestMethod]
	public void EqualsShouldHandleNullCollections()
	{
		EqualityCollectionModel a = new() { Tags = null, Codes = null, Scores = null };
		EqualityCollectionModel b = new() { Tags = null, Codes = null, Scores = null };
		EqualityCollectionModel c = new() { Tags = ["a"], Codes = null, Scores = null };

		Assert.IsTrue(a.Equals(b));
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
		Assert.IsFalse(a.Equals(c));
	}
}

public abstract class EqualityModelBase
{
	public int BaseId { get; set; }
}

[GenerateEquality]
public partial class EqualityDerivedModel : EqualityModelBase
{
	public string? Name { get; set; }
}

[GenerateEquality(IncludeInherited = true)]
public partial class EqualityInheritedModel : EqualityModelBase
{
	public string? Name { get; set; }
}

[GenerateEquality(nameof(EqualityModelBase.BaseId), IncludeInherited = true)]
public partial class EqualityInheritedExcludeModel : EqualityModelBase
{
	public string? Name { get; set; }
}

[GenerateEquality]
public partial class EqualityCollectionModel
{
	public List<string>? Tags { get; set; }
	public int[]? Codes { get; set; }
	public Dictionary<string, int>? Scores { get; set; }
}

[GenerateEquality]
public partial class EqualityTestModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public double Price { get; set; }
	public bool IsActive { get; set; }
}

[GenerateEquality(nameof(Secret))]
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
