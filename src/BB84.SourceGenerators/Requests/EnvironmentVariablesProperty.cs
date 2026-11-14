// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Describes a property filled by the generated <c>Read</c> and <c>TryRead</c> methods.
/// </summary>
/// <param name="Name">The name of the property.</param>
/// <param name="VariableName">The name of the environment variable.</param>
/// <param name="TypeName">The fully qualified property type, including the nullable annotation.</param>
/// <param name="Conversion">The conversion of the environment variable value.</param>
/// <param name="IsMandatory">Indicates whether a missing environment variable is an error.</param>
/// <param name="InitialValue">The expression the value starts with: the constant fallback, <c>null</c>, <c>default</c> or <c>default!</c>.</param>
/// <param name="AssignAfterConstruction">
/// Indicates whether the property is assigned after construction, and only when the environment variable
/// is set, so that its (non-constant) initializer stays the fallback value.
/// </param>
internal sealed record EnvironmentVariablesProperty(
	string Name,
	string VariableName,
	string TypeName,
	EnvironmentVariablesConversion Conversion,
	bool IsMandatory,
	string InitialValue,
	bool AssignAfterConstruction
	);
