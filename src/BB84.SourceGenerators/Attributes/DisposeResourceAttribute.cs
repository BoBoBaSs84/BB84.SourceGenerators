// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Marks a field for automatic disposal in the generated <c>Dispose(bool)</c> method.
/// Only fields decorated with this attribute will be disposed by the generated code.
/// </summary>
/// <param name="order">
/// The disposal order. Fields with non-zero values are disposed first in ascending order,
/// followed by fields with order <c>0</c> (default) in source declaration order.
/// </param>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class DisposeResourceAttribute(int order = 0) : Attribute
{
	/// <summary>
	/// Gets the disposal order for this resource.
	/// </summary>
	public int Order => order;
}
