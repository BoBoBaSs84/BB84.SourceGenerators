// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have the dispose pattern generated for it.
/// The generated code implements <see cref="IDisposable"/> with a <c>Dispose(bool)</c> method,
/// a <c>ThrowIfDisposed()</c> guard, and optionally a finalizer.
/// </summary>
/// <param name="generateFinalizer">
/// When <see langword="true"/>, generates a finalizer that calls <c>Dispose(false)</c>.
/// </param>
/// <param name="async">
/// When <see langword="true"/>, additionally implements <see cref="System.IAsyncDisposable"/>
/// with <c>DisposeAsync()</c> and <c>DisposeAsyncCore()</c>.
/// </param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateDisposableAttribute(bool generateFinalizer = false, bool async = false) : Attribute
{
	/// <summary>
	/// Gets a value indicating whether a finalizer should be generated.
	/// </summary>
	public bool GenerateFinalizer => generateFinalizer;

	/// <summary>
	/// Gets a value indicating whether <see cref="System.IAsyncDisposable"/> should be implemented.
	/// </summary>
	public bool Async => async;
}
