// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Models;

/// <summary>
/// A cacheable description of a diagnostic, reported from the source output stage.
/// </summary>
/// <param name="Descriptor">The diagnostic descriptor.</param>
/// <param name="Location">The location of the diagnostic, or <see langword="null"/> for <see cref="Location.None"/>.</param>
/// <param name="MessageArgs">The message format arguments.</param>
internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo? Location, EquatableArray<string> MessageArgs)
{
	/// <summary>
	/// Creates a diagnostic description.
	/// </summary>
	/// <param name="descriptor">The diagnostic descriptor.</param>
	/// <param name="location">The location of the diagnostic, or <see langword="null"/> for <see cref="Location.None"/>.</param>
	/// <param name="messageArgs">The message format arguments.</param>
	/// <returns>The diagnostic description.</returns>
	internal static DiagnosticInfo Create(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] messageArgs)
		=> new(descriptor, location, messageArgs.ToEquatableArray());

	/// <summary>
	/// Creates the diagnostic, resolving its location against the given compilation.
	/// </summary>
	/// <param name="compilation">The compilation that contains the diagnostic location.</param>
	/// <returns>The diagnostic.</returns>
	internal Diagnostic ToDiagnostic(Compilation compilation)
		=> Diagnostic.Create(
			Descriptor,
			Location?.ToLocation(compilation) ?? Microsoft.CodeAnalysis.Location.None,
			[.. MessageArgs.Select(static arg => (object?)arg)]);
}
