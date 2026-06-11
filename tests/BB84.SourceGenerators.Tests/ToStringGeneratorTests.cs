// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

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

	[TestMethod]
	public void ToStringShouldFormatCollectionElementsCorrectly()
	{
		ToStringCollectionElementsTestModel model = new()
		{
			Name = "Alpha",
			Members = new List<string> { "Alice", "Bob", "Charlie" },
			Scores = new Dictionary<string, int> { ["Alice"] = 10, ["Bob"] = 20 }
		};

		string? result = model.ToString();
		string expected = nameof(ToStringCollectionElementsTestModel) + " { " +
			"Name = Alpha, " +
			"Members = [Alice, Bob, Charlie], " +
			"Scores = {Alice: 10, Bob: 20} }";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldHandleNullCollectionsGracefully()
	{
		ToStringCollectionElementsTestModel model = new()
		{
			Name = "Beta",
			Members = null,
			Scores = null
		};

		string? result = model.ToString();
		string expected = nameof(ToStringCollectionElementsTestModel) + " { " +
			"Name = Beta, " +
			"Members = null, " +
			"Scores = null }";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldFormatCollectionCountCorrectly()
	{
		ToStringCollectionCountTestModel model = new()
		{
			Name = "Team",
			Members = ["Alice", "Bob", "Charlie"]
		};

		string? result = model.ToString();
		string expected = nameof(ToStringCollectionCountTestModel) + " { " +
			"Name = Team, " +
			"Members = Count = 3 }";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldFormatCollectionTypeAndCountCorrectly()
	{
		ToStringCollectionTypeAndCountTestModel model = new()
		{
			Name = "Team",
			Members = ["Alice", "Bob"]
		};

		string? result = model.ToString();
		string expected = nameof(ToStringCollectionTypeAndCountTestModel) + " { " +
			"Name = Team, " +
			"Members = List`1 (Count = 2) }";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldFormatArrayElementsCorrectly()
	{
		ToStringArrayTestModel model = new()
		{
			Name = "Test",
			Tags = ["tag1", "tag2"]
		};

		string? result = model.ToString();
		string expected = nameof(ToStringArrayTestModel) + " { " +
			"Name = Test, " +
			"Tags = Count = 2 }";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldApplyCustomFormatSpecifiers()
	{
		ToStringFormatTestModel model = new()
		{
			CreatedAt = new DateTime(2025, 1, 15),
			Total = 1234.56m
		};

		string? result = model.ToString();
		string expected = nameof(ToStringFormatTestModel) + " { " +
			"CreatedAt = 2025-01-15, " +
			"Total = " + $"{model.Total:F2}" + " }";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ToStringShouldMixFormattedAndUnformattedProperties()
	{
		ToStringMixedFormatTestModel model = new()
		{
			Name = "Order",
			Amount = 99.5m,
			Created = new DateTime(2025, 6, 1, 14, 30, 0)
		};

		string? result = model.ToString();
		string expected = nameof(ToStringMixedFormatTestModel) + " { " +
			"Name = Order, " +
			"Amount = " + $"{model.Amount:C2}" + ", " +
			"Created = 2025-06-01 }";

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

[GenerateToString("Secret", nameof(Internal))]
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

[GenerateToString(CollectionFormat = CollectionFormat.Elements)]
public partial class ToStringCollectionElementsTestModel
{
	public string? Name { get; set; }
	public List<string>? Members { get; set; }
	public Dictionary<string, int>? Scores { get; set; }
}

[GenerateToString]
public partial class ToStringCollectionCountTestModel
{
	public string? Name { get; set; }
	public List<string>? Members { get; set; }
}

[GenerateToString(CollectionFormat = CollectionFormat.TypeAndCount)]
public partial class ToStringCollectionTypeAndCountTestModel
{
	public string? Name { get; set; }
	public List<string>? Members { get; set; }
}

[GenerateToString]
public partial class ToStringArrayTestModel
{
	public string? Name { get; set; }
	public string[]? Tags { get; set; }
}

[GenerateToString]
public partial class ToStringFormatTestModel
{
	[ToStringFormat("yyyy-MM-dd")]
	public DateTime CreatedAt { get; set; }

	[ToStringFormat("F2")]
	public decimal Total { get; set; }
}

[GenerateToString]
public partial class ToStringMixedFormatTestModel
{
	public string? Name { get; set; }

	[ToStringFormat("C2")]
	public decimal Amount { get; set; }

	[ToStringFormat("yyyy-MM-dd")]
	public DateTime Created { get; set; }
}
