# Validator Generator

Generates a `Validate()` method for classes, scanning properties for data annotation attributes at compile time and emitting direct validation checks. This replaces runtime reflection-based `Validator.TryValidateObject()` with zero-overhead generated code.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

`Validator.TryValidateObject()` discovers data annotations through `TypeDescriptor` and reflection at runtime. This is slow, allocates, and does not work well with trimming or AOT. This generator reads the same data annotations at compile time and emits direct `if`-checks. You keep the familiar attributes and get fast, reflection-free validation.

## Benefits

- Generates direct `if`-checks at compile time for each data annotation rule
- Replaces `Validator.TryValidateObject()` which uses `TypeDescriptor` and reflection at runtime
- Provides compile-time discovery of validation attributes - no runtime attribute scanning
- Supports custom error messages for localization and user-friendly feedback
- Supports `[EmailAddress]`, `[Url]`, `[Phone]`, `[CreditCard]`, and `[Compare]` attributes
- Automatically detects custom `ValidationAttribute` subclasses and delegates to `IsValid()`
- Integrates with `IValidatableObject` for complex cross-property validation logic

## Attributes

```csharp
[GenerateValidator]
```

**Supported Data Annotations:**

- `[Required]` - Validates that the property is not null (or not null/empty for strings)
- `[Range(min, max)]` - Validates that a numeric value falls within a specified range, or that a collection has between min and max elements
- `[StringLength(max, MinimumLength = min)]` - Validates string length within bounds
- `[MinLength(length)]` - Validates minimum length of a string or collection
- `[MaxLength(length)]` - Validates maximum length of a string or collection
- `[RegularExpression(pattern)]` - Validates that a string matches a regex pattern
- `[EmailAddress]` - Validates that a string is a valid email address format
- `[Url]` - Validates that a string is a valid fully-qualified HTTP, HTTPS, or FTP URL
- `[Phone]` - Validates that a string is a valid phone number format
- `[CreditCard]` - Validates that a string passes the Luhn algorithm (credit card check)
- `[Compare("OtherProperty")]` - Validates that the property value matches another property's value
- `[AllowedValues(v1, v2, ...)]` (.NET 8+) - Emits an inline membership check; fails if the property value is not one of the listed values
- `[DeniedValues(v1, v2, ...)]` (.NET 8+) - Emits an inline exclusion check; fails if the property value equals any of the listed values
- **Custom `ValidationAttribute` subclasses** - Any attribute inheriting from `ValidationAttribute` is automatically detected and validated via `IsValid()` delegation

**IValidatableObject Integration:**

When a class implements `IValidatableObject`, the generated `Validate()` method automatically calls `IValidatableObject.Validate(ValidationContext)` and merges the results into the error dictionary.

**Constraints:**

- The attribute cannot be applied to abstract classes. A compile-time error (`BB84SG0001`) will be emitted if used on an abstract class.
- When a class inherits from a base class, the generated validator includes validation for all public properties from the entire inheritance hierarchy.

## Usage

### Example

```csharp
using System.ComponentModel.DataAnnotations;
using BB84.SourceGenerators.Attributes;

[GenerateValidator]
public partial class UserRegistration
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }

    [Required]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    public string? Email { get; set; }

    [Range(18, 120)]
    public int Age { get; set; }

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string? Password { get; set; }

    [Range(1, 10)]
    public List<int>? Skills { get; set; }
}
```

### Generated Code

The generator creates the following methods on the partial class:

- `Validate()` - Returns a `Dictionary<string, List<string>>` where each key is a property name and the value is a list of validation error messages for that property. An empty dictionary indicates a valid instance.
- `Validate(string propertyName)` - Returns a `List<string>` of validation error messages for the specified property. An empty list indicates the property is valid.

Both methods contain direct `if`-checks for each data annotation rule and support custom error messages via `ErrorMessage` property.

### Usage Example

```csharp
var registration = new UserRegistration
{
    Name = "J",
    Email = "not-an-email",
    Age = 15,
    Password = "short"
};

Dictionary<string, List<string>> errors = registration.Validate();

if (errors.Count > 0)
{
    foreach (KeyValuePair<string, List<string>> entry in errors)
    {
        Console.WriteLine($"{entry.Key}:");
        foreach (string error in entry.Value)
        {
            Console.WriteLine($"  - {error}");
        }
    }
    // Output:
    // Name:
    //   - The field Name must be a string with a minimum length of 2 and a maximum length of 100.
    // Email:
    //   - The field Email must match the regular expression '^[^@\s]+@[^@\s]+\.[^@\s]+$'.
    // Age:
    //   - The field Age must be between 18 and 120.
    // Password:
    //   - The field Password must be a string or collection with a minimum length of 8.
}
else
{
    Console.WriteLine("Registration is valid!");
}

// Validate a single property
List<string> nameErrors = registration.Validate("Name");
if (nameErrors.Count > 0)
{
    Console.WriteLine($"Name errors: {string.Join(", ", nameErrors)}");
}

// Custom error messages
[GenerateValidator]
public partial class LoginModel
{
    [Required(ErrorMessage = "Username is required.")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Password cannot be empty.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string? Password { get; set; }
}

// Cross-property validation with [Compare]
[GenerateValidator]
public partial class RegistrationModel
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [MinLength(8)]
    public string? Password { get; set; }

    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }

    [Url]
    public string? Website { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    [CreditCard]
    public string? PaymentCard { get; set; }
}

// Custom ValidationAttribute subclasses
public sealed class EvenNumberAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
        => value is int n && n % 2 == 0;
}

[GenerateValidator]
public partial class CustomAttrModel
{
    [EvenNumber(ErrorMessage = "Must be even.")]
    public int Value { get; set; }
}

// IValidatableObject integration
[GenerateValidator]
public partial class DateRangeModel : IValidatableObject
{
    [Required]
    public DateTime Start { get; set; }

    [Required]
    public DateTime End { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (End <= Start)
            yield return new ValidationResult("End must be after Start.", new[] { nameof(End) });
    }
}

// AllowedValues / DeniedValues (.NET 8+) — inline membership/exclusion checks
[GenerateValidator]
public partial class OrderModel
{
    // Emits: if (!Equals(Status, "pending") && !Equals(Status, "shipped") && !Equals(Status, "delivered"))
    [AllowedValues("pending", "shipped", "delivered", ErrorMessage = "Invalid order status.")]
    public string? Status { get; set; }

    // Emits: if (Equals(Priority, 0) || Equals(Priority, -1))
    [DeniedValues(0, -1, ErrorMessage = "Priority must be positive.")]
    public int Priority { get; set; }
}
```

## Diagnostics

| ID           | Severity | Description                                             |
| ------------ | -------- | ------------------------------------------------------- |
| `BB84SG0001` | Error    | GenerateValidator cannot be applied to abstract classes |
