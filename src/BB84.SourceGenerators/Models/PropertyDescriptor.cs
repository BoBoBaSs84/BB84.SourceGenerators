// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Models;

/// <summary>
/// Describes a property discovered during source generation, carrying metadata
/// needed by multiple generators (Equality, Cloneable, Builder, ToString).
/// </summary>
internal sealed record PropertyDescriptor(string Name, string TypeName, bool IsValueType, bool IsNullable, bool IsCloneable, CollectionKind CollectionKind = CollectionKind.None, string? ElementTypeName = null, bool IsElementCloneable = false, string? DictionaryValueTypeName = null, bool IsDictionaryValueCloneable = false, string? FormatString = null, bool IsFormattable = false)
{
	/// <summary>
	/// Gets the name of the property.
	/// </summary>
	public string Name { get; } = Name;

	/// <summary>
	/// Gets the fully qualified type name of the property.
	/// </summary>
	public string TypeName { get; } = TypeName;

	/// <summary>
	/// Gets a value indicating whether the property type is a value type.
	/// </summary>
	public bool IsValueType { get; } = IsValueType;

	/// <summary>
	/// Gets a value indicating whether the property type is a reference type.
	/// </summary>
	public bool IsReferenceType => !IsValueType;

	/// <summary>
	/// Gets a value indicating whether the property is nullable.
	/// </summary>
	public bool IsNullable { get; } = IsNullable;

	/// <summary>
	/// Gets a value indicating whether the property type is marked with
	/// <c>[GenerateCloneable]</c> and supports deep cloning.
	/// </summary>
	public bool IsCloneable { get; } = IsCloneable;

	/// <summary>
	/// Gets the collection kind for this property (None if not a collection).
	/// </summary>
	public CollectionKind CollectionKind { get; } = CollectionKind;

	/// <summary>
	/// Gets the element type name for collection properties.
	/// </summary>
	public string? ElementTypeName { get; } = ElementTypeName;

	/// <summary>
	/// Gets a value indicating whether the element type of a collection is marked with
	/// <c>[GenerateCloneable]</c> and supports deep cloning.
	/// </summary>
	public bool IsElementCloneable { get; } = IsElementCloneable;

	/// <summary>
	/// Gets the dictionary value type name for dictionary properties.
	/// </summary>
	public string? DictionaryValueTypeName { get; } = DictionaryValueTypeName;

	/// <summary>
	/// Gets a value indicating whether the dictionary value type is marked with
	/// <c>[GenerateCloneable]</c> and supports deep cloning.
	/// </summary>
	public bool IsDictionaryValueCloneable { get; } = IsDictionaryValueCloneable;

	/// <summary>
	/// Gets the optional format string for the property, specified via <c>[ToStringFormat]</c>.
	/// When not <see langword="null"/>, the generated <c>ToString()</c> uses this format specifier.
	/// </summary>
	public string? FormatString { get; } = FormatString;

	/// <summary>
	/// Gets a value indicating whether the property type implements <see cref="System.IFormattable"/>.
	/// </summary>
	public bool IsFormattable { get; } = IsFormattable;
}
