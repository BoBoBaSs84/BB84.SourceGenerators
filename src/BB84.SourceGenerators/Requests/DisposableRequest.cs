// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate the dispose pattern for a class marked with the
/// <c>[GenerateDisposable]</c> attribute.
/// </summary>
/// <param name="Type">The class to extend.</param>
/// <param name="GenerateFinalizer">Indicates whether a finalizer is generated.</param>
/// <param name="GenerateAsync">Indicates whether <c>IAsyncDisposable</c> is implemented.</param>
/// <param name="IsSealed">Indicates whether the class is sealed.</param>
/// <param name="Fields">The fields to dispose, in disposal order.</param>
internal sealed record DisposableRequest(
	TypeTargetInfo Type,
	bool GenerateFinalizer,
	bool GenerateAsync,
	bool IsSealed,
	EquatableArray<(string FieldName, int Order, int SourceIndex)> Fields
	);
