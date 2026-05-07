using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Extensions;

/// <summary>
/// Represents a static class that provides extension methods for the <see cref="ISymbol"/> interface.
/// </summary>
internal static class SymbolExtensions
{
	/// <summary>
	/// Returns a fully qualified display string for the given symbol, omitting the global namespace as a containing namespace.
	/// </summary>
	/// <param name="symbol">The symbol for which to get the fully qualified display string.</param>
	/// <returns>A fully qualified display string for the given symbol.</returns>
	internal static string ToFullyQualifiedDisplayString(this ISymbol symbol)
		=> symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining));

	/// <summary>
	/// Returns a minimally qualified display string for the given symbol, omitting the global namespace as a containing namespace.
	/// </summary>
	/// <param name="symbol">The symbol for which to get the minimally qualified display string.</param>
	/// <returns>A minimally qualified display string for the given symbol.</returns>
	internal static string ToMinimalDisplayString(this ISymbol symbol)
		=> symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining));
}
