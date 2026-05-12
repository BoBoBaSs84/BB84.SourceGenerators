// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators.Models;

/// <summary>
/// Contains the common context information extracted from a class declaration
/// that is shared across multiple source generators.
/// </summary>
/// <param name="ClassDeclaration">Gets the class declaration syntax node.</param>
/// <param name="SemanticModel">Gets the semantic model for the class declaration.</param>
/// <param name="ClassSymbol">Gets the named type symbol for the class.</param>
/// <param name="ClassName">Gets the name of the class.</param>
/// <param name="NamespaceName">Gets the namespace of the class.</param>
/// <param name="Accessibility">Gets the accessibility keyword of the class.</param>
/// <param name="OuterClasses">Gets the list of outer (nesting) classes, from outermost to innermost.</param>
internal sealed record GeneratorContext(
	ClassDeclarationSyntax ClassDeclaration,
	SemanticModel SemanticModel,
	INamedTypeSymbol ClassSymbol,
	string ClassName,
	string NamespaceName,
	string Accessibility,
	List<(string Accessibility, string Name)> OuterClasses
	);
