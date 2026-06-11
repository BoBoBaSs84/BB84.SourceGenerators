// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Specifies how collection properties are formatted in the generated <c>ToString()</c> output.
/// </summary>
internal enum CollectionFormat
{
	/// <summary>
	/// Shows only the element count: <c>Count = 3</c>.
	/// </summary>
	Count = 0,

	/// <summary>
	/// Formats elements inline: <c>[item1, item2]</c> for list-like collections and
	/// <c>{key1: val1, key2: val2}</c> for dictionaries. This is the default.
	/// </summary>
	Elements = 1,

	/// <summary>
	/// Shows the runtime type name and element count: e.g. <c>List`1 (Count = 3)</c>.
	/// </summary>
	TypeAndCount = 2,
}

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

	/// <summary>
	/// Gets or sets how collection properties are formatted in the generated <c>ToString()</c> output.
	/// Defaults to <see cref="CollectionFormat.Count"/>.
	/// </summary>
	public CollectionFormat CollectionFormat { get; set; } = CollectionFormat.Count;
}
