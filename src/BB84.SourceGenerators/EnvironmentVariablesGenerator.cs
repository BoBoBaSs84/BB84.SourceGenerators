// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Analyzers;
using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Extensions;
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Models;
using BB84.SourceGenerators.Requests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators;

/// <summary>
/// Responsible for generating static <c>Read</c> and <c>TryRead</c> methods for classes and record
/// classes marked with the <see cref="GenerateEnvironmentVariablesAttribute"/> attribute, which fill
/// the public properties from environment variables, and for implementing partial properties that
/// read (and optionally write) their environment variable on every access.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class EnvironmentVariablesGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateEnvironmentVariablesAttribute>();

	private static readonly string NameAttributeName = typeof(EnvironmentVariableNameAttribute).FullName;

	private static readonly string[] FileUsings =
	[
		NamespaceNames.System,
		NamespaceNames.SystemCollectionsGeneric,
		NamespaceNames.SystemGlobalization,
	];

	private const string NullLiteral = "null";
	private const string DefaultLiteral = "default";
	private const string NullForgivingDefaultLiteral = "default!";

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<EnvironmentVariablesRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassOrRecordClassNode, Transform, TrackingNames.EnvironmentVariables, Execute);

	private static GeneratorResult<EnvironmentVariablesRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not TypeDeclarationSyntax typeDeclaration || context.TargetSymbol is not INamedTypeSymbol typeSymbol)
			return null;

		TypeTargetInfo type = TypeTargetInfo.Create(typeDeclaration, typeSymbol);

		if (!typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
			return GeneratorResult<EnvironmentVariablesRequest>.FromDiagnostics(type.CreateDiagnostic(DiagnosticDescriptors.EnvironmentVariablesTypeNotPartialDiagnostic, type.Name));

		AttributeData attribute = context.Attributes[0];
		NamingOptions naming = GetNamingOptions(attribute);

		if (!IsValidName(naming.Prefix) || string.IsNullOrWhiteSpace(naming.Prefix))
			return GeneratorResult<EnvironmentVariablesRequest>.FromDiagnostics(type.CreateDiagnostic(DiagnosticDescriptors.EnvironmentVariablesInvalidNameDiagnostic, naming.Prefix, type.Name));

		bool includeInherited = GeneratorHelpers.GetNamedArgument(attribute, nameof(GenerateEnvironmentVariablesAttribute.IncludeInherited), false);
		bool writable = GeneratorHelpers.GetNamedArgument(attribute, nameof(GenerateEnvironmentVariablesAttribute.Writable), false);
		HashSet<string> excluded = GeneratorHelpers.GetExcludedProperties(attribute, nameof(GenerateEnvironmentVariablesAttribute.ExcludeProperties));

		// Names are compared case-insensitively because environment variables are case-insensitive on Windows.
		CollectContext collect = new(type, typeSymbol, context.SemanticModel, naming, new(StringComparer.OrdinalIgnoreCase), [], cancellationToken);

		List<EnvironmentVariablesPartialProperty> partialProperties = CollectPartialProperties(collect, excluded, writable);
		List<EnvironmentVariablesProperty> properties = typeSymbol.IsAbstract
			? []
			: CollectProperties(collect, excluded, includeInherited, partialProperties.Select(static p => p.Name));

		if (properties.Count > 0)
			ValidateConstruction(collect, properties);

		if (collect.Diagnostics.Count > 0)
			return GeneratorResult<EnvironmentVariablesRequest>.FromDiagnostics([.. collect.Diagnostics]);

		if (properties.Count == 0 && partialProperties.Count == 0)
			return null;

		EnvironmentVariablesRequest request = new(type, typeDeclaration is RecordDeclarationSyntax, properties.ToEquatableArray(), partialProperties.ToEquatableArray());

		return GeneratorResult<EnvironmentVariablesRequest>.FromRequest(request);
	}

	private static void Execute(SourceProductionContext context, EnvironmentVariablesRequest request)
	{
		TypeTargetInfo type = request.Type;

		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.EnvironmentVariablesGeneratorFullName, type.NamespaceName, type.OuterClasses, FileUsings);
		sb.OpenClassOrRecord(request.IsRecord, type.Accessibility, type.Name);

		bool hasPrecedingMembers = false;

		if (request.Properties.Count > 0)
		{
			AppendReadMethods(sb, type.Name, request.Properties);
			hasPrecedingMembers = true;
		}

		foreach (EnvironmentVariablesPartialProperty property in request.PartialProperties)
		{
			if (hasPrecedingMembers)
				sb.AppendLine();
			hasPrecedingMembers = true;

			AppendPartialProperty(sb, property);
		}

		sb.CloseClassOrRecord(request.IsRecord);
		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, type.Name, null, sb);
	}

	private static void AppendReadMethods(SourceBuilder sb, string typeName, EquatableArray<EnvironmentVariablesProperty> properties)
	{
		sb.AppendSummary($"Reads a new <see cref=\"{typeName}\"/> from the environment variables of the current process.");
		sb.AppendLine("/// <returns>The <see cref=\"" + typeName + "\"/> read from the environment variables.</returns>");
		sb.AppendLine("/// <exception cref=\"InvalidOperationException\">A mandatory environment variable is missing or a value is invalid.</exception>");
		sb.AppendLine($"public static {typeName} Read()");
		sb.Indent();
		sb.AppendLine("=> Read(Environment.GetEnvironmentVariable);");
		sb.Outdent();
		sb.AppendLine();

		sb.AppendSummary($"Reads a new <see cref=\"{typeName}\"/> from the environment variables returned by <paramref name=\"lookup\"/>.");
		sb.AppendLine("/// <param name=\"lookup\">The function returning the value of an environment variable, or <see langword=\"null\"/> if it is not set.</param>");
		sb.AppendLine("/// <returns>The <see cref=\"" + typeName + "\"/> read from the environment variables.</returns>");
		sb.AppendLine("/// <exception cref=\"ArgumentNullException\"><paramref name=\"lookup\"/> is <see langword=\"null\"/>.</exception>");
		sb.AppendLine("/// <exception cref=\"InvalidOperationException\">A mandatory environment variable is missing or a value is invalid.</exception>");
		sb.AppendLine($"public static {typeName} Read(Func<string, string?> lookup)");
		sb.OpenBrace();
		sb.AppendLine($"if (!TryRead(lookup, out {typeName}? result, out IReadOnlyList<string> errors))");
		sb.Indent();
		sb.AppendLine("throw new InvalidOperationException(string.Join(Environment.NewLine, errors));");
		sb.Outdent();
		sb.AppendLine();
		sb.AppendLine("return result!;");
		sb.CloseBrace();
		sb.AppendLine();

		sb.AppendSummary($"Tries to read a new <see cref=\"{typeName}\"/> from the environment variables of the current process.");
		sb.AppendLine("/// <param name=\"result\">The <see cref=\"" + typeName + "\"/> read, or <see langword=\"null\"/> if reading failed.</param>");
		sb.AppendLine("/// <param name=\"errors\">The errors of all missing mandatory environment variables and invalid values; empty on success.</param>");
		sb.AppendLine("/// <returns><see langword=\"true\"/> if all environment variables were read; otherwise, <see langword=\"false\"/>.</returns>");
		sb.AppendLine($"public static bool TryRead(out {typeName}? result, out IReadOnlyList<string> errors)");
		sb.Indent();
		sb.AppendLine("=> TryRead(Environment.GetEnvironmentVariable, out result, out errors);");
		sb.Outdent();
		sb.AppendLine();

		sb.AppendSummary($"Tries to read a new <see cref=\"{typeName}\"/> from the environment variables returned by <paramref name=\"lookup\"/>.");
		sb.AppendLine("/// <param name=\"lookup\">The function returning the value of an environment variable, or <see langword=\"null\"/> if it is not set.</param>");
		sb.AppendLine("/// <param name=\"result\">The <see cref=\"" + typeName + "\"/> read, or <see langword=\"null\"/> if reading failed.</param>");
		sb.AppendLine("/// <param name=\"errors\">The errors of all missing mandatory environment variables and invalid values; empty on success.</param>");
		sb.AppendLine("/// <returns><see langword=\"true\"/> if all environment variables were read; otherwise, <see langword=\"false\"/>.</returns>");
		sb.AppendLine("/// <exception cref=\"ArgumentNullException\"><paramref name=\"lookup\"/> is <see langword=\"null\"/>.</exception>");
		sb.AppendLine($"public static bool TryRead(Func<string, string?> lookup, out {typeName}? result, out IReadOnlyList<string> errors)");
		sb.OpenBrace();
		sb.AppendLine("if (lookup is null)");
		sb.Indent();
		sb.AppendLine("throw new ArgumentNullException(nameof(lookup));");
		sb.Outdent();
		sb.AppendLine();
		sb.AppendLine("List<string> errorList = new List<string>();");

		foreach (EnvironmentVariablesProperty property in properties)
			AppendReadProperty(sb, property);

		sb.AppendLine();
		sb.AppendLine("if (errorList.Count > 0)");
		sb.OpenBrace();
		sb.AppendLine("result = null;");
		sb.AppendLine("errors = errorList;");
		sb.AppendLine("return false;");
		sb.CloseBrace();
		sb.AppendLine();
		sb.AppendLine($"result = new {typeName}");
		sb.OpenBrace();
		foreach (EnvironmentVariablesProperty property in properties.Where(static p => !p.AssignAfterConstruction))
			sb.AppendLine($"{property.Name} = {GetLocalName(property.Name, "Value")},");
		sb.Outdent();
		sb.AppendLine("};");

		foreach (EnvironmentVariablesProperty property in properties.Where(static p => p.AssignAfterConstruction))
		{
			sb.AppendLine($"if ({GetLocalName(property.Name, "Found")})");
			sb.Indent();
			sb.AppendLine($"result.{property.Name} = {GetLocalName(property.Name, "Value")};");
			sb.Outdent();
		}

		sb.AppendLine();
		sb.AppendLine("errors = Array.Empty<string>();");
		sb.AppendLine("return true;");
		sb.CloseBrace();
	}

	private static void AppendReadProperty(SourceBuilder sb, EnvironmentVariablesProperty property)
	{
		string raw = GetLocalName(property.Name, "Raw");
		string value = GetLocalName(property.Name, "Value");
		string parsed = GetLocalName(property.Name, "Parsed");
		string found = GetLocalName(property.Name, "Found");
		ValueConversion conversion = property.Conversion;
		bool isString = conversion.Kind == ValueConversionKind.String;

		sb.AppendLine();
		sb.AppendLine($"string? {raw} = lookup(\"{GeneratorHelpers.EscapeString(property.VariableName)}\");");
		sb.AppendLine($"{property.TypeName} {value} = {property.InitialValue};");
		if (property.AssignAfterConstruction)
			sb.AppendLine($"bool {found} = false;");

		string assign = isString ? $"{value} = {raw}!;" : $"{value} = {parsed};";
		string invalid = $"errorList.Add({InvalidMessage(property.VariableName, conversion.DisplayName)});";
		string tryParse = isString ? string.Empty : ValueConversions.GetTryParseExpression(conversion.Kind, conversion.UnderlyingTypeName, $"{raw}!", parsed);

		if (property.IsMandatory)
		{
			sb.AppendLine($"if (string.IsNullOrWhiteSpace({raw}))");
			sb.Indent();
			sb.AppendLine($"errorList.Add({MissingMessage(property.VariableName)});");
			sb.Outdent();

			if (!isString)
			{
				sb.AppendLine($"else if (!{tryParse})");
				sb.Indent();
				sb.AppendLine(invalid);
				sb.Outdent();
			}

			sb.AppendLine("else");
			AppendAssignment(sb, assign, property.AssignAfterConstruction ? $"{found} = true;" : null);
			return;
		}

		sb.AppendLine($"if (!string.IsNullOrWhiteSpace({raw}))");

		if (isString)
		{
			AppendAssignment(sb, assign, property.AssignAfterConstruction ? $"{found} = true;" : null);
			return;
		}

		sb.OpenBrace();
		sb.AppendLine($"if (!{tryParse})");
		sb.Indent();
		sb.AppendLine(invalid);
		sb.Outdent();
		sb.AppendLine("else");
		AppendAssignment(sb, assign, property.AssignAfterConstruction ? $"{found} = true;" : null);
		sb.CloseBrace();
	}

	private static void AppendAssignment(SourceBuilder sb, string assign, string? found)
	{
		if (found is null)
		{
			sb.Indent();
			sb.AppendLine(assign);
			sb.Outdent();
			return;
		}

		sb.OpenBrace();
		sb.AppendLine(assign);
		sb.AppendLine(found);
		sb.CloseBrace();
	}

	private static void AppendPartialProperty(SourceBuilder sb, EnvironmentVariablesPartialProperty property)
	{
		string variableName = $"\"{GeneratorHelpers.EscapeString(property.VariableName)}\"";
		ValueConversion conversion = property.Conversion;

		sb.AppendLine($"{property.Modifiers} {property.TypeName} {property.Name}");
		sb.OpenBrace();

		if (property.GetterPrefix is not null)
		{
			sb.AppendLine($"{property.GetterPrefix}get");
			sb.OpenBrace();
			sb.AppendLine($"string? raw = Environment.GetEnvironmentVariable({variableName});");
			sb.AppendLine("if (string.IsNullOrWhiteSpace(raw))");
			sb.Indent();
			sb.AppendLine(property.IsNullable
				? "return null;"
				: $"throw new InvalidOperationException({MissingMessage(property.VariableName)});");
			sb.Outdent();
			sb.AppendLine();

			if (conversion.Kind == ValueConversionKind.String)
			{
				sb.AppendLine("return raw!;");
			}
			else
			{
				sb.AppendLine($"if (!{ValueConversions.GetTryParseExpression(conversion.Kind, conversion.UnderlyingTypeName, "raw!", "parsed")})");
				sb.Indent();
				sb.AppendLine($"throw new InvalidOperationException({InvalidMessage(property.VariableName, conversion.DisplayName)});");
				sb.Outdent();
				sb.AppendLine();
				sb.AppendLine("return parsed;");
			}

			sb.CloseBrace();
		}

		if (property.SetterPrefix is not null)
		{
			string format = conversion.Kind == ValueConversionKind.String
				? "value"
				: conversion.IsNullableValueType
					? $"value.HasValue ? {ValueConversions.GetFormatExpression(conversion.Kind, "value.Value")} : null"
					: ValueConversions.GetFormatExpression(conversion.Kind, "value");

			sb.AppendLine($"{property.SetterPrefix}{property.SetterKeyword} => Environment.SetEnvironmentVariable({variableName}, {format});");
		}

		sb.CloseBrace();
	}

	private static string MissingMessage(string variableName)
		=> $"\"Missing required environment variable '{GeneratorHelpers.EscapeString(variableName)}'.\"";

	private static string InvalidMessage(string variableName, string displayName)
		=> $"\"Environment variable '{GeneratorHelpers.EscapeString(variableName)}' has an invalid {displayName} value.\"";

	private static string GetLocalName(string propertyName, string suffix)
		=> $"{NamingHelpers.ToCamelCase(propertyName)}{suffix}";

	private static NamingOptions GetNamingOptions(AttributeData attribute)
	{
		string prefix = attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string value ? value : string.Empty;
		string separator = GeneratorHelpers.GetNamedArgument(attribute, nameof(GenerateEnvironmentVariablesAttribute.Separator), "_");
		EnvironmentVariableNamingStyle style = (EnvironmentVariableNamingStyle)GeneratorHelpers.GetNamedArgument(attribute, nameof(GenerateEnvironmentVariablesAttribute.NamingStyle), (int)EnvironmentVariableNamingStyle.UpperSnakeCase);

		return new NamingOptions(prefix, separator, style);
	}

	private static string GetVariableName(IPropertySymbol property, NamingOptions naming)
	{
		if (GeneratorHelpers.TryGetSingleCtorArg(property, NameAttributeName, out string name))
			return name;

		return naming.Style == EnvironmentVariableNamingStyle.AsIs
			? $"{naming.Prefix}{naming.Separator}{property.Name}"
			: $"{naming.Prefix.ToUpperInvariant()}{naming.Separator}{NamingHelpers.ToUpperSnakeCase(property.Name)}";
	}

	private static bool IsValidName(string name)
		=> name.Length > 0 && name.IndexOf('=') < 0 && name.IndexOf('\0') < 0;

	private static bool IsNullable(IPropertySymbol property)
		=> property.NullableAnnotation == NullableAnnotation.Annotated
			|| property.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

	private static LocationInfo? GetLocation(ISymbol symbol, TypeTargetInfo type)
		=> symbol.Locations.FirstOrDefault(static l => l.IsInSource) is Location location
			? LocationInfo.From(location)
			: type.IdentifierLocation;

	/// <summary>
	/// Resolves the variable name and the conversion of the property, adding diagnostics for an
	/// invalid name or an unsupported type.
	/// </summary>
	private static (string VariableName, ValueConversion Conversion)? ResolveProperty(CollectContext collect, IPropertySymbol property)
	{
		string variableName = GetVariableName(property, collect.Naming);
		LocationInfo? location = GetLocation(property, collect.Type);
		bool isValid = true;

		if (!IsValidName(variableName))
		{
			collect.Diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.EnvironmentVariablesInvalidNameDiagnostic, location, variableName, property.Name));
			isValid = false;
		}
		else if (collect.Names.TryGetValue(variableName, out string? other))
		{
			collect.Diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.EnvironmentVariablesDuplicateNameDiagnostic, location, other, property.Name, variableName));
			isValid = false;
		}
		else
		{
			collect.Names[variableName] = property.Name;
		}

		if (!ValueConversions.TryGetKind(property.Type, out ValueConversionKind kind))
		{
			collect.Diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.EnvironmentVariablesUnsupportedTypeDiagnostic, location, property.Name, property.Type.ToDisplayString()));
			return null;
		}

		if (!isValid)
			return null;

		return (variableName, ValueConversions.Create(property.Type, kind));
	}

	private static List<EnvironmentVariablesPartialProperty> CollectPartialProperties(CollectContext collect, HashSet<string> excluded, bool writable)
	{
		List<EnvironmentVariablesPartialProperty> properties = [];

		foreach (IPropertySymbol property in collect.TypeSymbol.GetMembers().OfType<IPropertySymbol>())
		{
			if (!property.IsPartialDefinition || property.PartialImplementationPart is not null || property.IsIndexer || excluded.Contains(property.Name))
				continue;

			if (property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(collect.CancellationToken) is not PropertyDeclarationSyntax { AccessorList: { } accessors } syntax)
				continue;

			if (ResolveProperty(collect, property) is not (string variableName, ValueConversion conversion))
				continue;

			AccessorDeclarationSyntax? getter = accessors.Accessors.FirstOrDefault(static a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
			AccessorDeclarationSyntax? setter = accessors.Accessors.FirstOrDefault(static a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration));

			if (setter is not null && !writable)
			{
				collect.Diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.EnvironmentVariablesSetterNotWritableDiagnostic, LocationInfo.From(syntax.Identifier), property.Name, collect.Type.Name));
				continue;
			}

			properties.Add(new EnvironmentVariablesPartialProperty(
				$"{syntax.Modifiers}",
				property.Name,
				variableName,
				property.Type.ToPropertyTypeDisplayString(),
				conversion,
				IsNullable(property),
				getter is null ? null : GetAccessorPrefix(getter),
				setter is null ? null : GetAccessorPrefix(setter),
				setter?.Keyword.Text ?? "set"));
		}

		return properties;
	}

	private static List<EnvironmentVariablesProperty> CollectProperties(CollectContext collect, HashSet<string> excluded, bool includeInherited, IEnumerable<string> partialPropertyNames)
	{
		List<EnvironmentVariablesProperty> properties = [];
		HashSet<string> seen = new(partialPropertyNames, StringComparer.Ordinal);
		Compilation compilation = collect.SemanticModel.Compilation;

		foreach (IPropertySymbol property in GeneratorHelpers.GetPublicPropertySymbols(collect.TypeSymbol, includeInherited, requireGetter: false))
		{
			// The most derived declaration wins over overridden or hidden base properties.
			if (!seen.Add(property.Name) || excluded.Contains(property.Name))
				continue;

			// Unimplemented partial properties of the type itself are handled as live properties.
			if (property.IsPartialDefinition && property.PartialImplementationPart is null && SymbolEqualityComparer.Default.Equals(property.ContainingType, collect.TypeSymbol))
				continue;

			// Computed properties (expression or accessor bodies without a setter) are not settings.
			if (property.SetMethod is null && GeneratorHelpers.IsComputedProperty(property, collect.CancellationToken))
				continue;

			LocationInfo? location = GetLocation(property, collect.Type);

			if (property.SetMethod is not IMethodSymbol setter || !compilation.IsSymbolAccessibleWithin(setter, collect.TypeSymbol))
			{
				collect.Diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.EnvironmentVariablesPropertyNotAssignableDiagnostic, location, property.Name, collect.Type.Name));
				continue;
			}

			if (ResolveProperty(collect, property) is not (string variableName, ValueConversion conversion))
				continue;

			bool isNullable = IsNullable(property);
			EqualsValueClauseSyntax? initializer = GeneratorHelpers.GetPropertyInitializer(property, collect.CancellationToken);
			string initialValue = isNullable ? NullLiteral : property.Type.IsValueType ? DefaultLiteral : NullForgivingDefaultLiteral;
			bool assignAfterConstruction = false;

			if (initializer is not null)
			{
				if (!setter.IsInitOnly && !property.IsRequired)
				{
					assignAfterConstruction = true;
				}
				else if (GetConstantValue(compilation, initializer.Value, collect) is { HasValue: true } constant)
				{
					initialValue = GeneratorHelpers.FormatLiteral(constant.Value, property.Type);
				}
				else
				{
					collect.Diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.EnvironmentVariablesNonConstantInitializerDiagnostic, location, property.Name));
					continue;
				}
			}

			properties.Add(new EnvironmentVariablesProperty(
				property.Name,
				variableName,
				property.Type.ToPropertyTypeDisplayString(),
				conversion,
				IsMandatory: !isNullable && initializer is null,
				initialValue,
				assignAfterConstruction));
		}

		return properties;
	}

	private static Optional<object?> GetConstantValue(Compilation compilation, ExpressionSyntax expression, CollectContext collect)
	{
		SemanticModel semanticModel = expression.SyntaxTree == collect.SemanticModel.SyntaxTree
			? collect.SemanticModel
			: compilation.GetSemanticModel(expression.SyntaxTree);

		return semanticModel.GetConstantValue(expression, collect.CancellationToken);
	}

	/// <summary>
	/// Adds diagnostics when the generated object initializer cannot create the type: no
	/// parameterless constructor, or a required member that is not read.
	/// </summary>
	private static void ValidateConstruction(CollectContext collect, List<EnvironmentVariablesProperty> properties)
	{
		INamedTypeSymbol typeSymbol = collect.TypeSymbol;

		if (!typeSymbol.InstanceConstructors.Any(static c => c.Parameters.All(static p => p.IsOptional || p.IsParams)))
			collect.Diagnostics.Add(collect.Type.CreateDiagnostic(DiagnosticDescriptors.EnvironmentVariablesNoParameterlessConstructorDiagnostic, collect.Type.Name));

		HashSet<string> read = [.. properties.Select(static p => p.Name)];
		HashSet<string> reported = new(StringComparer.Ordinal);

		for (INamedTypeSymbol? current = typeSymbol; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
		{
			foreach (ISymbol member in current.GetMembers())
			{
				if (member is not (IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true }) || read.Contains(member.Name) || !reported.Add(member.Name))
					continue;

				collect.Diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.EnvironmentVariablesRequiredMemberNotReadDiagnostic, GetLocation(member, collect.Type), member.Name, collect.Type.Name));
			}
		}
	}

	private static string GetAccessorPrefix(AccessorDeclarationSyntax accessor)
		=> accessor.Modifiers.Count == 0 ? string.Empty : $"{accessor.Modifiers} ";

	/// <summary>
	/// The naming options of the attribute.
	/// </summary>
	/// <param name="Prefix">The prefix of the environment variable names.</param>
	/// <param name="Separator">The separator between the prefix and the property name.</param>
	/// <param name="Style">The naming style.</param>
	private sealed record NamingOptions(string Prefix, string Separator, EnvironmentVariableNamingStyle Style);

	/// <summary>
	/// The state shared while collecting the properties of a type in the transform stage. Holds symbols,
	/// so it must never be cached.
	/// </summary>
	/// <param name="Type">The type description.</param>
	/// <param name="TypeSymbol">The type symbol.</param>
	/// <param name="SemanticModel">The semantic model of the attributed declaration.</param>
	/// <param name="Naming">The naming options.</param>
	/// <param name="Names">The resolved environment variable names, mapped to their property names.</param>
	/// <param name="Diagnostics">The collected diagnostics.</param>
	/// <param name="CancellationToken">The cancellation token.</param>
	private sealed record CollectContext(
		TypeTargetInfo Type,
		INamedTypeSymbol TypeSymbol,
		SemanticModel SemanticModel,
		NamingOptions Naming,
		Dictionary<string, string> Names,
		List<DiagnosticInfo> Diagnostics,
		CancellationToken CancellationToken);
}
