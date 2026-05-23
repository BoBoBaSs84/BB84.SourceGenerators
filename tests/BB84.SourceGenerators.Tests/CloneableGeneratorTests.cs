// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;

using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class CloneableGeneratorTests
{
	[TestMethod]
	public void CloneShouldCreateShallowCopy()
	{
		CloneableTestModel original = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

		CloneableTestModel clone = original.Clone();

		Assert.AreNotSame(original, clone);
		Assert.AreEqual(original.Id, clone.Id);
		Assert.AreEqual(original.Name, clone.Name);
		Assert.AreEqual(original.Price, clone.Price);
		Assert.AreEqual(original.IsActive, clone.IsActive);
	}

	[TestMethod]
	public void CloneShouldShareReferenceTypeProperties()
	{
		CloneableNestedModel nested = new() { Value = "nested" };
		CloneableParentModel original = new() { Id = 1, Nested = nested };

		CloneableParentModel clone = original.Clone();

		Assert.AreNotSame(original, clone);
		Assert.AreSame(original.Nested, clone.Nested);
	}

	[TestMethod]
	public void DeepCloneShouldCreateDeepCopy()
	{
		CloneableNestedModel nested = new() { Value = "nested" };
		CloneableParentModel original = new() { Id = 1, Nested = nested };

		CloneableParentModel clone = original.DeepClone();

		Assert.AreNotSame(original, clone);
		Assert.AreNotSame(original.Nested, clone.Nested);
		Assert.AreEqual(original.Id, clone.Id);
		Assert.AreEqual(original.Nested.Value, clone.Nested?.Value);
	}

	[TestMethod]
	public void DeepCloneShouldHandleNullReferenceProperties()
	{
		CloneableParentModel original = new() { Id = 1, Nested = null };

		CloneableParentModel clone = original.DeepClone();

		Assert.AreNotSame(original, clone);
		Assert.AreEqual(original.Id, clone.Id);
		Assert.IsNull(clone.Nested);
	}

	[TestMethod]
	public void DeepCloneShouldNotAffectOriginal()
	{
		CloneableNestedModel nested = new() { Value = "original" };
		CloneableParentModel original = new() { Id = 1, Nested = nested };

		CloneableParentModel clone = original.DeepClone();
		clone.Id = 99;
		clone.Nested!.Value = "modified";

		Assert.AreEqual(1, original.Id);
		Assert.AreEqual("original", original.Nested.Value);
	}

	[TestMethod]
	public void CloneShouldExcludeSpecifiedProperties()
	{
		CloneableExcludeTestModel original = new() { Id = 1, Name = "John", Secret = "abc" };

		CloneableExcludeTestModel clone = original.Clone();

		Assert.AreEqual(original.Id, clone.Id);
		Assert.AreEqual(original.Name, clone.Name);
		Assert.IsNull(clone.Secret);
	}

	[TestMethod]
	public void DeepCloneShouldExcludeSpecifiedProperties()
	{
		CloneableExcludeTestModel original = new() { Id = 1, Name = "John", Secret = "abc" };

		CloneableExcludeTestModel clone = original.DeepClone();

		Assert.AreEqual(original.Id, clone.Id);
		Assert.AreEqual(original.Name, clone.Name);
		Assert.IsNull(clone.Secret);
	}

	[TestMethod]
	public void ImplementsICloneable()
	{
		CloneableTestModel model = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

		Assert.IsInstanceOfType<ICloneable>(model);
	}

	[TestMethod]
	public void ICloneableCloneShouldReturnDeepClone()
	{
		CloneableNestedModel nested = new() { Value = "nested" };
		CloneableParentModel original = new() { Id = 1, Nested = nested };

		object clone = ((ICloneable)original).Clone();

		Assert.IsInstanceOfType<CloneableParentModel>(clone);
		CloneableParentModel typedClone = (CloneableParentModel)clone;
		Assert.AreNotSame(original, typedClone);
		Assert.AreNotSame(original.Nested, typedClone.Nested);
		Assert.AreEqual(original.Nested.Value, typedClone.Nested?.Value);
	}

	[TestMethod]
	public void CloneShouldHandleNullStringProperties()
	{
		CloneableTestModel original = new() { Id = 1, Name = null, Price = 0.0, IsActive = false };

		CloneableTestModel clone = original.Clone();

		Assert.AreNotSame(original, clone);
		Assert.AreEqual(original.Id, clone.Id);
		Assert.IsNull(clone.Name);
	}

	[TestMethod]
	public void DeepCloneShouldCopyValueTypesDirectly()
	{
		CloneableTestModel original = new() { Id = 42, Name = "Test", Price = 3.14, IsActive = true };

		CloneableTestModel clone = original.DeepClone();

		Assert.AreEqual(42, clone.Id);
		Assert.AreEqual("Test", clone.Name);
		Assert.AreEqual(3.14, clone.Price);
		Assert.IsTrue(clone.IsActive);
	}

	[TestMethod]
	public void DeepCloneShouldDeepCopyList()
	{
		CloneableCollectionModel original = new()
		{
			Id = 1,
			Tags = ["a", "b", "c"]
		};

		CloneableCollectionModel clone = original.DeepClone();

		Assert.AreNotSame(original.Tags, clone.Tags);
		CollectionAssert.AreEqual(original.Tags, clone.Tags);
		clone.Tags!.Add("d");
		Assert.HasCount(3, original.Tags);
	}

	[TestMethod]
	public void DeepCloneShouldDeepCopyListWithCloneableElements()
	{
		CloneableCollectionModel original = new()
		{
			Id = 1,
			Items =
			[
				new() { Value = "first" },
				new() { Value = "second" }
			]
		};

		CloneableCollectionModel clone = original.DeepClone();

		Assert.AreNotSame(original.Items, clone.Items);
		Assert.AreNotSame(original.Items![0], clone.Items![0]);
		Assert.AreEqual("first", clone.Items[0].Value);
	}

	[TestMethod]
	public void DeepCloneShouldDeepCopyDictionary()
	{
		CloneableCollectionModel original = new()
		{
			Id = 1,
			Scores = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }
		};

		CloneableCollectionModel clone = original.DeepClone();

		Assert.AreNotSame(original.Scores, clone.Scores);
		Assert.HasCount(2, clone.Scores!);
		Assert.AreEqual(1, clone.Scores!["a"]);
		clone.Scores!["c"] = 3;
		Assert.HasCount(2, original.Scores!);
	}

	[TestMethod]
	public void DeepCloneShouldDeepCopyDictionaryWithCloneableValues()
	{
		CloneableCollectionModel original = new()
		{
			Id = 1,
			NestedMap = new Dictionary<string, CloneableNestedModel>
			{
				["key"] = new() { Value = "nested" }
			}
		};

		CloneableCollectionModel clone = original.DeepClone();

		Assert.AreNotSame(original.NestedMap, clone.NestedMap);
		Assert.AreNotSame(original.NestedMap!["key"], clone.NestedMap!["key"]);
		Assert.AreEqual("nested", clone.NestedMap["key"].Value);
	}

	[TestMethod]
	public void DeepCloneShouldDeepCopyArray()
	{
		CloneableCollectionModel original = new()
		{
			Id = 1,
			Numbers = [1, 2, 3]
		};

		CloneableCollectionModel clone = original.DeepClone();

		Assert.AreNotSame(original.Numbers, clone.Numbers);
		CollectionAssert.AreEqual(original.Numbers, clone.Numbers);
		clone.Numbers![0] = 99;
		Assert.AreEqual(1, original.Numbers[0]);
	}

	[TestMethod]
	public void DeepCloneShouldDeepCopyArrayWithCloneableElements()
	{
		CloneableCollectionModel original = new()
		{
			Id = 1,
			NestedArray = [new CloneableNestedModel { Value = "item" }]
		};

		CloneableCollectionModel clone = original.DeepClone();

		Assert.AreNotSame(original.NestedArray, clone.NestedArray);
		Assert.AreNotSame(original.NestedArray![0], clone.NestedArray![0]);
		Assert.AreEqual("item", clone.NestedArray[0].Value);
	}

	[TestMethod]
	public void DeepCloneShouldHandleImmutableArray()
	{
		CloneableImmutableModel original = new()
		{
			Values = [1, 2, 3],
			CloneableValues = [new CloneableNestedModel { Value = "x" }]
		};

		CloneableImmutableModel clone = original.DeepClone();

		Assert.HasCount(3, clone.Values);
		Assert.AreEqual(1, clone.Values[0]);
		Assert.AreNotSame(original.CloneableValues[0], clone.CloneableValues[0]);
		Assert.AreEqual("x", clone.CloneableValues[0].Value);
	}

	[TestMethod]
	public void StructCloneShouldCreateCopy()
	{
		CloneableStructModel original = new() { Id = 1, Name = "Test", Score = 9.5 };

		CloneableStructModel clone = original.Clone();

		Assert.AreEqual(1, clone.Id);
		Assert.AreEqual("Test", clone.Name);
		Assert.AreEqual(9.5, clone.Score);
	}

	[TestMethod]
	public void StructDeepCloneShouldCreateCopy()
	{
		CloneableStructModel original = new() { Id = 1, Name = "Test", Score = 9.5 };

		CloneableStructModel clone = original.DeepClone();

		Assert.AreEqual(1, clone.Id);
		Assert.AreEqual("Test", clone.Name);
		Assert.AreEqual(9.5, clone.Score);
	}

	[TestMethod]
	public void StructImplementsICloneable()
	{
		CloneableStructModel model = new() { Id = 1, Name = "Test", Score = 9.5 };

		Assert.IsInstanceOfType<ICloneable>(model);
	}

	[TestMethod]
	public void StructDeepCloneShouldDeepCopyCollections()
	{
		CloneableStructWithRefModel original = new()
		{
			Id = 1,
			Tags = ["a", "b"]
		};

		CloneableStructWithRefModel clone = original.DeepClone();

		Assert.AreNotSame(original.Tags, clone.Tags);
		CollectionAssert.AreEqual(original.Tags, clone.Tags);
		clone.Tags!.Add("c");
		Assert.HasCount(2, original.Tags);
	}

	[TestMethod]
	public void DeepCloneShouldHandleNullCollections()
	{
		CloneableCollectionModel original = new()
		{
			Id = 1,
			Tags = null,
			Items = null,
			Scores = null,
			Numbers = null
		};

		CloneableCollectionModel clone = original.DeepClone();

		Assert.IsNull(clone.Tags);
		Assert.IsNull(clone.Items);
		Assert.IsNull(clone.Scores);
		Assert.IsNull(clone.Numbers);
	}
}

