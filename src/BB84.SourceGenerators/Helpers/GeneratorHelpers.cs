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
	private static readonly string[] LineBreakSeparators = ["\r\n", "\n"];

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
				return Keywords.Public;
			if (modifier.IsKind(SyntaxKind.InternalKeyword))
				return Keywords.Internal;
			if (modifier.IsKind(SyntaxKind.ProtectedKeyword))
				return Keywords.Protected;
			if (modifier.IsKind(SyntaxKind.PrivateKeyword))
				return Keywords.Private;
		}

		return Keywords.Internal;
	}

	/// <summary>
	/// Gets the accessibility keyword string for a given <see cref="Accessibility"/> value.
	/// </summary>
	/// <param name="accessibility">The accessibility value.</param>
	/// <returns>The accessibility keyword string.</returns>
	internal static string GetAccessibilityKeyword(Accessibility accessibility) => accessibility switch
	{
		Accessibility.Public => Keywords.Public,
		Accessibility.Internal => Keywords.Internal,
		Accessibility.Protected => Keywords.Protected,
		Accessibility.ProtectedOrInternal => $"{Keywords.Protected} {Keywords.Internal}",
		Accessibility.ProtectedAndInternal => $"{Keywords.Private} {Keywords.Protected}",
		Accessibility.Private => Keywords.Private,
		_ => Keywords.Public
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
	/// Strips the "Attribute" suffix from an attribute type name.
	/// </summary>
	/// <param name="attributeName">The full attribute name (e.g., "GenerateToStringAttribute").</param>
	/// <returns>The attribute name without the "Attribute" suffix (e.g., "GenerateToString").</returns>
	internal static string StripAttributeSuffix(string attributeName)
		=> attributeName.EndsWith(AttributeKeyword, StringComparison.Ordinal)
			? attributeName.Substring(0, attributeName.Length - AttributeKeyword.Length)
			: attributeName;

	/// <summary>
	/// Checks whether the given type implements the interface with the given fully qualified name.
	/// </summary>
	/// <param name="typeSymbol">The type symbol to inspect.</param>
	/// <param name="interfaceFullName">The fully qualified name of the interface.</param>
	/// <returns><see langword="true"/> if the type implements the interface; otherwise, <see langword="false"/>.</returns>
	internal static bool ImplementsInterface(ITypeSymbol typeSymbol, string interfaceFullName)
	{
		foreach (INamedTypeSymbol iface in typeSymbol.AllInterfaces)
		{
			if (iface.ToDisplayString() == interfaceFullName)
				return true;
		}

		return false;
	}

	/// <summary>
	/// Gets the C# keyword prefix for a parameter based on its ref kind. <c>out</c> and <c>ref</c> are
	/// always emitted; the remaining modifiers only when requested.
	/// </summary>
	/// <param name="parameter">The parameter symbol.</param>
	/// <param name="includeIn">Whether <c>in</c> is emitted for <see cref="RefKind.In"/> parameters.</param>
	/// <param name="includeParams">Whether <c>params</c> is emitted for params parameters, taking precedence over the ref kind.</param>
	/// <param name="includeRefReadOnly">Whether <c>ref readonly</c> is emitted for <see cref="RefKind.RefReadOnlyParameter"/> parameters.</param>
	/// <returns>The modifier keyword followed by a space, or an empty string if none applies.</returns>
	internal static string GetRefKindModifier(IParameterSymbol parameter, bool includeIn, bool includeParams, bool includeRefReadOnly)
	{
		return includeParams && parameter.IsParams
			? "params "
			: parameter.RefKind switch
			{
				RefKind.Out => "out ",
				RefKind.Ref => "ref ",
				RefKind.In when includeIn => "in ",
				RefKind.RefReadOnlyParameter when includeRefReadOnly => "ref readonly ",
				_ => string.Empty
			};
	}

	/// <summary>
	/// Returns the fully-qualified metadata name, the simple type name, and the short name
	/// (without the "Attribute" suffix) for the given attribute type.
	/// </summary>
	/// <typeparam name="TAttribute">The attribute type.</typeparam>
	/// <returns>
	/// A tuple of <c>(MetadataName, TypeName, ShortName)</c> where <c>MetadataName</c> is the
	/// value of <see cref="Type.FullName"/>, <c>TypeName</c> is the simple type name including
	/// the "Attribute" suffix, and <c>ShortName</c> is the type name without the suffix.
	/// </returns>
	internal static (string MetadataName, string TypeName, string ShortName) GetAttributeNames<TAttribute>()
		where TAttribute : Attribute
	{
		string typeName = typeof(TAttribute).Name;
		return (typeof(TAttribute).FullName, typeName, StripAttributeSuffix(typeName));
	}

	/// <summary>
	/// Adds the source built by <paramref name="sb"/> to the compilation, using the hint name
	/// produced by <see cref="BuildHintName(IReadOnlyList{ValueTuple{string, string}}, string, string?)"/>.
	/// </summary>
	/// <param name="context">The source production context.</param>
	/// <param name="outerClasses">The outer (nesting) classes, from outermost to innermost.</param>
	/// <param name="typeName">The name of the generated or extended type.</param>
	/// <param name="feature">The optional feature suffix (e.g., "Equality").</param>
	/// <param name="sb">The source builder containing the generated source.</param>
	internal static void AddTypeSource(SourceProductionContext context, IReadOnlyList<(string Accessibility, string Name)> outerClasses, string typeName, string? feature, SourceBuilder sb)
		=> context.AddSource(BuildHintName(outerClasses, typeName, feature), sb.ToString());

	/// <summary>
	/// Builds the hint name for a generated source file, in the form
	/// <c>{Outer.Classes.}{TypeName}{.Feature}.g.cs</c>.
	/// </summary>
	/// <param name="outerClasses">The outer (nesting) classes, from outermost to innermost.</param>
	/// <param name="typeName">The name of the generated or extended type.</param>
	/// <param name="feature">The optional feature suffix (e.g., "Equality").</param>
	/// <returns>The hint name to pass to <see cref="SourceProductionContext.AddSource(string, string)"/>.</returns>
	internal static string BuildHintName(IReadOnlyList<(string Accessibility, string Name)> outerClasses, string typeName, string? feature = null)
		=> BuildHintName(outerClasses.Select(static o => o.Name), outerClasses.Count, typeName, feature);

	/// <inheritdoc cref="BuildHintName(IReadOnlyList{ValueTuple{string, string}}, string, string?)"/>
	internal static string BuildHintName(string typeName, string? feature = null)
		=> BuildHintName([], 0, typeName, feature);

	/// <inheritdoc cref="BuildHintName(IReadOnlyList{ValueTuple{string, string}}, string, string?)"/>
	internal static string BuildHintName(IReadOnlyList<(string Accessibility, string Name, bool IsStatic)> outerClasses, string typeName, string? feature = null)
		=> BuildHintName(outerClasses.Select(static o => o.Name), outerClasses.Count, typeName, feature);

	private static string BuildHintName(IEnumerable<string> outerClassNames, int outerClassCount, string typeName, string? feature)
	{
		string prefix = outerClassCount > 0 ? $"{string.Join(".", outerClassNames)}." : string.Empty;
		string suffix = feature is null ? string.Empty : $".{feature}";

		return $"{prefix}{typeName}{suffix}.g.cs";
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
	/// Formats a constant value with the invariant culture, so that generated code does not depend on
	/// the culture of the build machine (e.g. <c>0.5</c> instead of <c>0,5</c>).
	/// </summary>
	/// <param name="value">The constant value.</param>
	/// <returns>The formatted value, or <see langword="null"/> if <paramref name="value"/> is <see langword="null"/>.</returns>
	internal static string? FormatInvariant(object? value)
		=> value is IFormattable formattable
			? formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
			: value?.ToString();

	/// <summary>
	/// Registers an incremental source generator pipeline for the given trigger attribute. The transform
	/// produces a cacheable <see cref="GeneratorResult{TRequest}"/>; requests are passed to
	/// <paramref name="emit"/>, and diagnostics are reported with their locations resolved against the
	/// compilation. Only results that carry diagnostics depend on the compilation, so diagnostic-free
	/// results stay cached across edits.
	/// </summary>
	/// <typeparam name="TRequest">The request type, holding only equatable data.</typeparam>
	/// <param name="context">The generator initialization context.</param>
	/// <param name="attributeMetadataName">The fully qualified metadata name of the trigger attribute.</param>
	/// <param name="predicate">The syntactic filter for the attributed nodes.</param>
	/// <param name="transform">The transform producing the result for an attributed node.</param>
	/// <param name="trackingName">The tracking name of the transform step.</param>
	/// <param name="emit">The callback emitting the source for a request.</param>
	internal static void RegisterRequestGenerator<TRequest>(
		IncrementalGeneratorInitializationContext context,
		string attributeMetadataName,
		Func<SyntaxNode, CancellationToken, bool> predicate,
		Func<GeneratorAttributeSyntaxContext, CancellationToken, GeneratorResult<TRequest>?> transform,
		string trackingName,
		Action<SourceProductionContext, TRequest> emit)
		where TRequest : class
	{
		IncrementalValuesProvider<GeneratorResult<TRequest>> results =
			context.SyntaxProvider
				.ForAttributeWithMetadataName(attributeMetadataName, predicate, transform)
				.WithTrackingName(trackingName)
				.Where(static result => result is not null)
				.Select(static (result, _) => result!);

		context.RegisterSourceOutput(
			results.Where(static result => result.Request is not null).Select(static (result, _) => result.Request!),
			emit);

		context.RegisterSourceOutput(
			results.Where(static result => !result.Diagnostics.IsEmpty).Select(static (result, _) => result.Diagnostics).Combine(context.CompilationProvider),
			static (spc, input) =>
			{
				foreach (DiagnosticInfo diagnostic in input.Left)
					spc.ReportDiagnostic(diagnostic.ToDiagnostic(input.Right));
			});
	}

	/// <summary>
	/// Determines whether the node is a class declaration.
	/// </summary>
	/// <param name="node">The syntax node.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns><see langword="true"/> if the node is a class declaration; otherwise, <see langword="false"/>.</returns>
	internal static bool IsClassNode(SyntaxNode node, CancellationToken cancellationToken)
		=> IsClass(node);

	/// <summary>
	/// Determines whether the node is a class or record class declaration.
	/// </summary>
	/// <param name="node">The syntax node.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns><see langword="true"/> if the node is a class or record class declaration; otherwise, <see langword="false"/>.</returns>
	internal static bool IsClassOrRecordClassNode(SyntaxNode node, CancellationToken cancellationToken)
		=> IsClassOrRecordClass(node);

	/// <summary>
	/// Determines whether the node is a struct declaration.
	/// </summary>
	/// <param name="node">The syntax node.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns><see langword="true"/> if the node is a struct declaration; otherwise, <see langword="false"/>.</returns>
	internal static bool IsStructNode(SyntaxNode node, CancellationToken cancellationToken)
		=> IsStruct(node);

	private static bool IsClass(SyntaxNode node)
		=> node is ClassDeclarationSyntax;

	private static bool IsClassOrRecordClass(SyntaxNode node)
		=> node is ClassDeclarationSyntax
			|| (node is RecordDeclarationSyntax record && !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword));

	private static bool IsStruct(SyntaxNode node)
		=> node is StructDeclarationSyntax;

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
	/// <param name="toStringOrderAttributeName">
	/// When not <see langword="null"/>, checks whether the property is decorated with the specified attribute
	/// and sets <see cref="PropertyDescriptor.Order"/> accordingly. Properties with an explicit order are
	/// sorted first (ascending), followed by unordered properties in declaration order.
	/// </param>
	/// <param name="includeInherited">
	/// When <see langword="true"/>, also includes public properties from base types (excluding <see cref="object"/>).
	/// The <paramref name="excludedProperties"/> set applies to inherited members as well.
	/// </param>
	/// <returns>An immutable array of <see cref="PropertyDescriptor"/> instances.</returns>
	internal static ImmutableArray<PropertyDescriptor> GetPropertyDescriptors(
		INamedTypeSymbol classSymbol,
		HashSet<string>? excludedProperties = null,
		bool requireSetter = false,
		string? cloneableAttributeName = null,
		bool detectCollections = false,
		string? toStringFormatAttributeName = null,
		string? toStringOrderAttributeName = null,
		bool includeInherited = false)
	{
		ImmutableArray<PropertyDescriptor>.Builder builder = ImmutableArray.CreateBuilder<PropertyDescriptor>();

		foreach (IPropertySymbol propertySymbol in GetPublicPropertySymbols(classSymbol, includeInherited: includeInherited, requireGetter: true, requireSetter: requireSetter))
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
			int? order = null;

			if (cloneableAttributeName is not null)
				isCloneable = AttributeHelpers.HasAttribute(propertySymbol.Type, cloneableAttributeName);

			if (toStringFormatAttributeName is not null && AttributeHelpers.TryGetSingleCtorArg(propertySymbol, toStringFormatAttributeName, out string fmt))
				formatString = fmt;

			if (toStringOrderAttributeName is not null && AttributeHelpers.TryGetSingleCtorArg(propertySymbol, toStringOrderAttributeName, out int ord))
				order = ord;

			bool isFormattable = false;

			if (toStringFormatAttributeName is not null)
			{
				ITypeSymbol effectiveType = propertySymbol.Type;

				if (effectiveType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
					&& effectiveType is INamedTypeSymbol namedNullable)
				{
					effectiveType = namedNullable.TypeArguments[0];
				}

				isFormattable = ImplementsInterface(effectiveType, TypeNames.IFormattable);
			}

			// Detect collection types when explicitly requested or when cloneability checks are active
			if (cloneableAttributeName is not null || detectCollections)
			{
				(collectionKind, elementTypeName, isElementCloneable, dictionaryValueTypeName, isDictionaryValueCloneable) =
					DetectCollectionKind(propertySymbol.Type, cloneableAttributeName);
			}

			builder.Add(new PropertyDescriptor(propertySymbol.Name, typeName, isValueType, isNullable, isCloneable, collectionKind, elementTypeName, isElementCloneable, dictionaryValueTypeName, isDictionaryValueCloneable, formatString, isFormattable, order));
		}

		if (toStringOrderAttributeName is null)
			return builder.ToImmutable();

		// Ordered properties first (ascending), then unordered in declaration order (stable sort).
		return [.. builder
			.OrderBy(static p => p.Order.HasValue ? 0 : 1)
			.ThenBy(static p => p.Order ?? int.MaxValue)];
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
			bool elementCloneable = cloneableAttributeName is not null && AttributeHelpers.HasAttribute(arrayType.ElementType, cloneableAttributeName);
			return (CollectionKind.Array, elementType, elementCloneable, null, false);
		}

		if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
		{
			string originalDef = namedType.OriginalDefinition.ToDisplayString();

			// Dictionary<TKey, TValue>
			if (originalDef == TypeNames.DictionaryDefinition)
			{
				string keyType = namedType.TypeArguments[0].ToFullyQualifiedDisplayString();
				string valueType = namedType.TypeArguments[1].ToFullyQualifiedDisplayString();
				bool valueCloneable = cloneableAttributeName is not null && AttributeHelpers.HasAttribute(namedType.TypeArguments[1], cloneableAttributeName);
				return (CollectionKind.Dictionary, keyType, false, valueType, valueCloneable);
			}

			// List<T>
			if (originalDef == TypeNames.ListDefinition)
			{
				string elementType = namedType.TypeArguments[0].ToFullyQualifiedDisplayString();
				bool elementCloneable = cloneableAttributeName is not null && AttributeHelpers.HasAttribute(namedType.TypeArguments[0], cloneableAttributeName);
				return (CollectionKind.List, elementType, elementCloneable, null, false);
			}

			// ImmutableArray<T>
			if (originalDef == TypeNames.ImmutableArrayDefinition)
			{
				string elementType = namedType.TypeArguments[0].ToFullyQualifiedDisplayString();
				bool elementCloneable = cloneableAttributeName is not null && AttributeHelpers.HasAttribute(namedType.TypeArguments[0], cloneableAttributeName);
				return (CollectionKind.ImmutableArray, elementType, elementCloneable, null, false);
			}

			// ReadOnlyCollection<T>, IReadOnlyList<T>, IReadOnlyCollection<T>
			if (originalDef is TypeNames.ReadOnlyCollectionDefinition
				or TypeNames.IReadOnlyListDefinition
				or TypeNames.IReadOnlyCollectionDefinition)
			{
				string elementType = namedType.TypeArguments[0].ToFullyQualifiedDisplayString();
				bool elementCloneable = cloneableAttributeName is not null && AttributeHelpers.HasAttribute(namedType.TypeArguments[0], cloneableAttributeName);
				return (CollectionKind.ReadOnlyCollection, elementType, elementCloneable, null, false);
			}
		}

		return (CollectionKind.None, null, false, null, false);
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

	/// <summary>
	/// Gets the initializer of the given property, as declared in any of its declarations.
	/// </summary>
	/// <param name="property">The property symbol.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The initializer clause, or <see langword="null"/> if the property has no initializer.</returns>
	internal static EqualsValueClauseSyntax? GetPropertyInitializer(IPropertySymbol property, CancellationToken cancellationToken = default)
	{
		foreach (SyntaxReference syntaxReference in property.DeclaringSyntaxReferences)
		{
			if (syntaxReference.GetSyntax(cancellationToken) is PropertyDeclarationSyntax { Initializer: { } initializer })
				return initializer;
		}

		return null;
	}

	/// <summary>
	/// Formats a constant value as a C# literal that can be assigned to <paramref name="type"/>,
	/// independent of the culture of the build machine. Enum values are cast to the enum type.
	/// </summary>
	/// <param name="value">The constant value, as returned by <see cref="SemanticModel.GetConstantValue(SyntaxNode, CancellationToken)"/>.</param>
	/// <param name="type">The type the literal is assigned to.</param>
	/// <returns>The C# literal.</returns>
	internal static string FormatLiteral(object? value, ITypeSymbol type)
	{
		if (value is null)
			return "null";

		ITypeSymbol underlying = ValueConversions.GetUnderlyingType(type);
		string literal = value switch
		{
			double d when double.IsNaN(d) => "double.NaN",
			double d when double.IsPositiveInfinity(d) => "double.PositiveInfinity",
			double d when double.IsNegativeInfinity(d) => "double.NegativeInfinity",
			float f when float.IsNaN(f) => "float.NaN",
			float f when float.IsPositiveInfinity(f) => "float.PositiveInfinity",
			float f when float.IsNegativeInfinity(f) => "float.NegativeInfinity",
			_ => SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false) + value switch
			{
				float => "F",
				double => "D",
				decimal => "M",
				long => "L",
				ulong => "UL",
				uint => "U",
				_ => string.Empty,
			},
		};

		return underlying.TypeKind == TypeKind.Enum
			? $"({underlying.ToPropertyTypeDisplayString()})({literal})"
			: literal;
	}

	/// <summary>
	/// Determines whether the property is computed, that is, declared with an expression body or with
	/// accessor bodies instead of auto-implemented accessors.
	/// </summary>
	/// <param name="property">The property to inspect.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns><see langword="true"/> if any declaration of the property has a body; otherwise, <see langword="false"/>.</returns>
	internal static bool IsComputedProperty(IPropertySymbol property, CancellationToken cancellationToken = default)
		=> property.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax(cancellationToken) is PropertyDeclarationSyntax syntax
			&& (syntax.ExpressionBody is not null || syntax.AccessorList?.Accessors.Any(static a => a.Body is not null || a.ExpressionBody is not null) == true));

	/// <summary>
	/// Gets the text of the XML <c>&lt;summary&gt;</c> documentation comment of the symbol, with the
	/// lines trimmed and joined by a single space.
	/// </summary>
	/// <param name="symbol">The symbol whose declaration is inspected.</param>
	/// <returns>The flattened summary text, or <see langword="null"/> if the symbol has no non-empty summary.</returns>
	internal static string? GetXmlSummaryComment(ISymbol symbol)
	{
		foreach (SyntaxReference syntaxRef in symbol.DeclaringSyntaxReferences)
		{
			SyntaxNode node = syntaxRef.GetSyntax();

			if (!node.HasLeadingTrivia)
				continue;

			foreach (SyntaxTrivia trivia in node.GetLeadingTrivia())
			{
				if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
				{
					SyntaxNode? structure = trivia.GetStructure();
					if (structure is null)
						continue;

					foreach (SyntaxNode child in structure.ChildNodes())
					{
						if (child is not XmlElementSyntax xmlElement)
							continue;

						if (xmlElement.StartTag.Name.ToString() != "summary")
							continue;

						string content = xmlElement.Content.ToString();
						string[] lines = content.Split(LineBreakSeparators, StringSplitOptions.RemoveEmptyEntries);

						List<string> cleanLines = [];
						foreach (string line in lines)
						{
							string trimmed = line.Trim().TrimStart('/').Trim();
							if (trimmed.Length > 0)
								cleanLines.Add(trimmed);
						}

						if (cleanLines.Count > 0)
							return string.Join(" ", cleanLines);
					}
				}
			}
		}

		return null;
	}
}
