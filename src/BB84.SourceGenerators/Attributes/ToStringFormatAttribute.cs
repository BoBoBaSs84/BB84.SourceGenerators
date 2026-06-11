// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Specifies a custom format string for a property in the generated <c>ToString()</c> output.
/// The format string is passed to the property's <c>ToString(string)</c> method at runtime.
/// </summary>
/// <param name="format">The format string to use (e.g., <c>"yyyy-MM-dd"</c> for dates or <c>"C2"</c> for currency).</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class ToStringFormatAttribute(string format) : Attribute
{
	/// <summary>
	/// Gets the format string to use for the property in the generated <c>ToString()</c> output.
	/// </summary>
	public string Format { get; } = format;
}
