// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have a <c>ToString()</c> override generated for it.
/// The generated method returns a formatted string containing the class name and all (or selected)
/// public property values in the format <c>ClassName { Prop1 = val1, Prop2 = val2 }</c>.
/// </summary>
/// <param name="excludeProperties">The property names to exclude from the generated <c>ToString()</c> output.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateToStringAttribute(params string[] excludeProperties) : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GenerateToStringAttribute"/> class.
	/// </summary>
	public GenerateToStringAttribute() : this([])
	{ }

	/// <summary>
	/// Gets the property names to exclude from the generated <c>ToString()</c> output.
	/// </summary>
	public string[] ExcludeProperties => excludeProperties;
}
