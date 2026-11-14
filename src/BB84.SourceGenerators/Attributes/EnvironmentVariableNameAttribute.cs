// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Overrides the environment variable name of the decorated property of a type marked with the
/// <see cref="GenerateEnvironmentVariablesAttribute"/> attribute. The name is used as written,
/// without the prefix, the separator or the naming style.
/// </summary>
/// <param name="name">The full name of the environment variable, for example <c>ENCRYPT</c>.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class EnvironmentVariableNameAttribute(string name) : Attribute
{
	/// <summary>
	/// Gets the full name of the environment variable.
	/// </summary>
	public string Name => name;
}
