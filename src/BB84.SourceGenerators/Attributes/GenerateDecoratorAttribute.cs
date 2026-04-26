// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have a decorator pattern generated for it.
/// The class must implement at least one interface. The generated code delegates all interface
/// members to an inner instance and exposes them as virtual methods for selective overriding.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateDecoratorAttribute : Attribute
{ }
