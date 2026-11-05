// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a section of the INI file, backed by a property marked with the <c>[IniSection]</c> attribute.
/// </summary>
/// <param name="PropertyPath">The member access path of the section property, relative to the generated class.</param>
/// <param name="SectionName">The full section name, including the names of the parent sections.</param>
/// <param name="Values">The values of the section.</param>
/// <param name="Comment">The comment written above the section, or <see langword="null"/> if none.</param>
internal sealed record IniFileSection(
	string PropertyPath,
	string SectionName,
	EquatableArray<IniFileValue> Values,
	string? Comment = null
	);
