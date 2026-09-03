// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators.Models;

/// <summary>
/// Contains the common context information extracted from a class or record declaration,
/// shared by generators that support both class and record (record class) targets.
/// </summary>
/// <param name="ClassDeclaration">Gets the class or record declaration syntax node.</param>
/// <param name="SemanticModel">Gets the semantic model for the declaration.</param>
/// <param name="ClassSymbol">Gets the named type symbol for the class or record.</param>
/// <param name="ClassName">Gets the name of the class or record.</param>
/// <param name="NamespaceName">Gets the namespace of the class or record.</param>
/// <param name="Accessibility">Gets the accessibility keyword of the class or record.</param>
/// <param name="OuterClasses">Gets the list of outer (nesting) classes, from outermost to innermost.</param>
/// <param name="IsRecord">Gets a value indicating whether the declaration is a record.</param>
/// <param name="PositionalParameterList">
/// Gets the primary constructor parameter list when the declaration is a positional record;
/// otherwise, <see langword="null"/>.
/// </param>
internal sealed record TypeGeneratorContext(
	TypeDeclarationSyntax ClassDeclaration,
	SemanticModel SemanticModel,
	INamedTypeSymbol ClassSymbol,
	string ClassName,
	string NamespaceName,
	string Accessibility,
	List<(string Accessibility, string Name)> OuterClasses,
	bool IsRecord,
	ParameterListSyntax? PositionalParameterList
	);
