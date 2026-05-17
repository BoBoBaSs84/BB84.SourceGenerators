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
	public void ValidateShouldReturnEmptyDictionaryForValidInstance()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "john@example.com",
			Age = 25,
			Bio = "Hello",
			Password = "abcdef"
		};

		Dictionary<string, List<string>> errors = model.Validate();

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

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Name"));
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

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Name"));
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

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Age"));
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

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Age"));
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

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Bio"));
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

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Bio"));
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

		List<string> errors = model.Validate("Password");

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

		List<string> errors = model.Validate("Password");

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

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Email"));
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

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsGreaterThanOrEqualTo(4, errors.Count);
	}

	[TestMethod]
	public void ValidateShouldUseCustomErrorMessages()
	{
		ValidatorCustomMessageTestModel model = new()
		{
			Name = null
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Name"));
		Assert.IsTrue(errors["Name"].Exists(e => e == "Please provide a name."));
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

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldReturnEmptyDictionaryForValidCollectionRange()
	{
		ValidatorCollectionRangeTestModel model = new()
		{
			Scores = [50, 75, 100]
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldDetectCollectionRangeViolationTooLow()
	{
		ValidatorCollectionRangeTestModel model = new()
		{
			Scores = []
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Scores"));
	}

	[TestMethod]
	public void ValidateShouldDetectCollectionRangeViolationTooHigh()
	{
		ValidatorCollectionRangeTestModel model = new()
		{
			Scores = [.. Enumerable.Range(1, 101)]
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Scores"));
	}

	[TestMethod]
	public void ValidateShouldAllowNullCollectionForRangeValidation()
	{
		ValidatorCollectionRangeTestModel model = new()
		{
			Scores = null
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldReturnEmptyDictionaryForEmptyCollectionWithZeroMinRange()
	{
		ValidatorStringCollectionRangeTestModel model = new()
		{
			Tags = []
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldReturnEmptyDictionaryForValidListRange()
	{
		ValidatorListRangeTestModel model = new()
		{
			Values = [2, 5, 8]
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldDetectListRangeViolation()
	{
		ValidatorListRangeTestModel model = new()
		{
			Values = [.. Enumerable.Range(1, 11)]
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Values"));
	}

	[TestMethod]
	public void ValidateShouldReturnEmptyDictionaryForValidStringCollectionRange()
	{
		ValidatorStringCollectionRangeTestModel model = new()
		{
			Tags = ["a", "b", "c"]
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldDetectStringCollectionRangeViolation()
	{
		ValidatorStringCollectionRangeTestModel model = new()
		{
			Tags = [.. Enumerable.Range(1, 11).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture))]
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Tags"));
	}

	[TestMethod]
	public void ValidateShouldWorkForNestedClasses()
	{
		ValidatorOuterTestModel.ValidatorNestedTestModel model = new()
		{
			Id = 0
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Id"));
	}

	[TestMethod]
	public void ValidateShouldReturnEmptyDictionaryForValidNestedClass()
	{
		ValidatorOuterTestModel.ValidatorNestedTestModel model = new()
		{
			Id = 5
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidatePropertyShouldReturnErrorsForSpecificProperty()
	{
		ValidatorTestModel model = new()
		{
			Name = null,
			Email = "john@example.com",
			Age = 25,
			Bio = "Hello",
			Password = "abcdef"
		};

		List<string> errors = model.Validate("Name");

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.Exists(e => e.Contains("Name")));
	}

	[TestMethod]
	public void ValidatePropertyShouldReturnEmptyListForValidProperty()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "john@example.com",
			Age = 25,
			Bio = "Hello",
			Password = "abcdef"
		};

		List<string> errors = model.Validate("Name");

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidatePropertyShouldReturnEmptyListForUnknownProperty()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Email = "john@example.com",
			Age = 25,
			Bio = "Hello",
			Password = "abcdef"
		};

		List<string> errors = model.Validate("NonExistentProperty");

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldDetectEmailAddressViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Age = 25,
			CompanyEmail = "not-an-email"
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("CompanyEmail"));
	}

	[TestMethod]
	public void ValidateShouldPassValidEmailAddress()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Age = 25,
			CompanyEmail = "user@example.com"
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsFalse(errors.ContainsKey("CompanyEmail"));
	}

	[TestMethod]
	public void ValidateShouldDetectUrlViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Age = 25,
			Website = "not-a-url"
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("Website"));
	}

	[TestMethod]
	public void ValidateShouldPassValidUrl()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Age = 25,
			Website = "https://example.com"
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsFalse(errors.ContainsKey("Website"));
	}

	[TestMethod]
	public void ValidateShouldDetectPhoneViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Age = 25,
			PhoneNumber = "abc123"
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("PhoneNumber"));
	}

	[TestMethod]
	public void ValidateShouldPassValidPhone()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Age = 25,
			PhoneNumber = "+1 (555) 123-4567"
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsFalse(errors.ContainsKey("PhoneNumber"));
	}

	[TestMethod]
	public void ValidateShouldDetectCreditCardViolation()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Age = 25,
			CC = "1234567890123456"
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("CC"));
	}

	[TestMethod]
	public void ValidateShouldPassValidCreditCard()
	{
		ValidatorTestModel model = new()
		{
			Name = "John",
			Age = 25,
			CC = "4111111111111111" // valid Luhn
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsFalse(errors.ContainsKey("CC"));
	}

	[TestMethod]
	public void ValidateShouldDetectCompareViolation()
	{
		ValidatorCompareTestModel model = new()
		{
			Password = "abc123",
			ConfirmPassword = "different"
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("ConfirmPassword"));
	}

	[TestMethod]
	public void ValidateShouldPassCompareWhenEqual()
	{
		ValidatorCompareTestModel model = new()
		{
			Password = "abc123",
			ConfirmPassword = "abc123"
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldDetectCustomValidationAttributeViolation()
	{
		ValidatorCustomAttrTestModel model = new()
		{
			EvenNumber = 3
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("EvenNumber"));
	}

	[TestMethod]
	public void ValidateShouldPassCustomValidationAttribute()
	{
		ValidatorCustomAttrTestModel model = new()
		{
			EvenNumber = 4
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsEmpty(errors);
	}

	[TestMethod]
	public void ValidateShouldIntegrateIValidatableObject()
	{
		ValidatorValidatableObjectTestModel model = new()
		{
			Start = new DateTime(2025, 6, 1),
			End = new DateTime(2025, 1, 1) // End before Start
		};

		Dictionary<string, List<string>> errors = model.Validate();

		Assert.IsNotEmpty(errors);
		Assert.IsTrue(errors.ContainsKey("End"));
	}

	[TestMethod]
	public void ValidateShouldPassIValidatableObjectWhenValid()
	{
		ValidatorValidatableObjectTestModel model = new()
		{
			Start = new DateTime(2025, 1, 1),
			End = new DateTime(2025, 6, 1)
		};

		Dictionary<string, List<string>> errors = model.Validate();

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

	[EmailAddress]
	public string? CompanyEmail { get; set; }

	[Url]
	public string? Website { get; set; }

	[Phone]
	public string? PhoneNumber { get; set; }

	[CreditCard]
	public string? CC { get; set; }

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

[GenerateValidator]
public partial class ValidatorCompareTestModel
{
	public string? Password { get; set; }

	[Compare(nameof(Password))]
	public string? ConfirmPassword { get; set; }
}

public sealed class EvenNumberAttribute : ValidationAttribute
{
	public override bool IsValid(object? value)
		=> value is int number && number % 2 == 0;
}

[GenerateValidator]
public partial class ValidatorCustomAttrTestModel
{
	[EvenNumber(ErrorMessage = "The EvenNumber field must be an even number.")]
	public int EvenNumber { get; set; }
}

[GenerateValidator]
public partial class ValidatorValidatableObjectTestModel : IValidatableObject
{
	public DateTime Start { get; set; }
	public DateTime End { get; set; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (End <= Start)
			yield return new ValidationResult("End must be after Start.", [nameof(End)]);
	}
}
