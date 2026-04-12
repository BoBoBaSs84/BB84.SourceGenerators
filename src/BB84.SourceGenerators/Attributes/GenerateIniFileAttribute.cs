// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have static methods generated for
/// reading and writing INI file content. The generated code will include the following methods:
/// <list type="bullet">
/// <item>A static <c>Read</c> method that takes an INI file string content and returns a deserialized instance.</item>
/// <item>A static <c>Write</c> method that takes an instance and returns the serialized INI file string content.</item>
/// </list>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateIniFileAttribute : Attribute
{ }
