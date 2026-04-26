// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have a singleton pattern generated for it.
/// The generated code includes a static <c>Instance</c> property and a private parameterless constructor.
/// </summary>
/// <param name="useLazy">
/// When <see langword="true"/> (default), the singleton is backed by <see cref="System.Lazy{T}"/>
/// for thread-safe lazy initialization. When <see langword="false"/>, a simple static readonly
/// field is used instead.
/// </param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateSingletonAttribute(bool useLazy = true) : Attribute
{
	/// <summary>
	/// Gets a value indicating whether the singleton should use <see cref="Lazy{T}"/>
	/// for thread-safe lazy initialization.
	/// </summary>
	public bool UseLazy => useLazy;
}
