using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate an abstraction for a static class.
/// </summary>
/// <param name="TargetType">The named type symbol of the target static class.</param>
/// <param name="AbstractionType">The named type symbol of the generated abstraction.</param>
/// <param name="ImplementationType">The named type symbol of the generated implementation.</param>
/// <param name="ExcludeMethods">The methods to exclude from the abstraction.</param>
internal sealed record AbstractionRequest(
		INamedTypeSymbol TargetType,
		INamedTypeSymbol AbstractionType,
		INamedTypeSymbol ImplementationType,
		string[] ExcludeMethods
);
