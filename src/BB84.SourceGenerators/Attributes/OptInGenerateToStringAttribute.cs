// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> or <c>struct</c> should have a <c>ToString()</c> override generated for it.
/// The generated method returns a formatted string containing the type name and all (or selected)
/// public property values in the format <c>TypeName { Prop1 = val1, Prop2 = val2 }</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
internal sealed class OptInGenerateToStringAttribute() : Attribute
{
	/// <summary>
	/// Gets or sets how collection properties are formatted in the generated <c>ToString()</c> output.
	/// Defaults to <see cref="CollectionFormat.Count"/>.
	/// </summary>
	public CollectionFormat CollectionFormat { get; set; } = CollectionFormat.Count;

	/// <summary>
	/// Gets or sets the separator string used between properties in the generated <c>ToString()</c> output.
	/// Defaults to <c>", "</c>.
	/// </summary>
	public string Separator { get; set; } = ", ";

	/// <summary>
	/// Gets or sets a value indicating whether to generate an <see cref="IFormattable"/> implementation.
	/// When <see langword="true"/>, the class implements <see cref="IFormattable"/> and properties whose types
	/// implement <see cref="IFormattable"/> receive the <c>formatProvider</c> parameter.
	/// Defaults to <see langword="false"/>.
	/// </summary>
	public bool Formattable { get; set; }

	/// <summary>
	/// Gets or sets the placeholder string rendered when a nullable property value is <see langword="null"/>.
	/// When <see langword="null"/> (the default), null values produce an empty string (standard interpolation behavior).
	/// Common values: <c>"null"</c>, <c>"&lt;null&gt;"</c>.
	/// </summary>
	public string? NullPlaceholder { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to include inherited public properties from base classes
	/// in the generated <c>ToString()</c> output.
	/// When <see langword="true"/>, the generator walks the base type hierarchy and appends inherited properties
	/// after the declared properties. The <c>excludeProperties</c> list applies to inherited members as well.
	/// Defaults to <see langword="false"/>.
	/// </summary>
	public bool IncludeInherited { get; set; }
}
