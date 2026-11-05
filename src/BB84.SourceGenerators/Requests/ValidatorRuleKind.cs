// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Specifies the kind of a validation rule, derived from the data annotation attribute it was read from.
/// </summary>
internal enum ValidatorRuleKind
{
	/// <summary>
	/// The <c>[Required]</c> rule.
	/// </summary>
	Required,

	/// <summary>
	/// The <c>[Range]</c> rule.
	/// </summary>
	Range,

	/// <summary>
	/// The <c>[StringLength]</c> rule.
	/// </summary>
	StringLength,

	/// <summary>
	/// The <c>[MinLength]</c> rule.
	/// </summary>
	MinLength,

	/// <summary>
	/// The <c>[MaxLength]</c> rule.
	/// </summary>
	MaxLength,

	/// <summary>
	/// The <c>[RegularExpression]</c> rule.
	/// </summary>
	RegularExpression,

	/// <summary>
	/// The <c>[EmailAddress]</c> rule.
	/// </summary>
	EmailAddress,

	/// <summary>
	/// The <c>[Url]</c> rule.
	/// </summary>
	Url,

	/// <summary>
	/// The <c>[Phone]</c> rule.
	/// </summary>
	Phone,

	/// <summary>
	/// The <c>[CreditCard]</c> rule.
	/// </summary>
	CreditCard,

	/// <summary>
	/// The <c>[Compare]</c> rule.
	/// </summary>
	Compare,

	/// <summary>
	/// The <c>[AllowedValues]</c> rule.
	/// </summary>
	AllowedValues,

	/// <summary>
	/// The <c>[DeniedValues]</c> rule.
	/// </summary>
	DeniedValues,

	/// <summary>
	/// The custom <c>ValidationAttribute</c> rule.
	/// </summary>
	CustomValidation
}
