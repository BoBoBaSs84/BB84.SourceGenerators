// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a value of an INI file section, backed by a property marked with the <c>[IniValue]</c> attribute.
/// </summary>
/// <param name="PropertyName">The name of the value property.</param>
/// <param name="KeyName">The key name of the value.</param>
/// <param name="TypeName">The type name of the value property.</param>
/// <param name="IsEnum">Indicates whether the value type is an enumeration.</param>
/// <param name="IsFlagsEnum">Indicates whether the value type is a flags enumeration.</param>
/// <param name="EnumFullName">The fully qualified enumeration type name, or <see langword="null"/> if the value is not an enumeration.</param>
/// <param name="Comment">The comment written above the value, or <see langword="null"/> if none.</param>
/// <param name="IsCollection">Indicates whether the value type is a list or an array.</param>
/// <param name="CollectionElementTypeName">The element type name of the collection, or <see langword="null"/> if the value is not a collection.</param>
/// <param name="IsArray">Indicates whether the value type is an array.</param>
internal sealed record IniFileValue(
	string PropertyName,
	string KeyName,
	string TypeName,
	bool IsEnum = false,
	bool IsFlagsEnum = false,
	string? EnumFullName = null,
	string? Comment = null,
	bool IsCollection = false,
	string? CollectionElementTypeName = null,
	bool IsArray = false
	);
