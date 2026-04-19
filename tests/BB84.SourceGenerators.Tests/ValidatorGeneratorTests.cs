// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.ComponentModel.DataAnnotations;

using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class ValidatorGeneratorTests
{
	[TestMethod]
	public void ValidateShouldReturnEmptyListForValidInstance()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "john@example.com",
			Age = 25,
			Bio = "Hello",
			Password = "abcdef"
		};

		List<string> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldDetectRequiredViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = null,
			Email = "john@example.com",
			Age = 25,
			Bio = "Hello",
			Password = "abcdef"
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Name")));
	}

	[TestMethod]
	public void ValidateShouldDetectRequiredEmptyStringViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = "",
			Email = "john@example.com",
			Age = 25,
			Bio = "Hello",
			Password = "abcdef"
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Name")));
	}

	[TestMethod]
	public void ValidateShouldDetectRangeViolationTooLow()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "john@example.com",
			Age = 0,
			Bio = "Hello",
			Password = "abcdef"
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Age")));
	}

	[TestMethod]
	public void ValidateShouldDetectRangeViolationTooHigh()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "john@example.com",
			Age = 200,
			Bio = "Hello",
			Password = "abcdef"
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Age")));
	}

	[TestMethod]
	public void ValidateShouldDetectStringLengthViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "john@example.com",
			Age = 25,
			Bio = new string('x', 501),
			Password = "abcdef"
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Bio")));
	}

	[TestMethod]
	public void ValidateShouldDetectStringLengthMinimumViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "john@example.com",
			Age = 25,
			Bio = "ab",
			Password = "abcdef"
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Bio")));
	}

	[TestMethod]
	public void ValidateShouldDetectMinLengthViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "john@example.com",
			Age = 25,
			Bio = "Hello",
			Password = "ab"
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Password")));
	}

	[TestMethod]
	public void ValidateShouldDetectMaxLengthViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "john@example.com",
			Age = 25,
			Bio = "Hello",
			Password = new string('x', 51)
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Password")));
	}

	[TestMethod]
	public void ValidateShouldDetectRegularExpressionViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "not-an-email",
			Age = 25,
			Bio = "Hello",
			Password = "abcdef"
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Email")));
	}

	[TestMethod]
	public void ValidateShouldDetectMultipleViolations()
	{
		ValidatorTestModel model = new()
		{
			Name = null,
			Email = "invalid",
			Age = -5,
			Bio = "ab",
			Password = "a"
		};

		List<string> errors = model.Validate();

		Assert.IsGreaterThanOrEqualTo(4, errors.Count);
	}

	[TestMethod]
	public void ValidateShouldUseCustomErrorMessages()
	{
		ValidatorCustomMessageTestModel model = new()
		{
			Name = null
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e == "Please provide a name."));
	}

	[TestMethod]
	public void ValidateShouldAllowNullForNonRequiredProperties()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = null,
			Age = 25,
			Bio = null,
			Password = null
		};

		List<string> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldReturnEmptyListForValidCollectionRange()
	{
		ValidatorCollectionRangeTestModel model = new()
		{
			Scores = [50, 75, 100]
		};

		List<string> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldDetectCollectionRangeViolationTooLow()
	{
		ValidatorCollectionRangeTestModel model = new()
		{
			Scores = []
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Scores")));
	}

	[TestMethod]
	public void ValidateShouldDetectCollectionRangeViolationTooHigh()
	{
		ValidatorCollectionRangeTestModel model = new()
		{
			Scores = Enumerable.Range(1, 101).ToArray()
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Scores")));
	}

	[TestMethod]
	public void ValidateShouldAllowNullCollectionForRangeValidation()
	{
		ValidatorCollectionRangeTestModel model = new()
		{
			Scores = null
		};

		List<string> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldReturnEmptyListForEmptyCollectionWithZeroMinRange()
	{
		ValidatorStringCollectionRangeTestModel model = new()
		{
			Tags = []
		};

		List<string> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldReturnEmptyListForValidListRange()
	{
		ValidatorListRangeTestModel model = new()
		{
			Values = [2, 5, 8]
		};

		List<string> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldDetectListRangeViolation()
	{
		ValidatorListRangeTestModel model = new()
		{
			Values = Enumerable.Range(1, 11).ToList()
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Values")));
	}

	[TestMethod]
	public void ValidateShouldReturnEmptyListForValidStringCollectionRange()
	{
		ValidatorStringCollectionRangeTestModel model = new()
		{
			Tags = ["a", "b", "c"]
		};

		List<string> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldDetectStringCollectionRangeViolation()
	{
		ValidatorStringCollectionRangeTestModel model = new()
		{
			Tags = Enumerable.Range(1, 11).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray()
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Tags")));
	}

	[TestMethod]
	public void ValidateShouldWorkForNestedClasses()
	{
		ValidatorOuterTestModel.ValidatorNestedTestModel model = new()
		{
			Id = 0
		};

		List<string> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Id")));
	}

	[TestMethod]
	public void ValidateShouldReturnEmptyListForValidNestedClass()
	{
		ValidatorOuterTestModel.ValidatorNestedTestModel model = new()
		{
			Id = 5
		};

		List<string> errors = model.Validate();

		Assert.IsEmpty(errors);
	}
}

[GenerateValidator]
public partial class ValidatorTestModel
{
	[Required]
	public string? Name { get; set; }

	[RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
	public string? Email { get; set; }

	[Range(1, 150)]
	public int Age { get; set; }

	[StringLength(500, MinimumLength = 3)]
	public string? Bio { get; set; }

	[MinLength(5)]
	[MaxLength(50)]
	public string? Password { get; set; }
}

[GenerateValidator]
public partial class ValidatorCustomMessageTestModel
{
	[Required(ErrorMessage = "Please provide a name.")]
	public string? Name { get; set; }
}

[GenerateValidator]
public partial class ValidatorCollectionRangeTestModel
{
	[Range(1, 100)]
	public int[]? Scores { get; set; }
}

[GenerateValidator]
public partial class ValidatorListRangeTestModel
{
	[Range(1, 10)]
	public List<int>? Values { get; set; }
}

[GenerateValidator]
public partial class ValidatorStringCollectionRangeTestModel
{
	[Range(0, 10)]
	public string[]? Tags { get; set; }
}

public partial class ValidatorOuterTestModel
{
	[GenerateValidator]
	public partial class ValidatorNestedTestModel
	{
		[Range(1, int.MaxValue)]
		public int Id { get; set; }
	}
}
