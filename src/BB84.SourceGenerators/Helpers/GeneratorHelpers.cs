// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Provides shared helper methods used across multiple source generators.
/// </summary>
internal static class GeneratorHelpers
{
	private const string PublicKeyword = "public";
	private const string InternalKeyword = "internal";
	private const string ProtectedKeyword = "protected";
	private const string PrivateKeyword = "private";

	/// <summary>
	/// Gets the accessibility keyword for a class declaration.
	/// </summary>
	/// <param name="classDeclaration">The class declaration syntax.</param>
	/// <returns>The accessibility keyword string.</returns>
	internal static string GetAccessibility(ClassDeclarationSyntax classDeclaration)
	{
		foreach (SyntaxToken modifier in classDeclaration.Modifiers)
		{
			if (modifier.IsKind(SyntaxKind.PublicKeyword))
				return PublicKeyword;
			if (modifier.IsKind(SyntaxKind.InternalKeyword))
				return InternalKeyword;
			if (modifier.IsKind(SyntaxKind.ProtectedKeyword))
				return ProtectedKeyword;
			if (modifier.IsKind(SyntaxKind.PrivateKeyword))
				return PrivateKeyword;
		}

		return InternalKeyword;
	}

	/// <summary>
	/// Gets the accessibility keyword string for a given <see cref="Accessibility"/> value.
	/// </summary>
	/// <param name="accessibility">The accessibility value.</param>
	/// <returns>The accessibility keyword string.</returns>
	internal static string GetAccessibilityKeyword(Accessibility accessibility) => accessibility switch
	{
		Accessibility.Public => PublicKeyword,
		Accessibility.Internal => InternalKeyword,
		Accessibility.Protected => ProtectedKeyword,
		Accessibility.ProtectedOrInternal => $"{ProtectedKeyword} {InternalKeyword}",
		Accessibility.ProtectedAndInternal => $"{PrivateKeyword} {ProtectedKeyword}",
		Accessibility.Private => PrivateKeyword,
		_ => PublicKeyword
	};

	/// <summary>
	/// Gets the list of outer (nesting) classes for a given class declaration.
	/// </summary>
	/// <param name="classDeclaration">The class declaration syntax.</param>
	/// <returns>A list of tuples containing the accessibility and name of each outer class, from outermost to innermost.</returns>
	internal static List<(string Accessibility, string Name)> GetOuterClasses(ClassDeclarationSyntax classDeclaration)
	{
		List<(string Accessibility, string Name)> outerClasses = [];
		SyntaxNode? parent = classDeclaration.Parent;

		while (parent is ClassDeclarationSyntax outerClass)
		{
			outerClasses.Add((GetAccessibility(outerClass), outerClass.Identifier.Text));
			parent = outerClass.Parent;
		}

		outerClasses.Reverse();
		return outerClasses;
	}

	/// <summary>
	/// Gets the set of excluded property names from an attribute's constructor arguments.
	/// </summary>
	/// <param name="classDeclaration">The class declaration syntax.</param>
	/// <param name="semanticModel">The semantic model.</param>
	/// <param name="attributeShortName">The short name of the attribute (e.g., "GenerateToString").</param>
	/// <param name="attributeFullName">The full name of the attribute (e.g., "GenerateToStringAttribute").</param>
	/// <returns>A set of excluded property names.</returns>
	internal static HashSet<string> GetExcludedProperties(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, string attributeShortName, string attributeFullName)
	{
		HashSet<string> excluded = new(StringComparer.Ordinal);

		foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
		{
			foreach (AttributeSyntax attribute in attributeList.Attributes)
			{
				string name = attribute.Name.ToString();

				if (name != attributeShortName && name != attributeFullName)
					continue;

				if (attribute.ArgumentList is null)
					continue;

				foreach (AttributeArgumentSyntax argument in attribute.ArgumentList.Arguments)
				{
					Optional<object?> constantValue = semanticModel.GetConstantValue(argument.Expression);

					if (constantValue.HasValue && constantValue.Value is string value)
						excluded.Add(value);
				}
			}
		}

		return excluded;
	}

	/// <summary>
	/// Escapes a string for use in a regular string literal.
	/// </summary>
	/// <param name="value">The string value to escape.</param>
	/// <returns>The escaped string.</returns>
	internal static string EscapeString(string value)
		=> value.Replace("\\", "\\\\").Replace("\"", "\\\"");

	/// <summary>
	/// Escapes a string for use in a verbatim string literal.
	/// </summary>
	/// <param name="value">The string value to escape.</param>
	/// <returns>The escaped string.</returns>
	internal static string EscapeVerbatimString(string value)
		=> value.Replace("\"", "\"\"");

	/// <summary>
	/// Transforms a <see cref="GeneratorAttributeSyntaxContext"/> into a tuple of class declaration syntax and semantic model.
	/// </summary>
	/// <param name="context">The generator attribute syntax context.</param>
	/// <returns>A tuple of class declaration syntax and semantic model, or null if the target node is not a class declaration.</returns>
	internal static (ClassDeclarationSyntax ClassSyntax, SemanticModel SemanticModel)? TransformClassSyntax(GeneratorAttributeSyntaxContext context)
		=> context.TargetNode is not ClassDeclarationSyntax classSyntax ? null : ((ClassDeclarationSyntax ClassSyntax, SemanticModel SemanticModel)?)(classSyntax, context.SemanticModel);
}
