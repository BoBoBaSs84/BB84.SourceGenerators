// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Extensions;

/// <summary>
/// Represents a static class that provides extension methods for the <see cref="ISymbol"/> interface.
/// </summary>
internal static class SymbolExtensions
{
	private static readonly SymbolDisplayFormat FullyQualifiedFormatWithoutGlobal =
		SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining);

	private static readonly SymbolDisplayFormat MinimallyQualifiedFormatWithoutGlobal =
		SymbolDisplayFormat.MinimallyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining);

	/// <summary>
	/// Returns a fully qualified display string for the given symbol, omitting the global namespace as a containing namespace.
	/// </summary>
	/// <param name="symbol">The symbol for which to get the fully qualified display string.</param>
	/// <returns>A fully qualified display string for the given symbol.</returns>
	internal static string ToFullyQualifiedDisplayString(this ISymbol symbol)
		=> symbol.ToDisplayString(FullyQualifiedFormatWithoutGlobal);

	/// <summary>
	/// Returns a minimally qualified display string for the given symbol, omitting the global namespace as a containing namespace.
	/// </summary>
	/// <param name="symbol">The symbol for which to get the minimally qualified display string.</param>
	/// <returns>A minimally qualified display string for the given symbol.</returns>
	internal static string ToMinimalDisplayString(this ISymbol symbol)
		=> symbol.ToDisplayString(MinimallyQualifiedFormatWithoutGlobal);
}
