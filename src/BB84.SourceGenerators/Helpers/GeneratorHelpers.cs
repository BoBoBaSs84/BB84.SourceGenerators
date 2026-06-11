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
	/// Gets the accessibility keyword for a type declaration.
	/// </summary>
	/// <param name="typeDeclaration">The type declaration syntax.</param>
	/// <returns>The accessibility keyword string.</returns>
	internal static string GetAccessibility(TypeDeclarationSyntax typeDeclaration)
	{
		foreach (SyntaxToken modifier in typeDeclaration.Modifiers)
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
	/// Gets the list of outer (nesting) classes for a given type declaration.
	/// </summary>
	/// <param name="typeDeclaration">The type declaration syntax.</param>
	/// <returns>A list of tuples containing the accessibility and name of each outer class, from outermost to innermost.</returns>
	internal static List<(string Accessibility, string Name)> GetOuterClasses(TypeDeclarationSyntax typeDeclaration)
	{
		List<(string Accessibility, string Name)> outerTypes = [];
		SyntaxNode? parent = typeDeclaration.Parent;

		while (parent is TypeDeclarationSyntax parentType)
		{
			outerTypes.Add((GetAccessibility(parentType), parentType.Identifier.Text));
			parent = parentType.Parent;
		}

		outerTypes.Reverse();
		return outerTypes;
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
		=> GetExcludedProperties((TypeDeclarationSyntax)classDeclaration, semanticModel, attributeFullName);

	/// <summary>
	/// Gets the set of excluded property names from an attribute's constructor arguments on a type declaration.
	/// </summary>
	/// <param name="typeDeclaration">The type declaration syntax (class or struct).</param>
	/// <param name="semanticModel">The semantic model.</param>
	/// <param name="attributeFullName">The full name of the attribute (e.g., "GenerateToStringAttribute").</param>
	/// <returns>A set of excluded property names.</returns>
	internal static HashSet<string> GetExcludedProperties(
		TypeDeclarationSyntax typeDeclaration,
		SemanticModel semanticModel,
		string attributeFullName)
	{
		string attributeShortName = StripAttributeSuffix(attributeFullName);
		HashSet<string> excluded = new(StringComparer.Ordinal);

		foreach (AttributeListSyntax attributeList in typeDeclaration.AttributeLists)
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
	/// Returns the fully-qualified metadata name, the simple full name, and the short name
	/// (without the "Attribute" suffix) for the given attribute type.
	/// </summary>
	/// <typeparam name="TAttribute">The attribute type.</typeparam>
	/// <returns>
	/// A tuple of <c>(MetadataName, FullName, ShortName)</c> where <c>MetadataName</c> is the
	/// value of <see cref="Type.FullName"/>, <c>FullName</c> is the simple type name including
	/// the "Attribute" suffix, and <c>ShortName</c> is the type name without the suffix.
	/// </returns>
	internal static (string MetadataName, string FullName, string ShortName) GetAttributeNames<TAttribute>()
		where TAttribute : Attribute
	{
		string fullName = typeof(TAttribute).Name;
		return (typeof(TAttribute).FullName, fullName, StripAttributeSuffix(fullName));
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

	/// <summary>
	/// Registers a class-based incremental source generator pipeline that filters for classes
	/// decorated with the specified attribute, transforms them via <see cref="TransformClassSyntax"/>,
	/// and invokes the provided <paramref name="execute"/> callback.
	/// </summary>
	/// <param name="context">The generator initialization context.</param>
	/// <param name="attributeMetadataName">The fully qualified metadata name of the trigger attribute.</param>
	/// <param name="execute">The callback to execute for each matched class.</param>
	internal static void RegisterClassGenerator(
		IncrementalGeneratorInitializationContext context,
		string attributeMetadataName,
		Action<SourceProductionContext, (ClassDeclarationSyntax ClassSyntax, SemanticModel SemanticModel)?> execute)
	{
		IncrementalValuesProvider<(ClassDeclarationSyntax ClassSyntax, SemanticModel SemanticModel)?> provider =
			context.SyntaxProvider
				.ForAttributeWithMetadataName(
					fullyQualifiedMetadataName: attributeMetadataName,
					predicate: static (node, _) => node is ClassDeclarationSyntax,
					transform: static (ctx, _) => TransformClassSyntax(ctx))
				.Where(static result => result is not null);

		context.RegisterSourceOutput(provider, execute);
	}

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
	/// <param name="detectCollections">
	/// When <see langword="true"/>, detects collection types and sets <see cref="PropertyDescriptor.CollectionKind"/> accordingly.
	/// </param>
	/// <param name="toStringFormatAttributeName">
	/// When not <see langword="null"/>, checks whether the property is decorated with the specified attribute
	/// and sets <see cref="PropertyDescriptor.FormatString"/> accordingly.
	/// </param>
	/// <returns>An immutable array of <see cref="PropertyDescriptor"/> instances.</returns>
	internal static ImmutableArray<PropertyDescriptor> GetPropertyDescriptors(
		INamedTypeSymbol classSymbol,
		HashSet<string>? excludedProperties = null,
		bool requireSetter = false,
		string? cloneableAttributeName = null,
		bool detectCollections = false,
		string? toStringFormatAttributeName = null)
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
			CollectionKind collectionKind = CollectionKind.None;
			string? elementTypeName = null;
			bool isElementCloneable = false;
			string? dictionaryValueTypeName = null;
			bool isDictionaryValueCloneable = false;
			string? formatString = null;

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

			if (toStringFormatAttributeName is not null)
			{
				foreach (AttributeData attributeData in propertySymbol.GetAttributes())
				{
					if (attributeData.AttributeClass?.ToDisplayString() == toStringFormatAttributeName
						&& attributeData.ConstructorArguments.Length == 1
						&& attributeData.ConstructorArguments[0].Value is string fmt)
					{
						formatString = fmt;
						break;
					}
				}
			}

			// Detect collection types when explicitly requested or when cloneability checks are active
			if (cloneableAttributeName is not null || detectCollections)
			{
				(collectionKind, elementTypeName, isElementCloneable, dictionaryValueTypeName, isDictionaryValueCloneable) =
					DetectCollectionKind(propertySymbol.Type, cloneableAttributeName);
			}

			builder.Add(new PropertyDescriptor(propertySymbol.Name, typeName, isValueType, isNullable, isCloneable, collectionKind, elementTypeName, isElementCloneable, dictionaryValueTypeName, isDictionaryValueCloneable, formatString));
		}

		return builder.ToImmutable();
	}

	/// <summary>
	/// Detects the collection kind of a type symbol and extracts element type information.
	/// </summary>
	private static (CollectionKind Kind, string? ElementTypeName, bool IsElementCloneable, string? DictionaryValueTypeName, bool IsDictionaryValueCloneable) DetectCollectionKind(
		ITypeSymbol typeSymbol,
		string? cloneableAttributeName)
	{
		// Unwrap nullable
		if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
			typeSymbol = nullable.TypeArguments[0];

		// Array
		if (typeSymbol is IArrayTypeSymbol arrayType)
		{
			string elementType = arrayType.ElementType.ToFullyQualifiedDisplayString();
			bool elementCloneable = cloneableAttributeName is not null && HasAttribute(arrayType.ElementType, cloneableAttributeName);
			return (CollectionKind.Array, elementType, elementCloneable, null, false);
		}

		if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
		{
			string originalDef = namedType.OriginalDefinition.ToDisplayString();

			// Dictionary<TKey, TValue>
			if (originalDef == "System.Collections.Generic.Dictionary<TKey, TValue>")
			{
				string keyType = namedType.TypeArguments[0].ToFullyQualifiedDisplayString();
				string valueType = namedType.TypeArguments[1].ToFullyQualifiedDisplayString();
				bool valueCloneable = cloneableAttributeName is not null && HasAttribute(namedType.TypeArguments[1], cloneableAttributeName);
				return (CollectionKind.Dictionary, keyType, false, valueType, valueCloneable);
			}

			// List<T>
			if (originalDef == "System.Collections.Generic.List<T>")
			{
				string elementType = namedType.TypeArguments[0].ToFullyQualifiedDisplayString();
				bool elementCloneable = cloneableAttributeName is not null && HasAttribute(namedType.TypeArguments[0], cloneableAttributeName);
				return (CollectionKind.List, elementType, elementCloneable, null, false);
			}

			// ImmutableArray<T>
			if (originalDef == "System.Collections.Immutable.ImmutableArray<T>")
			{
				string elementType = namedType.TypeArguments[0].ToFullyQualifiedDisplayString();
				bool elementCloneable = cloneableAttributeName is not null && HasAttribute(namedType.TypeArguments[0], cloneableAttributeName);
				return (CollectionKind.ImmutableArray, elementType, elementCloneable, null, false);
			}

			// ReadOnlyCollection<T>, IReadOnlyList<T>, IReadOnlyCollection<T>
			if (originalDef is "System.Collections.ObjectModel.ReadOnlyCollection<T>"
				or "System.Collections.Generic.IReadOnlyList<T>"
				or "System.Collections.Generic.IReadOnlyCollection<T>")
			{
				string elementType = namedType.TypeArguments[0].ToFullyQualifiedDisplayString();
				bool elementCloneable = cloneableAttributeName is not null && HasAttribute(namedType.TypeArguments[0], cloneableAttributeName);
				return (CollectionKind.ReadOnlyCollection, elementType, elementCloneable, null, false);
			}
		}

		return (CollectionKind.None, null, false, null, false);
	}

	/// <summary>
	/// Checks whether a type symbol has a specific attribute.
	/// </summary>
	private static bool HasAttribute(ITypeSymbol typeSymbol, string attributeName)
	{
		foreach (AttributeData attr in typeSymbol.GetAttributes())
		{
			if (attr.AttributeClass?.ToDisplayString() == attributeName)
				return true;
		}
		return false;
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
