// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have equality members generated for it.
/// The generated code includes <c>Equals(object)</c>, <c>Equals(T)</c>, <c>GetHashCode()</c>,
/// <c>operator ==</c>, and <c>operator !=</c>, implementing <see cref="IEquatable{T}"/>.
/// </summary>
/// <param name="excludeProperties">The property names to exclude from the generated equality comparison.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateEqualityAttribute(params string[] excludeProperties) : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GenerateEqualityAttribute"/> class.
	/// </summary>
	public GenerateEqualityAttribute() : this([])
	{ }

	/// <summary>
	/// Gets the property names to exclude from the generated equality comparison.
	/// </summary>
	public string[] ExcludeProperties => excludeProperties;
}
