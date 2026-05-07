// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>partial</c> method should have a mapping implementation generated for it.
/// The source generator inspects the method's parameter type (source) and return type (target) at compile time
/// and emits property-to-property mapping code, eliminating runtime reflection entirely.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateAutoMapperAttribute : Attribute
{ }
