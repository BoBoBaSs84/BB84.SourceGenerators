// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class MultiAttributeCompositionTests
{
	[TestMethod]
	public void ToStringAndEqualityShouldCoexist()
	{
		ToStringEqualityModel a = new() { Id = 1, Name = "Test" };
		ToStringEqualityModel b = new() { Id = 1, Name = "Test" };

		// ToString works
		string? str = a.ToString();
		Assert.IsNotNull(str);
		Assert.Contains("Id", str);
		Assert.Contains("Name", str);

		// Equality works
		Assert.IsTrue(a.Equals(b));
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
		Assert.IsTrue(a == b);
	}

	[TestMethod]
	public void ToStringAndBuilderShouldCoexist()
	{
		ToStringBuilderModel model = new ToStringBuilderModelBuilder()
			.WithId(42)
			.WithName("Built")
			.Build();

		string? str = model.ToString();

		Assert.IsNotNull(str);
		Assert.Contains("42", str);
		Assert.Contains("Built", str);
	}

	[TestMethod]
	public void ToStringAndCloneableShouldCoexist()
	{
		ToStringCloneableModel original = new() { Id = 1, Name = "Clone" };

		ToStringCloneableModel clone = original.Clone();

		Assert.AreNotSame(original, clone);
		Assert.AreEqual(original.Id, clone.Id);
		Assert.AreEqual(original.Name, clone.Name);

		string? originalStr = original.ToString();
		string? cloneStr = clone.ToString();
		Assert.AreEqual(originalStr, cloneStr);
	}

	[TestMethod]
	public void EqualityAndCloneableShouldCoexist()
	{
		EqualityCloneableModel original = new() { Id = 1, Name = "Test" };

		EqualityCloneableModel clone = original.Clone();

		Assert.IsTrue(original.Equals(clone));
		Assert.AreEqual(original.GetHashCode(), clone.GetHashCode());
		Assert.IsTrue(original == clone);
		Assert.AreNotSame(original, clone);
	}

	[TestMethod]
	public void ToStringEqualityAndCloneableShouldCoexist()
	{
		TripleModel a = new() { Id = 1, Value = "Hello" };
		TripleModel b = a.Clone();

		Assert.IsTrue(a.Equals(b));
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
		Assert.AreEqual(a.ToString(), b.ToString());
		Assert.AreNotSame(a, b);
	}

	[TestMethod]
	public void ToStringEqualityBuilderAndCloneableShouldCoexist()
	{
		QuadModel model = new QuadModelBuilder()
			.WithId(99)
			.WithLabel("Quad")
			.Build();

		QuadModel clone = model.Clone();

		// All generated methods work
		Assert.IsTrue(model.Equals(clone));
		Assert.AreEqual(model.GetHashCode(), clone.GetHashCode());
		Assert.AreEqual(model.ToString(), clone.ToString());
		Assert.AreNotSame(model, clone);
		Assert.AreEqual(99, clone.Id);
		Assert.AreEqual("Quad", clone.Label);
	}

	[TestMethod]
	public void ValidatorAndToStringShouldCoexist()
	{
		ValidatorToStringModel model = new() { Name = "Test" };

		Dictionary<string, List<string>> errors = model.Validate();
		string? str = model.ToString();

		Assert.IsEmpty(errors);
		Assert.IsNotNull(str);
		Assert.Contains("Name", str);
	}

	[TestMethod]
	public void ValidatorAndEqualityShouldCoexist()
	{
		ValidatorEqualityModel a = new() { Name = "Test" };
		ValidatorEqualityModel b = new() { Name = "Test" };

		Dictionary<string, List<string>> errors = a.Validate();
		Assert.IsEmpty(errors);
		Assert.IsTrue(a.Equals(b));
	}
}

#region Multi-Attribute Test Models

[GenerateToString]
[GenerateEquality]
public partial class ToStringEqualityModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
}

[GenerateToString]
[GenerateBuilder]
public partial class ToStringBuilderModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
}

[GenerateToString]
[GenerateCloneable]
public partial class ToStringCloneableModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
}

[GenerateEquality]
[GenerateCloneable]
public partial class EqualityCloneableModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
}

[GenerateToString]
[GenerateEquality]
[GenerateCloneable]
public partial class TripleModel
{
	public int Id { get; set; }
	public string? Value { get; set; }
}

[GenerateToString]
[GenerateEquality]
[GenerateBuilder]
[GenerateCloneable]
public partial class QuadModel
{
	public int Id { get; set; }
	public string? Label { get; set; }
}

[GenerateValidator]
[GenerateToString]
public partial class ValidatorToStringModel
{
	[System.ComponentModel.DataAnnotations.Required]
	public string? Name { get; set; }
}

[GenerateValidator]
[GenerateEquality]
public partial class ValidatorEqualityModel
{
	[System.ComponentModel.DataAnnotations.Required]
	public string? Name { get; set; }
}

#endregion
