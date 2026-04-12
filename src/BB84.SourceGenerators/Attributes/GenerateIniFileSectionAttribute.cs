// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated property represents a section within an INI file.
/// The property type must be a class whose properties can be decorated with
/// <see cref="IniFileValueAttribute"/> to define key-value pairs within the section.
/// </summary>
/// <param name="name">
/// The name of the section in the INI file. If not specified, the property name is used.
/// </param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateIniFileSectionAttribute(string? name = null) : Attribute
{
	/// <summary>
	/// Gets the name of the section in the INI file.
	/// </summary>
	public string? Name => name;
}
