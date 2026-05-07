// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Specifies a custom property name mapping between the source and target types
/// for a method decorated with <see cref="GenerateAutoMapperAttribute"/>.
/// </summary>
/// <param name="sourceProperty">The name of the property on the source type.</param>
/// <param name="targetProperty">The name of the property on the target type.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
internal sealed class PropertyMappingAttribute(string sourceProperty, string targetProperty) : Attribute
{
	/// <summary>
	/// Gets the name of the property on the source type.
	/// </summary>
	public string SourceProperty { get; } = sourceProperty;

	/// <summary>
	/// Gets the name of the property on the target type.
	/// </summary>
	public string TargetProperty { get; } = targetProperty;
}
