// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have compile-time constant properties generated
/// for the assembly information attributes (such as title, version, company, copyright, etc.),
/// eliminating the need for runtime reflection to access assembly metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateAssemblyInformationAttribute : Attribute
{ }
