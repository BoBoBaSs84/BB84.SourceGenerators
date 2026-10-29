// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;

using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Models;
using BB84.SourceGenerators.Requests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators;

/// <summary>
/// Responsible for generating a <c>ToString()</c> override for classes marked
/// with the <see cref="GenerateToStringAttribute"/> attribute.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ToStringGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateToStringAttribute>();

	private static readonly string[] FileUsings =
	[
		NamespaceNames.System,
		NamespaceNames.SystemCollectionsGeneric,
	];

	private static readonly string ToStringFormatAttributeName = typeof(ToStringFormatAttribute).FullName;
	private static readonly string ToStringOrderAttributeName = typeof(ToStringOrderAttribute).FullName;

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<ToStringRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassOrRecordClassNode, Transform, TrackingNames.ToStringOverride, Execute);

	private static GeneratorResult<ToStringRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not TypeDeclarationSyntax typeDeclaration || context.TargetSymbol is not INamedTypeSymbol typeSymbol)
			return null;

		SemanticModel semanticModel = context.SemanticModel;
		bool isRecord = typeDeclaration is RecordDeclarationSyntax;

		CollectionFormat collectionFormat = GetCollectionFormat(typeDeclaration, semanticModel);
		string separator = GetSeparator(typeDeclaration, semanticModel);
		bool formattable = GetFormattable(typeDeclaration, semanticModel);
		string? nullPlaceholder = GetNullPlaceholder(typeDeclaration, semanticModel);
		bool includeInherited = GetIncludeInherited(typeDeclaration, semanticModel);
		HashSet<string> excludedProperties = GeneratorHelpers.GetExcludedProperties(typeDeclaration, semanticModel, nameof(GenerateToStringAttribute));
		ImmutableArray<PropertyDescriptor> properties = GeneratorHelpers.GetPropertyDescriptors(typeSymbol, excludedProperties, detectCollections: true, toStringFormatAttributeName: ToStringFormatAttributeName, toStringOrderAttributeName: ToStringOrderAttributeName, includeInherited: includeInherited);
		bool printMembersIsOverride = isRecord
			&& typeSymbol.BaseType is { IsRecord: true, SpecialType: not SpecialType.System_Object };

		return GeneratorResult<ToStringRequest>.FromRequest(new ToStringRequest(
			TypeTargetInfo.Create(typeDeclaration, typeSymbol),
			isRecord,
			properties.ToEquatableArray(),
			collectionFormat,
			separator,
			formattable,
			nullPlaceholder,
			printMembersIsOverride));
	}

	private static void Execute(SourceProductionContext context, ToStringRequest request)
	{
		TypeTargetInfo type = request.Type;
		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.ToStringGeneratorFullName, type.NamespaceName, type.OuterClasses, FileUsings);

		AppendPartialType(sb, type.Name, type.Accessibility, request.Properties.AsImmutableArray(), request.CollectionFormat, request.Separator, request.Formattable, request.NullPlaceholder, request.IsRecord, request.PrintMembersIsOverride);

		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, type.Name, "ToString", sb);
	}

	private static void AppendPartialType(SourceBuilder sb, string className, string accessibility, ImmutableArray<PropertyDescriptor> properties, CollectionFormat collectionFormat, string separator, bool formattable, string? nullPlaceholder, bool isRecord, bool printMembersIsOverride)
	{
		bool hasDictionary = collectionFormat == CollectionFormat.Elements
			&& properties.Any(static p => p.CollectionKind == CollectionKind.Dictionary);

		sb.OpenClassOrRecord(isRecord, accessibility, className, formattable ? "IFormattable" : null);

		if (formattable)
			AppendFormattableToString(sb, className, properties, collectionFormat, separator, nullPlaceholder);
		else if (isRecord)
			AppendPrintMembers(sb, properties, collectionFormat, separator, nullPlaceholder, printMembersIsOverride);
		else
			AppendToString(sb, className, properties, collectionFormat, separator, nullPlaceholder);

		if (hasDictionary)
		{
			sb.AppendLine();
			AppendDictionaryFormatHelper(sb);
		}

		sb.CloseClassOrRecord(isRecord);
	}

	private static void AppendFormattableToString(SourceBuilder sb, string className, ImmutableArray<PropertyDescriptor> properties, CollectionFormat collectionFormat, string separator, string? nullPlaceholder)
	{
		sb.AppendLine("/// <inheritdoc/>");
		sb.AppendLine("public override string ToString()");
		sb.Indent();
		sb.AppendLine("=> ToString(null, null);");
		sb.Outdent();
		sb.AppendLine();
		sb.AppendLine("/// <inheritdoc/>");
		sb.AppendLine("public string ToString(string? format, IFormatProvider? formatProvider)");
		sb.OpenBrace();
		AppendToStringBody(sb, className, properties, collectionFormat, separator, nullPlaceholder, formattable: true);
		sb.CloseBrace();
	}

	private static void AppendPrintMembers(SourceBuilder sb, ImmutableArray<PropertyDescriptor> properties, CollectionFormat collectionFormat, string separator, string? nullPlaceholder, bool printMembersIsOverride)
	{
		string modifier = printMembersIsOverride ? "protected override" : "protected virtual";

		sb.AppendSummary("Appends this record's generated member values to <paramref name=\"builder\"/>.");
		sb.AppendLine("/// <param name=\"builder\">The builder to append formatted member values to.</param>");
		sb.AppendLine("/// <returns><see langword=\"true\"/> if any members were appended; otherwise, <see langword=\"false\"/>.</returns>");
		sb.AppendLine($"{modifier} bool PrintMembers(System.Text.StringBuilder builder)");
		sb.OpenBrace();
		AppendPrintMembersBody(sb, properties, collectionFormat, separator, nullPlaceholder, printMembersIsOverride);
		sb.CloseBrace();
	}

	private static void AppendToString(SourceBuilder sb, string className, ImmutableArray<PropertyDescriptor> properties, CollectionFormat collectionFormat, string separator, string? nullPlaceholder)
	{
		sb.AppendLine("/// <inheritdoc/>");
		sb.AppendLine("public override string ToString()");
		sb.OpenBrace();
		AppendToStringBody(sb, className, properties, collectionFormat, separator, nullPlaceholder, formattable: false);
		sb.CloseBrace();
	}

	private static void AppendCollectionLocals(SourceBuilder sb, ImmutableArray<PropertyDescriptor> properties, CollectionFormat collectionFormat)
	{
		foreach (PropertyDescriptor prop in properties)
		{
			if (prop.CollectionKind == CollectionKind.None)
				continue;
			sb.AppendLine($"string {prop.Name.ToLowerInvariant()} = {BuildCollectionFormatExpression(prop, collectionFormat)};");
		}
	}

	private static void AppendToStringBody(SourceBuilder sb, string className, ImmutableArray<PropertyDescriptor> properties, CollectionFormat collectionFormat, string separator, string? nullPlaceholder, bool formattable)
	{
		if (properties.Length == 0)
		{
			sb.AppendLine($"return \"{className} {{ }}\";");
			return;
		}

		AppendCollectionLocals(sb, properties, collectionFormat);

		string escapedSeparator = GeneratorHelpers.EscapeString(separator);
		sb.AppendLine("System.Text.StringBuilder sb = new();");
		sb.AppendLine($"sb.Append(\"{className} {{ \");");

		for (int i = 0; i < properties.Length; i++)
		{
			if (i > 0)
				sb.AppendLine($"sb.Append(\"{escapedSeparator}\");");

			PropertyDescriptor prop = properties[i];
			sb.AppendLine($"sb.Append($\"{{nameof({prop.Name})}} = \");");
			sb.AppendLine($"sb.Append({GetValueExpression(prop, nullPlaceholder, formattable)});");
		}

		sb.AppendLine("sb.Append(\" }\");");
		sb.AppendLine("return sb.ToString();");
	}

	private static void AppendPrintMembersBody(SourceBuilder sb, ImmutableArray<PropertyDescriptor> properties, CollectionFormat collectionFormat, string separator, string? nullPlaceholder, bool isOverride)
	{
		AppendCollectionLocals(sb, properties, collectionFormat);

		string escapedSeparator = GeneratorHelpers.EscapeString(separator);

		sb.AppendLine(isOverride ? "bool appended = base.PrintMembers(builder);" : "bool appended = false;");

		foreach (PropertyDescriptor prop in properties)
		{
			sb.AppendLine("if (appended)");
			sb.Indent();
			sb.AppendLine($"builder.Append(\"{escapedSeparator}\");");
			sb.Outdent();
			sb.AppendLine($"builder.Append($\"{{nameof({prop.Name})}} = \");");
			sb.AppendLine($"builder.Append({GetValueExpression(prop, nullPlaceholder, formattable: false)});");
			sb.AppendLine("appended = true;");
		}

		sb.AppendLine("return appended;");
	}

	private static string GetValueExpression(PropertyDescriptor prop, string? nullPlaceholder, bool formattable)
	{
		if (prop.CollectionKind != CollectionKind.None)
			return prop.Name.ToLowerInvariant();

		if (formattable && prop.IsFormattable)
		{
			if (nullPlaceholder is not null && prop.IsNullable)
			{
				string escaped = GeneratorHelpers.EscapeString(nullPlaceholder);
				return $"({prop.Name}?.ToString(format, formatProvider) ?? \"{escaped}\")";
			}
			string nullConditional = prop.IsNullable ? "?" : "";
			return $"{prop.Name}{nullConditional}.ToString(format, formatProvider)";
		}

		if (nullPlaceholder is not null && prop.IsNullable)
		{
			string escaped = GeneratorHelpers.EscapeString(nullPlaceholder);
			return prop.FormatString is not null
				? $"({prop.Name}?.ToString(\"{prop.FormatString}\") ?? \"{escaped}\")"
				: $"({prop.Name}?.ToString() ?? \"{escaped}\")";
		}

		return prop.FormatString is not null
			? prop.IsNullable
				? $"{prop.Name}?.ToString(\"{prop.FormatString}\")"
				: $"{prop.Name}.ToString(\"{prop.FormatString}\")"
			: prop.Name;
	}

	private static string BuildCollectionFormatExpression(PropertyDescriptor prop, CollectionFormat format)
	{
		bool isImmutableArray = prop.CollectionKind == CollectionKind.ImmutableArray;
		bool isArray = prop.CollectionKind == CollectionKind.Array;
		bool isDictionary = prop.CollectionKind == CollectionKind.Dictionary;

		// ImmutableArray<T> (non-nullable struct): IsValueType = true
		// ImmutableArray<T>? (Nullable<ImmutableArray<T>>): IsValueType = false
		bool isNullableImmutableArray = isImmutableArray && !prop.IsValueType;
		bool isNonNullableImmutableArray = isImmutableArray && prop.IsValueType;

		// Arrays and ImmutableArrays use .Length; all other collection types use .Count
		bool usesLength = isArray || isImmutableArray;

		// Expression for the non-null collection value
		string directAccess = isNullableImmutableArray ? $"{prop.Name}.Value" : prop.Name;
		string countAccess = usesLength ? $"{directAccess}.Length" : $"{directAccess}.Count";

		// Null guard (null means no null check required for non-nullable value types)
		string? nullGuard = isNonNullableImmutableArray
			? null
			: isNullableImmutableArray
				? $"!{prop.Name}.HasValue"
				: $"{prop.Name} == null";

		string WrapGuard(string valueExpr)
			=> nullGuard is not null ? $"{nullGuard} ? \"null\" : {valueExpr}" : valueExpr;

		return format switch
		{
			CollectionFormat.Elements when isDictionary => $"DictionaryToString({prop.Name})",// The DictionaryToString helper handles null internally
			CollectionFormat.Elements => WrapGuard($"\"[\" + string.Join(\", \", {directAccess}) + \"]\""),
			CollectionFormat.Count => WrapGuard($"\"Count = \" + {countAccess}"),
			CollectionFormat.TypeAndCount => WrapGuard($"{directAccess}.GetType().Name + \" (Count = \" + {countAccess} + \")\""),
			_ => prop.Name,
		};
	}

	private static void AppendDictionaryFormatHelper(SourceBuilder sb)
	{
		sb.AppendLine("private static string DictionaryToString<TKey, TValue>(IDictionary<TKey, TValue>? dictionary)");
		sb.OpenBrace();
		sb.AppendLine("if (dictionary == null)");
		sb.Indent();
		sb.AppendLine("return \"null\";");
		sb.Outdent();
		sb.AppendLine("List<string> parts = new List<string>(dictionary.Count);");
		sb.AppendLine("foreach (KeyValuePair<TKey, TValue> kvp in dictionary)");
		sb.Indent();
		sb.AppendLine("parts.Add(kvp.Key + \": \" + kvp.Value);");
		sb.Outdent();
		sb.AppendLine("return \"{\" + string.Join(\", \", parts) + \"}\";");
		sb.CloseBrace();
	}

	private static bool GetFormattable(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel)
		=> GeneratorHelpers.GetNamedArgumentValue(classDeclaration, semanticModel, AttributeNames.ShortName, AttributeNames.TypeName, nameof(GenerateToStringAttribute.Formattable), false);

	private static string GetSeparator(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel)
		=> GeneratorHelpers.GetNamedArgumentValue(classDeclaration, semanticModel, AttributeNames.ShortName, AttributeNames.TypeName, nameof(GenerateToStringAttribute.Separator), ", ");

	private static CollectionFormat GetCollectionFormat(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel)
		=> (CollectionFormat)GeneratorHelpers.GetNamedArgumentValue(classDeclaration, semanticModel, AttributeNames.ShortName, AttributeNames.TypeName, nameof(GenerateToStringAttribute.CollectionFormat), (int)CollectionFormat.Count);

	private static string? GetNullPlaceholder(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel)
		=> GeneratorHelpers.GetNamedArgumentValue<string?>(classDeclaration, semanticModel, AttributeNames.ShortName, AttributeNames.TypeName, nameof(GenerateToStringAttribute.NullPlaceholder), null);

	private static bool GetIncludeInherited(TypeDeclarationSyntax classDeclaration, SemanticModel semanticModel)
		=> GeneratorHelpers.GetNamedArgumentValue(classDeclaration, semanticModel, AttributeNames.ShortName, AttributeNames.TypeName, nameof(GenerateToStringAttribute.IncludeInherited), false);
}
