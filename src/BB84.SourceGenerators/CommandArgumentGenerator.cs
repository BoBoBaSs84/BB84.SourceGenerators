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
/// A source generator that creates a private constructor, static <c>Parse</c> and <c>TryParse</c>
/// methods and a <c>Usage</c> constant for classes and record classes marked with the
/// <see cref="GenerateCommandArgumentAttribute"/>, parsing command line arguments of the form
/// <c>--propertyName value</c> into their public properties.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CommandArgumentGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateCommandArgumentAttribute>();

	private static readonly string[] FileUsings =
	[
		NamespaceNames.System,
		NamespaceNames.SystemCollectionsGeneric,
		NamespaceNames.SystemGlobalization,
	];

	private const string OptionPrefix = "--";
	private const string UsageIndent = "  ";
	private const int UsageColumnGap = 3;

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<CommandArgumentRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassOrRecordClassNode, Transform, TrackingNames.CommandArgument, Execute);

	private static GeneratorResult<CommandArgumentRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not TypeDeclarationSyntax typeDeclaration || context.TargetSymbol is not INamedTypeSymbol typeSymbol)
			return null;

		TypeTargetInfo type = TypeTargetInfo.Create(typeDeclaration, typeSymbol);

		if (!typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
			return GeneratorResult<CommandArgumentRequest>.FromDiagnostics(type.CreateDiagnostic(DiagnosticDescriptors.CommandArgumentTypeNotPartialDiagnostic, type.Name));

		if (typeSymbol.IsAbstract || typeSymbol.IsStatic)
			return null;

		List<DiagnosticInfo> diagnostics = [];

		foreach (IMethodSymbol constructor in typeSymbol.InstanceConstructors)
		{
			if (!constructor.IsImplicitlyDeclared && constructor.DeclaredAccessibility != Accessibility.Private)
				diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.CommandArgumentNonPrivateConstructorDiagnostic, GetLocation(constructor, type), type.Name));
		}

		HashSet<string> excluded = GeneratorHelpers.GetExcludedProperties(context.Attributes[0], nameof(GenerateCommandArgumentAttribute.ExcludeProperties));
		List<CommandArgumentOption> options = CollectOptions(context.SemanticModel, typeSymbol, type, excluded, diagnostics, cancellationToken);

		if (diagnostics.Count > 0)
			return GeneratorResult<CommandArgumentRequest>.FromDiagnostics([.. diagnostics]);

		CommandArgumentRequest request = new(
			type,
			typeDeclaration is RecordDeclarationSyntax,
			context.SemanticModel.Compilation.AssemblyName ?? type.Name,
			HasRequiredMembers(typeSymbol),
			options.Any(static o => o.Conversion.Kind == ValueConversionKind.Encoding),
			options.ToEquatableArray());

		return GeneratorResult<CommandArgumentRequest>.FromRequest(request);
	}

	private static void Execute(SourceProductionContext context, CommandArgumentRequest request)
	{
		TypeTargetInfo type = request.Type;

		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.CommandArgumentGeneratorFullName, type.NamespaceName, type.OuterClasses, FileUsings);
		sb.OpenClassOrRecord(request.IsRecord, type.Accessibility, type.Name);

		AppendUsage(sb, type.Name, request);
		sb.AppendLine();
		AppendConstructor(sb, type.Name, request);
		sb.AppendLine();
		AppendParseMethod(sb, type.Name);
		sb.AppendLine();
		AppendTryParseMethod(sb, type.Name, request.Options);

		if (request.NeedsEncodingHelper)
		{
			sb.AppendLine();
			ValueConversions.AppendEncodingHelper(sb);
		}

		sb.CloseClassOrRecord(request.IsRecord);
		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, type.Name, null, sb);
	}

	private static void AppendUsage(SourceBuilder sb, string typeName, CommandArgumentRequest request)
	{
		List<string> lines = [$"Usage: {request.AssemblyName} [options]"];
		List<string> columns = [.. request.Options.Select(GetUsageColumn)];
		int width = columns.Count == 0 ? 0 : columns.Max(static c => c.Length) + UsageColumnGap;

		for (int i = 0; i < request.Options.Count; i++)
		{
			string description = GetUsageDescription(request.Options[i]);
			lines.Add(description.Length == 0
				? $"{UsageIndent}{columns[i]}"
				: $"{UsageIndent}{columns[i].PadRight(width)}{description}");
		}

		sb.AppendSummary($"The usage text of the <see cref=\"{typeName}\"/> command line arguments.");
		sb.AppendLine("public const string Usage =");
		sb.Indent();
		for (int i = 0; i < lines.Count; i++)
		{
			bool isLast = i == lines.Count - 1;
			sb.AppendLine($"\"{GeneratorHelpers.EscapeString(lines[i])}{(isLast ? "\";" : "\\n\" +")}");
		}
		sb.Outdent();
	}

	private static void AppendConstructor(SourceBuilder sb, string typeName, CommandArgumentRequest request)
	{
		sb.AppendSummary($"Initializes a new instance of the <see cref=\"{typeName}\"/> class with the parsed option values.");
		foreach (CommandArgumentOption option in request.Options)
			sb.AppendLine($"/// <param name=\"{option.ParameterName.TrimStart('@')}\">The value of the <c>{option.OptionName}</c> option.</param>");

		if (request.SetsRequiredMembers)
			sb.AppendLine("[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]");

		sb.AppendLine($"private {typeName}({string.Join(", ", request.Options.Select(static o => $"{o.TypeName} {o.ParameterName}"))})");
		sb.OpenBrace();
		foreach (CommandArgumentOption option in request.Options)
		{
			string target = option.PropertyName == option.ParameterName ? $"this.{option.PropertyName}" : option.PropertyName;
			sb.AppendLine($"{target} = {option.ParameterName};");
		}
		sb.CloseBrace();
	}

	private static void AppendParseMethod(SourceBuilder sb, string typeName)
	{
		sb.AppendSummary($"Parses the specified command line <paramref name=\"args\"/> into a new <see cref=\"{typeName}\"/> instance.");
		sb.AppendLine("/// <param name=\"args\">The command line arguments.</param>");
		sb.AppendLine($"/// <returns>The parsed <see cref=\"{typeName}\"/>, or <see langword=\"null\"/> if the arguments are invalid.</returns>");
		sb.AppendLine("/// <exception cref=\"ArgumentNullException\"><paramref name=\"args\"/> is <see langword=\"null\"/>.</exception>");
		sb.AppendLine($"public static {typeName}? Parse(string[] args)");
		sb.Indent();
		sb.AppendLine("=> TryParse(args, out " + typeName + "? result, out _) ? result : null;");
		sb.Outdent();
	}

	private static void AppendTryParseMethod(SourceBuilder sb, string typeName, EquatableArray<CommandArgumentOption> options)
	{
		sb.AppendSummary($"Tries to parse the specified command line <paramref name=\"args\"/> into a new <see cref=\"{typeName}\"/> instance.");
		sb.AppendLine("/// <param name=\"args\">The command line arguments.</param>");
		sb.AppendLine($"/// <param name=\"result\">The parsed <see cref=\"{typeName}\"/>, or <see langword=\"null\"/> if parsing failed.</param>");
		sb.AppendLine("/// <param name=\"errors\">The errors of all unknown, repeated, missing and invalid options; empty on success.</param>");
		sb.AppendLine("/// <returns><see langword=\"true\"/> if the arguments were parsed; otherwise, <see langword=\"false\"/>.</returns>");
		sb.AppendLine("/// <exception cref=\"ArgumentNullException\"><paramref name=\"args\"/> is <see langword=\"null\"/>.</exception>");
		sb.AppendLine($"public static bool TryParse(string[] args, out {typeName}? result, out IReadOnlyList<string> errors)");
		sb.OpenBrace();
		sb.AppendLine("if (args is null)");
		sb.Indent();
		sb.AppendLine("throw new ArgumentNullException(nameof(args));");
		sb.Outdent();
		sb.AppendLine();
		sb.AppendLine("List<string> errorList = new List<string>();");

		foreach (CommandArgumentOption option in options)
		{
			sb.AppendLine($"{option.TypeName} {GetLocalName(option, "Value")} = {option.InitialValue};");
			sb.AppendLine($"bool {GetLocalName(option, "Found")} = false;");
		}

		sb.AppendLine();
		sb.AppendLine("for (int i = 0; i < args.Length; i++)");
		sb.OpenBrace();
		sb.AppendLine("switch (args[i])");
		sb.OpenBrace();

		foreach (CommandArgumentOption option in options)
			AppendOptionCase(sb, option);

		sb.AppendLine("default:");
		sb.Indent();
		sb.AppendLine("errorList.Add(\"Unknown option '\" + args[i] + \"'.\");");
		sb.AppendLine("break;");
		sb.Outdent();
		sb.CloseBrace();
		sb.CloseBrace();

		foreach (CommandArgumentOption option in options.Where(static o => o.IsMandatory))
		{
			sb.AppendLine();
			sb.AppendLine($"if (!{GetLocalName(option, "Found")})");
			sb.Indent();
			sb.AppendLine($"errorList.Add(\"Missing required option '{option.OptionName}'.\");");
			sb.Outdent();
		}

		sb.AppendLine();
		sb.AppendLine("if (errorList.Count > 0)");
		sb.OpenBrace();
		sb.AppendLine("result = null;");
		sb.AppendLine("errors = errorList;");
		sb.AppendLine("return false;");
		sb.CloseBrace();
		sb.AppendLine();
		sb.AppendLine($"result = new {typeName}({string.Join(", ", options.Select(static o => GetLocalName(o, "Value")))});");
		sb.AppendLine("errors = Array.Empty<string>();");
		sb.AppendLine("return true;");
		sb.CloseBrace();
	}

	private static void AppendOptionCase(SourceBuilder sb, CommandArgumentOption option)
	{
		string value = GetLocalName(option, "Value");
		string found = GetLocalName(option, "Found");

		sb.AppendLine($"case \"{option.OptionName}\":");
		sb.Indent();
		sb.AppendLine($"if ({found})");
		sb.Indent();
		sb.AppendLine($"errorList.Add(\"Option '{option.OptionName}' is specified more than once.\");");
		sb.Outdent();
		sb.AppendLine($"{found} = true;");

		if (option.IsFlag)
		{
			sb.AppendLine($"{value} = true;");
			sb.AppendLine("break;");
			sb.Outdent();
			return;
		}

		string raw = GetLocalName(option, "Raw");
		string parsed = GetLocalName(option, "Parsed");
		ValueConversion conversion = option.Conversion;

		sb.AppendLine($"if (i + 1 >= args.Length || args[i + 1].StartsWith(\"{OptionPrefix}\", StringComparison.Ordinal))");
		sb.OpenBrace();
		sb.AppendLine($"errorList.Add(\"Option '{option.OptionName}' requires a value.\");");
		sb.AppendLine("break;");
		sb.CloseBrace();
		sb.AppendLine();
		sb.AppendLine($"string {raw} = args[++i];");

		if (conversion.Kind == ValueConversionKind.String)
		{
			sb.AppendLine($"{value} = {raw};");
		}
		else
		{
			sb.AppendLine($"if (!{ValueConversions.GetTryParseExpression(conversion.Kind, conversion.UnderlyingTypeName, raw, parsed)})");
			sb.Indent();
			sb.AppendLine($"errorList.Add(\"Option '{option.OptionName}' has an invalid {conversion.DisplayName} value.\");");
			sb.Outdent();
			sb.AppendLine("else");
			sb.Indent();
			sb.AppendLine($"{value} = {parsed};");
			sb.Outdent();
		}

		sb.AppendLine("break;");
		sb.Outdent();
	}

	private static string GetUsageColumn(CommandArgumentOption option)
	{
		if (option.IsFlag)
			return option.OptionName;

		string placeholder = option.OptionName.Substring(OptionPrefix.Length);
		return option.IsMandatory ? $"{option.OptionName} <{placeholder}>" : $"{option.OptionName} [{placeholder}]";
	}

	private static string GetUsageDescription(CommandArgumentOption option)
	{
		List<string> parts = [];

		if (option.Summary is not null)
			parts.Add(option.Summary);

		if (option.DefaultText is not null)
			parts.Add($"Default: {option.DefaultText}.");
		else if (!option.IsMandatory && !option.IsFlag)
			parts.Add("Optional.");

		return string.Join(" ", parts);
	}

	private static string GetLocalName(CommandArgumentOption option, string suffix)
		=> $"{NamingHelpers.ToCamelCase(option.PropertyName)}{suffix}";

	private static List<CommandArgumentOption> CollectOptions(SemanticModel semanticModel, INamedTypeSymbol typeSymbol, TypeTargetInfo type, HashSet<string> excluded, List<DiagnosticInfo> diagnostics, CancellationToken cancellationToken)
	{
		List<CommandArgumentOption> options = [];
		Dictionary<string, string> names = new(StringComparer.Ordinal);

		foreach (IPropertySymbol property in GeneratorHelpers.GetPublicPropertySymbols(typeSymbol))
		{
			if (excluded.Contains(property.Name) || GeneratorHelpers.IsComputedProperty(property, cancellationToken))
				continue;

			LocationInfo? location = GetLocation(property, type);
			string camelName = NamingHelpers.ToCamelCase(property.Name);
			string optionName = $"{OptionPrefix}{camelName}";
			bool isValid = true;

			if (names.TryGetValue(optionName, out string? other))
			{
				diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.CommandArgumentDuplicateOptionNameDiagnostic, location, other, property.Name, optionName));
				isValid = false;
			}
			else
			{
				names[optionName] = property.Name;
			}

			if (!ValueConversions.TryGetKind(property.Type, out ValueConversionKind kind, allowEncoding: true))
			{
				diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.CommandArgumentUnsupportedTypeDiagnostic, location, property.Name, property.Type.ToDisplayString()));
				continue;
			}

			ValueConversion conversion = ValueConversions.Create(property.Type, kind);
			bool isNullable = property.NullableAnnotation == NullableAnnotation.Annotated || conversion.IsNullableValueType;
			bool isFlag = kind == ValueConversionKind.Boolean && !isNullable;
			string initialValue = isNullable ? "null" : isFlag ? "false" : property.Type.IsValueType ? "default" : "default!";
			string? defaultText = null;
			bool isOptional = isNullable || isFlag;

			if (GeneratorHelpers.GetPropertyInitializer(property, cancellationToken) is EqualsValueClauseSyntax initializer)
			{
				SemanticModel model = initializer.SyntaxTree == semanticModel.SyntaxTree
					? semanticModel
					: semanticModel.Compilation.GetSemanticModel(initializer.SyntaxTree);

				if (model.GetConstantValue(initializer.Value, cancellationToken) is not { HasValue: true } constant)
				{
					diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.CommandArgumentNonConstantInitializerDiagnostic, location, property.Name));
					continue;
				}

				initialValue = GeneratorHelpers.FormatLiteral(constant.Value, property.Type);
				defaultText = isFlag && constant.Value is false ? null : FormatDefaultText(constant.Value, property.Type);
				isOptional = true;
			}

			if (!isValid)
				continue;

			options.Add(new CommandArgumentOption(
				property.Name,
				SyntaxFacts.GetKeywordKind(camelName) == SyntaxKind.None ? camelName : $"@{camelName}",
				optionName,
				property.Type.ToPropertyTypeDisplayString(),
				conversion,
				isFlag,
				IsMandatory: !isOptional,
				initialValue,
				defaultText,
				GeneratorHelpers.GetXmlSummaryComment(property)));
		}

		return options;
	}

	private static string? FormatDefaultText(object? value, ITypeSymbol type)
	{
		ITypeSymbol underlying = ValueConversions.GetUnderlyingType(type);

		if (underlying.TypeKind == TypeKind.Enum && value is not null)
		{
			IFieldSymbol? member = underlying.GetMembers()
				.OfType<IFieldSymbol>()
				.FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, value));

			return member?.Name ?? GeneratorHelpers.FormatInvariant(value);
		}

		return value switch
		{
			null => null,
			string text => text.Length == 0 ? null : text,
			bool flag => flag ? "true" : "false",
			_ => GeneratorHelpers.FormatInvariant(value),
		};
	}

	private static bool HasRequiredMembers(INamedTypeSymbol typeSymbol)
	{
		for (INamedTypeSymbol? current = typeSymbol; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
		{
			if (current.GetMembers().Any(static m => m is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true }))
				return true;
		}

		return false;
	}

	private static LocationInfo? GetLocation(ISymbol symbol, TypeTargetInfo type)
		=> symbol.Locations.FirstOrDefault(static l => l.IsInSource) is Location location
			? LocationInfo.From(location)
			: type.IdentifierLocation;
}
