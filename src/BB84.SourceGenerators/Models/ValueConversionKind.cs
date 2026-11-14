// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Models;

/// <summary>
/// Classifies how a text value is converted to and from a property type.
/// </summary>
internal enum ValueConversionKind
{
	/// <summary>The value is a <see cref="string"/> and is used as is.</summary>
	String,
	/// <summary>The value is a <see cref="bool"/>; <c>true</c>/<c>false</c> (any casing) and <c>1</c>/<c>0</c> are accepted.</summary>
	Boolean,
	/// <summary>The value is a single <see cref="char"/>.</summary>
	Char,
	/// <summary>The value is an integral number, parsed with <c>NumberStyles.Integer</c>.</summary>
	Integer,
	/// <summary>The value is a <see cref="float"/> or <see cref="double"/>, parsed with <c>NumberStyles.Float | NumberStyles.AllowThousands</c>.</summary>
	FloatingPoint,
	/// <summary>The value is a <see cref="decimal"/>, parsed with <c>NumberStyles.Number</c>.</summary>
	Decimal,
	/// <summary>The value is a <see cref="DateTime"/>.</summary>
	DateTime,
	/// <summary>The value is a <see cref="DateTimeOffset"/>.</summary>
	DateTimeOffset,
	/// <summary>The value is a <see cref="TimeSpan"/>.</summary>
	TimeSpan,
	/// <summary>The value is a <see cref="Guid"/>.</summary>
	Guid,
	/// <summary>The value is an enum member name or a defined numeric value (case-insensitive).</summary>
	Enum,
	/// <summary>The value is a comma-separated list of <c>[Flags]</c> enum member names or a numeric value (case-insensitive).</summary>
	FlagsEnum,
	/// <summary>The value is a <c>System.Text.Encoding</c> name or numeric code page.</summary>
	Encoding,
}
