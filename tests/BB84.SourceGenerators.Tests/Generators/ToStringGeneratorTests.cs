// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests.Generators;

[TestClass]
public sealed class ToStringGeneratorTests
{
	[TestMethod]
	public void ToStringShouldReturnAllProperties()
	{
		ToStringTestModel model = new()
		{
			Id = 42,
			Name = "John",
			Description = "A test model",
			Price = 19.99,
			IsActive = true
		};

		string? result = model.ToString();
		string expected = $"{nameof(ToStringTestModel)} {{ " +
			$"{nameof(model.Id)} = {model.Id}, " +
			$"{nameof(model.Name)} = {model.Name}, " +
			$"{nameof(model.Description)} = {model.Description}, " +
			$"{nameof(model.Price)} = {model.Price}, " +
			$"{nameof(model.IsActive)} = {model.IsActive} }}";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldHandleNullValues()
	{
		ToStringTestModel model = new()
		{
			Id = 1,
			Name = "Test",
			Description = null,
			Price = 0.0,
			IsActive = false
		};

		string? result = model.ToString();
		string expected = $"{nameof(ToStringTestModel)} {{ " +
			$"{nameof(model.Id)} = {model.Id}, " +
			$"{nameof(model.Name)} = {model.Name}, " +
			$"{nameof(model.Description)} = {model.Description}, " +
			$"{nameof(model.Price)} = {model.Price}, " +
			$"{nameof(model.IsActive)} = {model.IsActive} }}";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldHandleDefaultValues()
	{
		ToStringTestModel model = new()
		{
			Name = "Test",
			Description = "A super test"
		};

		string? result = model.ToString();
		string expected = $"{nameof(ToStringTestModel)} {{ " +
			$"{nameof(model.Id)} = {model.Id}, " +
			$"{nameof(model.Name)} = {model.Name}, " +
			$"{nameof(model.Description)} = {model.Description}, " +
			$"{nameof(model.Price)} = {model.Price}, " +
			$"{nameof(model.IsActive)} = {model.IsActive} }}";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldExcludeSpecifiedProperties()
	{
		ToStringExcludeTestModel model = new()
		{
			Id = 1,
			Name = "Visible",
			Secret = "Hidden",
			Internal = "AlsoHidden"
		};

		string? result = model.ToString();
		string expected = $"{nameof(ToStringExcludeTestModel)} {{ " +
			$"{nameof(model.Id)} = {model.Id}, " +
			$"{nameof(model.Name)} = {model.Name} }}";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldReturnClassNameOnlyWhenAllExcluded()
	{
		ToStringAllExcludedTestModel model = new()
		{
			Value = "test"
		};

		string? result = model.ToString();
		string expected = $"{nameof(ToStringAllExcludedTestModel)} {{ }}";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldWorkForNestedClasses()
	{
		ToStringOuterTestModel.ToStringNestedTestModel model = new()
		{
			Id = 1,
			Name = "Nested"
		};

		string? result = model.ToString();
		string expected = $"ToStringNestedTestModel {{ Id = {model.Id}, Name = {model.Name} }}";

		Assert.AreEqual(expected, result);
	}
}

[GenerateToString]
public partial class ToStringTestModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? Description { get; set; }
	public double Price { get; set; }
	public bool IsActive { get; set; }
}

[GenerateToString("Secret", "Internal")]
public partial class ToStringExcludeTestModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? Secret { get; set; }
	public string? Internal { get; set; }
}

[GenerateToString("Value")]
public partial class ToStringAllExcludedTestModel
{
	public string? Value { get; set; }
}

public partial class ToStringOuterTestModel
{
	[GenerateToString]
	public partial class ToStringNestedTestModel
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}
}
