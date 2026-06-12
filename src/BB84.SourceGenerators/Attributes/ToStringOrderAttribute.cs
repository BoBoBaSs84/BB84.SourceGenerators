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
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class ToStringOrderAttribute(int order) : Attribute
{
	/// <summary>
	/// Gets the sort position for this property in the generated <c>ToString()</c> output.
	/// </summary>
	public int Order { get; } = order;
}