[GenerateCloneable]
public partial class CloneableTestModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public double Price { get; set; }
	public bool IsActive { get; set; }
}

[GenerateCloneable]
public partial class CloneableParentModel
{
	public int Id { get; set; }
	public CloneableNestedModel? Nested { get; set; }
}

[GenerateCloneable]
public partial class CloneableNestedModel
{
	public string? Value { get; set; }
}

[GenerateCloneable(nameof(Secret))]
public partial class CloneableExcludeTestModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? Secret { get; set; }
}

[GenerateCloneable]
public partial class CloneableCollectionModel
{
	public int Id { get; set; }
	public List<string>? Tags { get; set; }
	public List<CloneableNestedModel>? Items { get; set; }
	public Dictionary<string, int>? Scores { get; set; }
	public Dictionary<string, CloneableNestedModel>? NestedMap { get; set; }
	public int[]? Numbers { get; set; }
	public CloneableNestedModel[]? NestedArray { get; set; }
}

[GenerateCloneable]
public partial class CloneableImmutableModel
{
	public ImmutableArray<int> Values { get; set; }
	public ImmutableArray<CloneableNestedModel> CloneableValues { get; set; }
}

[GenerateCloneable]
public partial struct CloneableStructModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public double Score { get; set; }
}

[GenerateCloneable]
public partial struct CloneableStructWithRefModel
{
	public int Id { get; set; }
	public List<string>? Tags { get; set; }
}
