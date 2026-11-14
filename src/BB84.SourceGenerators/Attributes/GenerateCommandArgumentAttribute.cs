// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that a private constructor, static <c>Parse</c> and <c>TryParse</c> methods and a
/// <c>Usage</c> constant should be generated for the decorated partial class or record class, which
/// parse command line arguments of the form <c>--propertyName value</c> into its public properties.
/// </summary>
/// <remarks>
/// <para>
/// The option name is <c>--</c> followed by the property name in camel case. Non-nullable properties
/// without an initializer are mandatory; nullable properties and properties with a constant initializer
/// are optional, and the initializer is the default value. A non-nullable <see cref="bool"/> property is
/// a flag that takes no value and is <see langword="true"/> when present.
/// </para>
/// <para>
/// The type must not declare a constructor that is not private, because the generated
/// <c>Parse</c> method is the only way to create an instance. Excluded properties are not
/// assigned by the generated constructor.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateCommandArgumentAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the names of the properties that are not command line options.
	/// </summary>
	public string[]? ExcludeProperties { get; set; }
}
