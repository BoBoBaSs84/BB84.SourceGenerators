// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;

using BB84.SourceGenerators.Extensions;
using BB84.SourceGenerators.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Provides shared helper methods used across multiple source generators.
/// </summary>
internal static class GeneratorHelpers
{
	private const string AttributeKeyword = "Attribute";
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
	/// <param name="attributeFullName">The full name of the attribute (e.g., "GenerateToStringAttribute").</param>
	/// <returns>A set of excluded property names.</returns>
	internal static HashSet<string> GetExcludedProperties(
		ClassDeclarationSyntax classDeclaration,
		SemanticModel semanticModel,
		string attributeFullName)
	{
		string attributeShortName = StripAttributeSuffix(attributeFullName);
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
	/// Strips the "Attribute" suffix from an attribute type name.
	/// </summary>
	/// <param name="attributeName">The full attribute name (e.g., "GenerateToStringAttribute").</param>
	/// <returns>The attribute name without the "Attribute" suffix (e.g., "GenerateToString").</returns>
	internal static string StripAttributeSuffix(string attributeName)
		=> attributeName.EndsWith(AttributeKeyword, StringComparison.Ordinal)
			? attributeName[..^AttributeKeyword.Length]
			: attributeName;

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

	/// <summary>
	/// Attempts to create a <see cref="GeneratorContext"/> from the given input tuple.
	/// Resolves the class symbol, extracts name, namespace, accessibility, and outer classes.
	/// </summary>
	/// <param name="input">The input tuple from the incremental pipeline, or <see langword="null"/>.</param>
	/// <param name="context">When this method returns <see langword="true"/>, contains the created context.</param>
	/// <returns><see langword="true"/> if the context was created successfully; otherwise, <see langword="false"/>.</returns>
	internal static bool TryCreateContext(
		(ClassDeclarationSyntax ClassSyntax, SemanticModel SemanticModel)? input,
		out GeneratorContext context)
	{
		context = null!;

		if (input is null)
			return false;

		ClassDeclarationSyntax classDeclaration = input.Value.ClassSyntax;
		SemanticModel semanticModel = input.Value.SemanticModel;

		INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
		if (classSymbol is null)
			return false;

		string className = classSymbol.Name;
		string namespaceName = classDeclaration.GetNamespace();
		string accessibility = GetAccessibility(classDeclaration);
		List<(string Accessibility, string Name)> outerClasses = GetOuterClasses(classDeclaration);

		context = new GeneratorContext(classDeclaration, semanticModel, classSymbol, className, namespaceName, accessibility, outerClasses);
		return true;
	}

	/// <summary>
	/// Gets all public readable properties from the given class symbol, optionally excluding specific properties.
	/// Properties must be public, non-static, and have a getter.
	/// </summary>
	/// <param name="classSymbol">The class symbol to inspect.</param>
	/// <param name="excludedProperties">Optional set of property names to exclude.</param>
	/// <param name="requireSetter">When <see langword="true"/>, only includes properties that also have a public setter.</param>
	/// <param name="cloneableAttributeName">
	/// When not <see langword="null"/>, checks whether the property type is decorated with the specified attribute
	/// and sets <see cref="PropertyDescriptor.IsCloneable"/> accordingly.
	/// </param>
	/// <returns>An immutable array of <see cref="PropertyDescriptor"/> instances.</returns>
	internal static ImmutableArray<PropertyDescriptor> GetPropertyDescriptors(
		INamedTypeSymbol classSymbol,
		HashSet<string>? excludedProperties = null,
		bool requireSetter = false,
		string? cloneableAttributeName = null)
	{
		ImmutableArray<PropertyDescriptor>.Builder builder = ImmutableArray.CreateBuilder<PropertyDescriptor>();

		foreach (IPropertySymbol propertySymbol in GetPublicPropertySymbols(classSymbol, requireGetter: true, requireSetter: requireSetter))
		{
			if (propertySymbol.IsWriteOnly)
				continue;

			if (excludedProperties is not null && excludedProperties.Contains(propertySymbol.Name))
				continue;

			bool isValueType = propertySymbol.Type.IsValueType
				&& propertySymbol.Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;
			bool isNullable = propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;
			string typeName = propertySymbol.Type.ToFullyQualifiedDisplayString();

			bool isCloneable = false;
			if (cloneableAttributeName is not null)
			{
				foreach (AttributeData attributeData in propertySymbol.Type.GetAttributes())
				{
					if (attributeData.AttributeClass?.ToDisplayString() == cloneableAttributeName)
					{
						isCloneable = true;
						break;
					}
				}
			}

			builder.Add(new PropertyDescriptor(propertySymbol.Name, typeName, isValueType, isNullable, isCloneable));
		}

		return builder.ToImmutable();
	}

	/// <summary>
	/// Enumerates public, non-static, non-indexer property symbols from the given type,
	/// optionally walking the inheritance hierarchy.
	/// </summary>
	/// <param name="typeSymbol">The type symbol to inspect.</param>
	/// <param name="includeInherited">When <see langword="true"/>, also includes properties from base types (excluding <see cref="object"/>).</param>
	/// <param name="requireGetter">When <see langword="true"/>, only includes properties with a getter.</param>
	/// <param name="requireSetter">When <see langword="true"/>, only includes properties with a public setter.</param>
	/// <returns>An enumerable of matching <see cref="IPropertySymbol"/> instances.</returns>
	internal static IEnumerable<IPropertySymbol> GetPublicPropertySymbols(
		ITypeSymbol typeSymbol,
		bool includeInherited = false,
		bool requireGetter = true,
		bool requireSetter = false)
	{
		ITypeSymbol? current = typeSymbol;

		do
		{
			foreach (ISymbol member in current.GetMembers())
			{
				if (member is not IPropertySymbol propertySymbol)
					continue;

				if (propertySymbol.DeclaredAccessibility != Accessibility.Public)
					continue;

				if (propertySymbol.IsStatic || propertySymbol.IsIndexer)
					continue;

				if (requireGetter && propertySymbol.GetMethod is null)
					continue;

				if (requireSetter && (propertySymbol.SetMethod is null || propertySymbol.SetMethod.DeclaredAccessibility != Accessibility.Public))
					continue;

				yield return propertySymbol;
			}

			current = includeInherited ? current.BaseType : null;
		}
		while (current is not null && current.SpecialType != SpecialType.System_Object);
	}
}
