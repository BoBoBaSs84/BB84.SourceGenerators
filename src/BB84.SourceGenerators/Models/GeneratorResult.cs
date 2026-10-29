// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Models;

/// <summary>
/// The cacheable outcome of a generator transform: an optional request to emit source for,
/// and the diagnostics to report. Diagnostics cannot be raised from the transform stage, so
/// they are carried through the pipeline and reported from the source output stage.
/// </summary>
/// <typeparam name="TRequest">The request type, holding only equatable data.</typeparam>
/// <param name="Request">The request to emit source for, or <see langword="null"/> if nothing is emitted.</param>
/// <param name="Diagnostics">The diagnostics to report.</param>
internal sealed record GeneratorResult<TRequest>(TRequest? Request, EquatableArray<DiagnosticInfo> Diagnostics)
	where TRequest : class
{
	/// <summary>
	/// Creates a result that emits the given request without diagnostics.
	/// </summary>
	/// <param name="request">The request.</param>
	/// <returns>The result.</returns>
	internal static GeneratorResult<TRequest> FromRequest(TRequest request)
		=> new(request, EquatableArray<DiagnosticInfo>.Empty);

	/// <summary>
	/// Creates a result that only reports the given diagnostics.
	/// </summary>
	/// <param name="diagnostics">The diagnostics.</param>
	/// <returns>The result.</returns>
	internal static GeneratorResult<TRequest> FromDiagnostics(params DiagnosticInfo[] diagnostics)
		=> new(null, diagnostics.ToEquatableArray());
}
