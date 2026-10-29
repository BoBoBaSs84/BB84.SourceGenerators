// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Analyzers;
using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Models;
using BB84.SourceGenerators.Requests;

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
		=> GeneratorHelpers.RegisterRequestGenerator<SingletonRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassNode, Transform, TrackingNames.Singleton, Execute);

	private static GeneratorResult<SingletonRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not ClassDeclarationSyntax classDeclaration || context.TargetSymbol is not INamedTypeSymbol classSymbol)
			return null;

		TypeTargetInfo type = TypeTargetInfo.Create(classDeclaration, classSymbol);

		// Check for required members that prevent parameterless initialization
		ISymbol? requiredMember = classSymbol.GetMembers().FirstOrDefault(IsRequiredMember);
		if (requiredMember is not null)
			return GeneratorResult<SingletonRequest>.FromDiagnostics(type.CreateDiagnostic(DiagnosticDescriptors.SingletonRequiresParameterlessInitializationDiagnostic, type.Name, requiredMember.Name));

		bool useLazy = GetUseLazy(classSymbol);
		string instanceTypeName = GetFirstInterfaceName(classSymbol) ?? type.Name;

		return GeneratorResult<SingletonRequest>.FromRequest(new SingletonRequest(type, useLazy, instanceTypeName));
	}

	private static void Execute(SourceProductionContext context, SingletonRequest request)
	{
		TypeTargetInfo type = request.Type;
		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.SingletonGeneratorFullName, type.NamespaceName, type.OuterClasses, []);

		AppendPartialClass(sb, type.Name, type.Accessibility, request.UseLazy, request.InstanceTypeName);

		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, type.Name, "Singleton", sb);
	}

	private static void AppendPartialClass(SourceBuilder sb, string className, string accessibility, bool useLazy, string instanceTypeName)
	{
		sb.OpenClass(accessibility, className);

		if (useLazy)
		{
			sb.AppendLine($"private static readonly Lazy<{className}> _lazyInstance = new Lazy<{className}>(() => new {className}());");
			sb.AppendLine();
			AppendInstanceSummary(sb, className, instanceTypeName);
			sb.AppendLine($"public static {instanceTypeName} Instance => _lazyInstance.Value;");
		}
		else
		{
			AppendInstanceSummary(sb, className, instanceTypeName);
			sb.AppendLine($"public static {instanceTypeName} Instance {{ get; }} = new {className}();");
		}

		sb.AppendLine();
		sb.AppendLine($"private {className}() {{ }}");
		sb.CloseClass();
	}

	private static void AppendInstanceSummary(SourceBuilder sb, string className, string instanceTypeName)
		=> sb.AppendSummary(instanceTypeName != className
			? $"Gets the singleton instance of <see cref=\"{className}\"/> as <see cref=\"{instanceTypeName}\"/>."
			: $"Gets the singleton instance of <see cref=\"{className}\"/>.");

	private static bool IsRequiredMember(ISymbol member)
		=> member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true };

	private static bool GetUseLazy(INamedTypeSymbol namedTypeSymbol)
		=> GeneratorHelpers.GetConstructorArgumentValue(namedTypeSymbol, AttributeNames.TypeName, 0, true);
	
	private static string? GetFirstInterfaceName(INamedTypeSymbol classSymbol)
		=> classSymbol.Interfaces.FirstOrDefault()?.Name;
}
