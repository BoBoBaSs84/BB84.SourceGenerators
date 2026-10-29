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

		foreach (AttributeSyntax attribute in FindAttributes(typeDeclaration.AttributeLists, attributeFullName, attributeShortName))
		{
			if (attribute.ArgumentList is null)
				continue;

			foreach (AttributeArgumentSyntax argument in attribute.ArgumentList.Arguments)
			{
				Optional<object?> constantValue = semanticModel.GetConstantValue(argument.Expression);

				if (constantValue.HasValue && constantValue.Value is string value)
					_ = excluded.Add(value);
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
	/// Gets the value of a named argument from an attribute on a type declaration.
	/// Searches the attribute lists for an attribute matching <paramref name="attributeShortName"/> or
	/// <paramref name="attributeFullName"/>, then looks for a named argument with the given <paramref name="propertyName"/>.
	/// </summary>
	/// <typeparam name="T">The expected type of the argument value.</typeparam>
	/// <param name="typeDeclaration">The type declaration syntax.</param>
	/// <param name="semanticModel">The semantic model.</param>
	/// <param name="attributeShortName">The short name of the attribute.</param>
	/// <param name="attributeFullName">The full name of the attribute.</param>
	/// <param name="propertyName">The name of the named argument to extract.</param>
	/// <param name="defaultValue">The default value to return if the argument is not found.</param>
	/// <returns>The extracted value, or <paramref name="defaultValue"/> if not found.</returns>
	internal static T GetNamedArgumentValue<T>(
		TypeDeclarationSyntax typeDeclaration,
		SemanticModel semanticModel,
		string attributeShortName,
		string attributeFullName,
		string propertyName,
		T defaultValue)
	{
		foreach (AttributeSyntax attribute in FindAttributes(typeDeclaration.AttributeLists, attributeFullName, attributeShortName))
		{
			if (attribute.ArgumentList is null)
				return defaultValue;

			foreach (AttributeArgumentSyntax arg in attribute.ArgumentList.Arguments)
			{
				if (arg.NameEquals?.Name.Identifier.Text != propertyName)
					continue;

				Optional<object?> value = semanticModel.GetConstantValue(arg.Expression);

				if (value.HasValue && value.Value is T typedValue)
					return typedValue;

				break;
			}

			return defaultValue;
		}

		return defaultValue;
	}

	/// <summary>
	/// Finds the attributes in the given attribute lists whose name, as written in source, matches
	/// <paramref name="attributeFullName"/> or <paramref name="attributeShortName"/>.
	/// </summary>
	/// <param name="attributeLists">The attribute lists of a declaration.</param>
	/// <param name="attributeFullName">The simple type name of the attribute (e.g., "AlsoNotifyAttribute").</param>
	/// <param name="attributeShortName">The attribute name without the "Attribute" suffix (e.g., "AlsoNotify").</param>
	/// <returns>The matching attributes, in declaration order.</returns>
	internal static IEnumerable<AttributeSyntax> FindAttributes(SyntaxList<AttributeListSyntax> attributeLists, string attributeFullName, string attributeShortName)
	{
		foreach (AttributeListSyntax attributeList in attributeLists)
		{
			foreach (AttributeSyntax attribute in attributeList.Attributes)
			{
				string name = attribute.Name.ToString();

				if (name == attributeFullName || name == attributeShortName)
					yield return attribute;
			}
		}
	}

	/// <summary>
	/// Gets the value of a named argument from attribute data.
	/// </summary>
	/// <typeparam name="T">The expected type of the argument value.</typeparam>
	/// <param name="attribute">The attribute data to inspect.</param>
	/// <param name="name">The name of the named argument.</param>
	/// <param name="defaultValue">The default value to return if the argument is not found.</param>
	/// <returns>The argument value, or <paramref name="defaultValue"/> if not found.</returns>
	internal static T GetNamedArgument<T>(AttributeData attribute, string name, T defaultValue)
	{
		foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
		{
			if (argument.Key == name && argument.Value.Value is T value)
				return value;
		}

		return defaultValue;
	}

	/// <summary>
	/// Gets the elements of an array-valued named argument from attribute data.
	/// </summary>
	/// <param name="attribute">The attribute data to inspect.</param>
	/// <param name="name">The name of the named argument.</param>
	/// <returns>The array elements, or <see langword="null"/> if the argument is not set.</returns>
	internal static ImmutableArray<TypedConstant>? GetNamedArrayArgument(AttributeData attribute, string name)
	{
		foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
		{
			if (argument.Key == name && argument.Value.Kind is TypedConstantKind.Array)
				return argument.Value.Values;
		}

		return null;
	}

	/// <summary>
	/// Gets the single constructor argument of the first attribute on <paramref name="symbol"/> whose
	/// class matches <paramref name="attributeName"/> and has exactly one argument of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The expected type of the argument value.</typeparam>
	/// <param name="symbol">The symbol to inspect.</param>
	/// <param name="attributeName">The fully qualified name of the attribute.</param>
	/// <param name="value">When this method returns <see langword="true"/>, contains the argument value.</param>
	/// <returns><see langword="true"/> if a matching attribute was found; otherwise, <see langword="false"/>.</returns>
	internal static bool TryGetSingleCtorArg<T>(ISymbol symbol, string attributeName, out T value)
	{
		foreach (AttributeData attributeData in symbol.GetAttributes())
		{
			if (attributeData.AttributeClass?.ToDisplayString() == attributeName
				&& attributeData.ConstructorArguments.Length == 1
				&& attributeData.ConstructorArguments[0].Value is T typedValue)
			{
				value = typedValue;
				return true;
			}
		}

		value = default!;
		return false;
	}

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
	/// Converts a field name to a property name by removing a leading underscore and
	/// capitalizing the first letter.
	/// </summary>
	/// <param name="fieldName">The field name to convert.</param>
	/// <returns>The converted property name, or an empty string if nothing remains.</returns>
	internal static string FieldNameToPropertyName(string fieldName)
	{
		string name = fieldName.StartsWith("_", StringComparison.OrdinalIgnoreCase) ? fieldName[1..] : fieldName;
		return name.Length == 0 ? name : $"{char.ToUpperInvariant(name[0])}{name[1..]}";
	}

	/// <summary>
	/// Converts a property name to a backing field name by prefixing an underscore
	/// and lowercasing the first letter.
	/// </summary>
	/// <param name="propertyName">The property name to convert.</param>
	/// <returns>The converted field name.</returns>
	internal static string PropertyNameToFieldName(string propertyName)
		=> $"_{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}";

	/// <summary>
	/// Gets the value of a positional constructor argument from an attribute on a type symbol.
	/// Searches the attributes of <paramref name="typeSymbol"/> for one whose class name matches
	/// <paramref name="attributeFullName"/>, then returns the constructor argument at <paramref name="index"/>.
	/// </summary>
	/// <typeparam name="T">The expected type of the constructor argument value.</typeparam>
	/// <param name="typeSymbol">The type symbol to inspect.</param>
	/// <param name="attributeFullName">The simple type name of the attribute (e.g., "GenerateIniFileAttribute").</param>
	/// <param name="index">The zero-based index of the constructor argument.</param>
	/// <param name="defaultValue">The default value to return if the argument is not found.</param>
	/// <returns>The extracted value, or <paramref name="defaultValue"/> if not found.</returns>
	internal static T GetConstructorArgumentValue<T>(
		INamedTypeSymbol typeSymbol,
		string attributeFullName,
		int index,
		T defaultValue)
	{
		foreach (AttributeData attributeData in typeSymbol.GetAttributes())
		{
			if (attributeData.AttributeClass?.Name != attributeFullName)
				continue;

			return attributeData.ConstructorArguments.Length > index && attributeData.ConstructorArguments[index].Value is T typedValue
				? typedValue
				: defaultValue;
		}

		return defaultValue;
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
				isCloneable = HasAttribute(propertySymbol.Type, cloneableAttributeName);

			if (toStringFormatAttributeName is not null && TryGetSingleCtorArg(propertySymbol, toStringFormatAttributeName, out string fmt))
				formatString = fmt;

			if (toStringOrderAttributeName is not null && TryGetSingleCtorArg(propertySymbol, toStringOrderAttributeName, out int ord))
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
	/// Checks whether a symbol has an attribute with the given fully qualified name.
	/// </summary>
	/// <param name="symbol">The symbol to inspect.</param>
	/// <param name="attributeName">The fully qualified name of the attribute.</param>
	/// <returns><see langword="true"/> if the attribute is present; otherwise, <see langword="false"/>.</returns>
	internal static bool HasAttribute(ISymbol symbol, string attributeName)
	{
		foreach (AttributeData attr in symbol.GetAttributes())
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
