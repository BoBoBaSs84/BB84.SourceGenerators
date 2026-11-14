// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Describes a partial property implemented to read (and optionally write) its environment variable
/// on every access.
/// </summary>
/// <param name="Modifiers">The modifiers of the defining declaration, including <c>partial</c>.</param>
/// <param name="Name">The name of the property.</param>
/// <param name="VariableName">The name of the environment variable.</param>
/// <param name="TypeName">The fully qualified property type, including the nullable annotation.</param>
/// <param name="Conversion">The conversion of the environment variable value.</param>
/// <param name="IsNullable">Indicates whether the property returns <see langword="null"/> when the environment variable is missing.</param>
/// <param name="GetterPrefix">The modifiers of the get accessor followed by a space, or <see langword="null"/> if the property has no getter.</param>
/// <param name="SetterPrefix">The modifiers of the set accessor followed by a space, or <see langword="null"/> if the property has no setter.</param>
/// <param name="SetterKeyword">The keyword of the setter (<c>set</c> or <c>init</c>).</param>
internal sealed record EnvironmentVariablesPartialProperty(
	string Modifiers,
	string Name,
	string VariableName,
	string TypeName,
	ValueConversion Conversion,
	bool IsNullable,
	string? GetterPrefix,
	string? SetterPrefix,
	string SetterKeyword
	);
