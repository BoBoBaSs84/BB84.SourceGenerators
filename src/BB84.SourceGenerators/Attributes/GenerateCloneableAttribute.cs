// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have clone methods generated for it.
/// The generated code includes a <c>Clone()</c> method for shallow copying and a
/// <c>DeepClone()</c> method that recursively deep clones reference-type properties
/// also marked with <see cref="GenerateCloneableAttribute"/>, implementing <see cref="ICloneable"/>.
/// </summary>
/// <param name="excludeProperties">The property names to exclude from the generated clone methods.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateCloneableAttribute(params string[] excludeProperties) : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GenerateCloneableAttribute"/> class.
	/// </summary>
	public GenerateCloneableAttribute() : this([])
	{ }

	/// <summary>
	/// Gets the property names to exclude from the generated clone methods.
	/// </summary>
	public string[] ExcludeProperties => excludeProperties;
}
