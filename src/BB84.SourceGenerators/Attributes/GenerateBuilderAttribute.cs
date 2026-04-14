// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have a builder class generated for it.
/// The generated builder provides a fluent API with <c>With{PropertyName}(value)</c> methods
/// for each public settable property and a <c>Build()</c> method that creates the instance.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateBuilderAttribute : Attribute
{ }
