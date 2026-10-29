// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Extensions;
using BB84.SourceGenerators.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators.Models;

/// <summary>
/// The cacheable description of the type declaration a generator extends.
/// </summary>
/// <param name="Name">The name of the type.</param>
/// <param name="NamespaceName">The namespace of the type.</param>
/// <param name="Accessibility">The accessibility keyword of the type.</param>
/// <param name="OuterClasses">The outer (nesting) classes, from outermost to innermost.</param>
/// <param name="IdentifierLocation">The location of the type identifier, used for diagnostics.</param>
internal sealed record TypeTargetInfo(
	string Name,
	string NamespaceName,
	string Accessibility,
	EquatableArray<(string Accessibility, string Name)> OuterClasses,
	LocationInfo? IdentifierLocation)
{
	/// <summary>
	/// Creates the description of the given type declaration.
	/// </summary>
	/// <param name="typeDeclaration">The type declaration syntax.</param>
	/// <param name="typeSymbol">The declared type symbol.</param>
	/// <returns>The type description.</returns>
	internal static TypeTargetInfo Create(TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol typeSymbol)
		=> new(
			typeSymbol.Name,
			typeDeclaration.GetNamespace(),
			GeneratorHelpers.GetAccessibility(typeDeclaration),
			GeneratorHelpers.GetOuterClasses(typeDeclaration).ToEquatableArray(),
			LocationInfo.From(typeDeclaration.Identifier));

	/// <summary>
	/// Creates a diagnostic located at the type identifier.
	/// </summary>
	/// <param name="descriptor">The diagnostic descriptor.</param>
	/// <param name="messageArgs">The message format arguments.</param>
	/// <returns>The diagnostic description.</returns>
	internal DiagnosticInfo CreateDiagnostic(DiagnosticDescriptor descriptor, params string[] messageArgs)
		=> DiagnosticInfo.Create(descriptor, IdentifierLocation, messageArgs);
}
