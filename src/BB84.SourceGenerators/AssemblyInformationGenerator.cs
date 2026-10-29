// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Models;
using BB84.SourceGenerators.Requests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators;

/// <summary>
/// Responsible for generating compile-time constant properties for assembly information
/// on classes marked with the <see cref="GenerateAssemblyInformationAttribute"/> attribute.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AssemblyInformationGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateAssemblyInformationAttribute>();

	private static readonly string[] FileUsings =
	[
		NamespaceNames.System,
		NamespaceNames.SystemCollectionsGeneric,
	];

	private static readonly (string AttributeName, string PropertyName)[] AssemblyAttributes =
	[
		("System.Reflection.AssemblyTitleAttribute", "Title"),
		("System.Reflection.AssemblyDescriptionAttribute", "Description"),
		("System.Reflection.AssemblyCompanyAttribute", "Company"),
		("System.Reflection.AssemblyProductAttribute", "Product"),
		("System.Reflection.AssemblyCopyrightAttribute", "Copyright"),
		("System.Reflection.AssemblyTrademarkAttribute", "Trademark"),
		("System.Reflection.AssemblyConfigurationAttribute", "Configuration"),
		("System.Reflection.AssemblyVersionAttribute", "Version"),
		("System.Reflection.AssemblyFileVersionAttribute", "FileVersion"),
		("System.Reflection.AssemblyInformationalVersionAttribute", "InformationalVersion"),
		("System.Runtime.Versioning.TargetFrameworkAttribute", "TargetFramework"),
		("System.Resources.NeutralResourcesLanguageAttribute", "NeutralResourcesLanguage")
	];

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<AssemblyInformationRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassNode, Transform, TrackingNames.AssemblyInformation, Execute);

	private static GeneratorResult<AssemblyInformationRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not ClassDeclarationSyntax classDeclaration || context.TargetSymbol is not INamedTypeSymbol classSymbol)
			return null;

		(Dictionary<string, string> assemblyValues, Dictionary<string, List<string>> allAssemblyValues) = GetAssemblyAttributeValues(context.SemanticModel.Compilation);

		EquatableArray<string> constantValues = AssemblyAttributes
			.Select(attribute => assemblyValues.TryGetValue(attribute.AttributeName, out string? value) ? value : string.Empty)
			.ToEquatableArray();

		EquatableArray<(string AttributeName, EquatableArray<string> Values)> otherAttributes = allAssemblyValues
			.Select(static entry => (entry.Key, entry.Value.ToEquatableArray()))
			.ToEquatableArray();

		return GeneratorResult<AssemblyInformationRequest>.FromRequest(new AssemblyInformationRequest(TypeTargetInfo.Create(classDeclaration, classSymbol), constantValues, otherAttributes));
	}

	private static void Execute(SourceProductionContext context, AssemblyInformationRequest request)
	{
		TypeTargetInfo type = request.Type;
		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.AssemblyInformationGeneratorFullName, type.NamespaceName, type.OuterClasses, FileUsings);

		AppendPartialClass(sb, type.Name, type.Accessibility, request.ConstantValues, request.OtherAttributes);

		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, type.Name, "AssemblyInformation", sb);
	}

	private static void AppendPartialClass(SourceBuilder sb, string className, string accessibility, EquatableArray<string> constantValues, EquatableArray<(string AttributeName, EquatableArray<string> Values)> otherAttributes)
	{
		sb.OpenClass(accessibility, className);

		for (int i = 0; i < AssemblyAttributes.Length; i++)
		{
			if (i > 0)
				sb.AppendLine();

			string propertyName = AssemblyAttributes[i].PropertyName;
			string escaped = GeneratorHelpers.EscapeString(constantValues[i]);

			sb.AppendSummary($"Gets the assembly {propertyName.ToLowerInvariant()} information.");
			sb.AppendLine($"public const string {propertyName} = \"{escaped}\";");
		}

		sb.AppendLine();
		sb.AppendSummary("Gets all assembly attributes available at compile time.");
		sb.AppendLine("public static readonly Dictionary<string, List<string>> Attributes = new(StringComparer.Ordinal)");
		sb.AppendLine("{");
		sb.Indent();

		foreach ((string attributeName, EquatableArray<string> attributeValues) in otherAttributes)
		{
			string escapedKey = GeneratorHelpers.EscapeString(attributeName);
			string values = string.Join(", ", attributeValues.Select(v => $"\"{GeneratorHelpers.EscapeString(v)}\""));
			sb.AppendLine($"[\"{escapedKey}\"] = new List<string> {{ {values} }},");
		}

		sb.Outdent();
		sb.AppendLine("};");

		sb.CloseClass();
	}

	private static (Dictionary<string, string> AssemblyValues, Dictionary<string, List<string>> AllAssemblyValues) GetAssemblyAttributeValues(Compilation compilation)
	{
		Dictionary<string, string> values = new(StringComparer.Ordinal);
		Dictionary<string, List<string>> allValues = new(StringComparer.Ordinal);

		foreach (AttributeData attribute in compilation.Assembly.GetAttributes())
		{
			string? fullName = attribute.AttributeClass?.ToDisplayString();
			if (fullName is null)
				continue;

			string value = GetAttributeValue(attribute);
			values[fullName] = value;

			if (AssemblyAttributes.Any(x => x.AttributeName == fullName))
				continue;

			if (!allValues.TryGetValue(fullName, out List<string>? attributeValues))
			{
				attributeValues = [];
				allValues[fullName] = attributeValues;
			}

			attributeValues.Add(value);
		}

		return (values, allValues);
	}

	private static string GetAttributeValue(AttributeData attribute)
	{
		List<string> parts = [];

		if (attribute.ConstructorArguments.Length > 0)
			parts.Add(string.Join(", ", attribute.ConstructorArguments.Select(FormatTypedConstant)));

		if (attribute.NamedArguments.Length > 0)
			parts.Add(string.Join(", ", attribute.NamedArguments.Select(arg => $"{arg.Key}={FormatTypedConstant(arg.Value)}")));

		return parts.Count > 0
			? string.Join("; ", parts)
			: string.Empty;
	}

	private static string FormatTypedConstant(TypedConstant constant)
	{
		return constant.IsNull
			? string.Empty
			: constant.Kind == TypedConstantKind.Array
			? string.Join(", ", constant.Values.Select(FormatTypedConstant))
			: GeneratorHelpers.FormatInvariant(constant.Value) ?? string.Empty;
	}
}
