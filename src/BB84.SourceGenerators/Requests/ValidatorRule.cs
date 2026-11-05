// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a validation rule of a property, read from a data annotation attribute.
/// </summary>
/// <param name="Kind">The kind of the validation rule.</param>
internal sealed record ValidatorRule(ValidatorRuleKind Kind)
{
	/// <summary>
	/// The minimum value or length, formatted with the invariant culture, or <see langword="null"/> if none.
	/// </summary>
	public string? MinValue { get; init; }

	/// <summary>
	/// The maximum value or length, formatted with the invariant culture, or <see langword="null"/> if none.
	/// </summary>
	public string? MaxValue { get; init; }

	/// <summary>
	/// The regular expression pattern, or <see langword="null"/> if none.
	/// </summary>
	public string? Pattern { get; init; }

	/// <summary>
	/// The custom error message, or <see langword="null"/> to use the default message.
	/// </summary>
	public string? ErrorMessage { get; init; }

	/// <summary>
	/// The name of the property to compare with, or <see langword="null"/> if none.
	/// </summary>
	public string? CompareProperty { get; init; }

	/// <summary>
	/// The fully qualified type name of the custom validation attribute, or <see langword="null"/> if none.
	/// </summary>
	public string? CustomAttributeTypeName { get; init; }

	/// <summary>
	/// The allowed or denied values, formatted as constant values, or <see langword="null"/> if none.
	/// </summary>
	public EquatableArray<string>? Values { get; init; }
}
