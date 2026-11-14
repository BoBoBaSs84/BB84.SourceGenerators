// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Models;

/// <summary>
/// Describes how a text value (such as an environment variable or a command line argument) is
/// converted to a property type and back.
/// </summary>
/// <param name="Kind">The conversion kind.</param>
/// <param name="UnderlyingTypeName">The fully qualified underlying (non-nullable) type name.</param>
/// <param name="DisplayName">The short type name used in error messages (e.g. <c>Int32</c>).</param>
/// <param name="IsNullableValueType">Indicates whether the property type is a <see cref="System.Nullable{T}"/>.</param>
internal sealed record ValueConversion(
	ValueConversionKind Kind,
	string UnderlyingTypeName,
	string DisplayName,
	bool IsNullableValueType
	);
