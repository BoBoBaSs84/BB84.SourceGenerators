// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have a <c>Validate()</c> method generated for it.
/// The generated method scans the class properties for data annotation attributes at compile time
/// and emits direct validation checks, replacing runtime reflection-based validation.
/// <para>
/// Supported data annotation attributes:
/// <list type="bullet">
/// <item><c>RequiredAttribute</c></item>
/// <item><c>RangeAttribute</c></item>
/// <item><c>StringLengthAttribute</c></item>
/// <item><c>MinLengthAttribute</c></item>
/// <item><c>MaxLengthAttribute</c></item>
/// <item><c>RegularExpressionAttribute</c></item>
/// </list>
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateValidatorAttribute : Attribute
{ }
