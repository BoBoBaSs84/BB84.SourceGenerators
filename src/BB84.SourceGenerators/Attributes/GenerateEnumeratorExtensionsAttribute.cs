// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>enum</c> should have extension methods generated for it.
/// The generated code will include the following extension methods:
/// <list type="bullet">
/// <item>A method to get the name of the enum value.</item>
/// <item>A method to get the description of the enum value from the <see cref="System.ComponentModel.DescriptionAttribute"/>.</item>
/// <item>A method to get the underlying integer value of the enum value.</item>
/// <item>A method to get all the enum values as a list.</item>
/// <item>A method to get all the enum names as a list.</item>
/// <item>A method to parse a member name into the enum value. For <c>[Flags]</c> enums an additional
/// overload parses separator-delimited member names into a combined value.</item>
/// <item>For <c>[Flags]</c> enums, a method to format a combined value as separator-delimited member names.</item>
/// </list>
/// </summary>
[AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateEnumeratorExtensionsAttribute : Attribute
{ }
