// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Analyzers;
using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators;

/// <summary>
/// Responsible for generating the singleton pattern for classes marked
/// with the <see cref="GenerateSingletonAttribute"/> attribute.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SingletonGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateSingletonAttribute>();

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterClassGenerator(context, AttributeNames.MetadataName, Execute);

	private void Execute(SourceProductionContext context, (ClassDeclarationSyntax ClassSyntax, SemanticModel SemanticModel)? input)
	{
		if (!GeneratorHelpers.TryCreateContext(input, out GeneratorContext ctx))
			return;

		// Check for required members that prevent parameterless initialization
		foreach (ISymbol member in ctx.ClassSymbol.GetMembers())
		{
			if (member is IPropertySymbol propertySymbol && propertySymbol.IsRequired)
			{
				GeneratorHelpers.ReportAtClass(context, ctx.ClassDeclaration, DiagnosticDescriptors.SingletonRequiresParameterlessInitializationDiagnostic, ctx.ClassName, propertySymbol.Name);
				return;
			}

			if (member is IFieldSymbol fieldSymbol && fieldSymbol.IsRequired)
			{
				GeneratorHelpers.ReportAtClass(context, ctx.ClassDeclaration, DiagnosticDescriptors.SingletonRequiresParameterlessInitializationDiagnostic, ctx.ClassName, fieldSymbol.Name);
				return;
			}
		}

		bool useLazy = GetUseLazy(ctx.ClassSymbol);
		string instanceTypeName = GetFirstInterfaceName(ctx.ClassSymbol) ?? ctx.ClassName;

		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.SingletonGeneratorFullName, ctx.NamespaceName, ctx.OuterClasses, []);

		AppendPartialClass(sb, ctx.ClassName, ctx.Accessibility, useLazy, instanceTypeName);

		sb.EndTypeFile(ctx.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, ctx.OuterClasses, ctx.ClassName, "Singleton", sb);
	}

	private static void AppendPartialClass(SourceBuilder sb, string className, string accessibility, bool useLazy, string instanceTypeName)
	{
		sb.OpenClass(accessibility, className);

		if (useLazy)
		{
			sb.AppendLine($"private static readonly Lazy<{className}> _lazyInstance = new Lazy<{className}>(() => new {className}());");
			sb.AppendLine();
			sb.AppendLine("/// <summary>");
			if (instanceTypeName != className)
				sb.AppendLine($"/// Gets the singleton instance of <see cref=\"{className}\"/> as <see cref=\"{instanceTypeName}\"/>.");
			else
				sb.AppendLine($"/// Gets the singleton instance of <see cref=\"{className}\"/>.");
			sb.AppendLine("/// </summary>");
			sb.AppendLine($"public static {instanceTypeName} Instance => _lazyInstance.Value;");
		}
		else
		{
			sb.AppendLine("/// <summary>");
			if (instanceTypeName != className)
				sb.AppendLine($"/// Gets the singleton instance of <see cref=\"{className}\"/> as <see cref=\"{instanceTypeName}\"/>.");
			else
				sb.AppendLine($"/// Gets the singleton instance of <see cref=\"{className}\"/>.");
			sb.AppendLine("/// </summary>");
			sb.AppendLine($"public static {instanceTypeName} Instance {{ get; }} = new {className}();");
		}

		sb.AppendLine();
		sb.AppendLine($"private {className}() {{ }}");
		sb.CloseClass();
	}

	private static bool GetUseLazy(INamedTypeSymbol namedTypeSymbol)
		=> GeneratorHelpers.GetConstructorArgumentValue(namedTypeSymbol, AttributeNames.TypeName, 0, true);
	
	private static string? GetFirstInterfaceName(INamedTypeSymbol classSymbol)
		=> classSymbol.Interfaces.FirstOrDefault()?.Name;
}
