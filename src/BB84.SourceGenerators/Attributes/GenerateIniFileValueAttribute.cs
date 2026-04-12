// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated property represents a key-value pair within an INI file section.
/// The property should be of a primitive type such as <see cref="string"/>, <see cref="int"/>,
/// <see cref="long"/>, <see cref="float"/>, <see cref="double"/>, <see cref="bool"/>,
/// <see cref="decimal"/>, or <see cref="DateTime"/>.
/// </summary>
/// <param name="name">
/// The key name in the INI file. If not specified, the property name is used.
/// </param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateIniFileValueAttribute(string? name = null) : Attribute
{
	/// <summary>
	/// Gets the key name in the INI file.
	/// </summary>
	public string? Name => name;
}
