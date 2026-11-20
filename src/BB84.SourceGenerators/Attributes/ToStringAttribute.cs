// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Specifies the sort position of a property in the generated <c>ToString()</c> output.
/// Properties with an explicit order are rendered first in ascending order, followed by
/// unordered properties in their natural declaration order.
/// </summary>
/// <param name="order">The sort position (lower values appear first).</param>
/// <param name="format">The format string to use (e.g., <c>"yyyy-MM-dd"</c> for dates or <c>"C2"</c> for currency).</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class ToStringAttribute(int order = 999, string? format = null) : Attribute
{
	/// <summary>
	/// Gets the sort position for this property in the generated <c>ToString()</c> output.
	/// </summary>
	public int Order { get; } = order;

	/// <summary>
	/// Gets the format string to use for the property in the generated <c>ToString()</c> output.
	/// </summary>
	public string? Format { get; } = format;
}

/*
 [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
internal sealed class ToStringAttribute : Attribute
{
	/// <summary>
	/// Gets the sort position for this property in the generated <c>ToString()</c> output.
	/// </summary>
	public int Order { get; }

	/// <summary>
	/// Gets the format string to use for the property in the generated <c>ToString()</c> output.
	/// </summary>
	public string? Format { get; }

	/// <param name="order">The sort position (lower values appear first).</param>
	public ToStringAttribute(int order = 999)
	{
		Order = order;
	}

	/// <param name="format">The format string to use (e.g., <c>"yyyy-MM-dd"</c> for dates or <c>"C2"</c> for currency).</param>
	public ToStringAttribute(string? format = null)
	{
		Format = format;
	}

	public ToStringAttribute()
	{
		
	}
}
*/
