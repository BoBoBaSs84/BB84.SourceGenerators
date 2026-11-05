// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate INI file serialization for a class marked with the
/// <c>[GenerateIniFile]</c> attribute.
/// </summary>
/// <param name="Type">The class to extend.</param>
/// <param name="StringComparison">The name of the <c>StringComparison</c> member used to match section and key names.</param>
/// <param name="SerializeComments">Indicates whether XML summary comments are written as INI comments.</param>
/// <param name="Sections">The sections of the INI file, in depth-first order.</param>
internal sealed record IniFileRequest(
	TypeTargetInfo Type,
	string StringComparison,
	bool SerializeComments,
	EquatableArray<IniFileSection> Sections
	);
