// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;

using BB84.SourceGenerators.Analyzers;
using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Extensions;
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Requests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators;

/// <summary>
/// Responsible for generating property-to-property mapping implementations for partial methods
/// decorated with the <see cref="GenerateAutoMapperAttribute"/> attribute.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AutoMapperGenerator : IIncrementalGenerator
{
	private static readonly string GeneratorName = typeof(AutoMapperGenerator).FullName;
	private static readonly string GeneratorAttributeName = typeof(GenerateAutoMapperAttribute).FullName;
	private static readonly string PropertyMappingAttributeName = typeof(PropertyMappingAttribute).FullName;

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValuesProvider<AutoMapperRequest?> provider =
			context.SyntaxProvider
				.ForAttributeWithMetadataName(
					fullyQualifiedMetadataName: GeneratorAttributeName,
					predicate: static (node, _) => node is MethodDeclarationSyntax,
					transform: static (ctx, _) => TransformMethod(ctx))
				.Where(static result => result is not null);

		context.RegisterSourceOutput(provider, Execute);
	}

	private static AutoMapperRequest? TransformMethod(GeneratorAttributeSyntaxContext context)
	{
		if (context.TargetNode is not MethodDeclarationSyntax methodSyntax)
			return null;

		SemanticModel semanticModel = context.SemanticModel;

		if (semanticModel.GetDeclaredSymbol(methodSyntax) is not IMethodSymbol methodSymbol)
			return null;

		// Must be a partial method
		if (!methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
			return null;

		// Must have exactly one parameter (the source)
		if (methodSymbol.Parameters.Length != 1)
			return null;

		// Must have a non-void return type (the target)
		if (methodSymbol.ReturnsVoid)
			return null;

		IParameterSymbol sourceParameter = methodSymbol.Parameters[0];
		ITypeSymbol sourceType = sourceParameter.Type;
		ITypeSymbol targetType = methodSymbol.ReturnType;

		// Containing type must be partial
		if (methodSymbol.ContainingType is not INamedTypeSymbol containingType)
			return null;

		if (methodSyntax.Parent is not ClassDeclarationSyntax containingClassSyntax)
			return null;

		if (!containingClassSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
			return null;

		// Gather property mappings from [PropertyMapping] attributes
		Dictionary<string, string> customMappings = GetCustomPropertyMappings(methodSymbol);

		// Resolve property mappings
		(ImmutableArray<AutoMapperPropertyMapping> mappings, ImmutableArray<string> unmappedProperties, ImmutableArray<(string, string)> typeMismatchProperties) = ResolvePropertyMappings(sourceType, targetType, customMappings);

		string methodAccessibility = GeneratorHelpers.GetAccessibilityKeyword(methodSymbol.DeclaredAccessibility);

		bool sourceIsNullable = sourceParameter.NullableAnnotation == NullableAnnotation.Annotated;

		Microsoft.CodeAnalysis.Text.TextSpan methodIdentifierSpan = methodSyntax.Identifier.Span;

		bool containingTypeIsStatic = containingClassSyntax.Modifiers.Any(SyntaxKind.StaticKeyword);

		var autoMapperRequest = new AutoMapperRequest(
			containingClassSyntax.GetNamespace(),
			GeneratorHelpers.GetAccessibility(containingClassSyntax),
			containingType.Name,
			containingTypeIsStatic,
			GetOuterClassesWithStatic(containingClassSyntax),
			methodAccessibility,
			methodSymbol.IsStatic,
			methodSymbol.Name,
			sourceParameter.Name,
			sourceType.ToFullyQualifiedDisplayString(),
			targetType.ToFullyQualifiedDisplayString(),
			targetType.ToMinimalDisplayString(),
			sourceIsNullable,
			mappings,
			unmappedProperties,
			typeMismatchProperties,
			targetType.Name,
			methodSyntax.SyntaxTree.FilePath,
			methodIdentifierSpan.Start,
			methodIdentifierSpan.End
			);

		return autoMapperRequest;
	}

	private static List<(string Accessibility, string Name, bool IsStatic)> GetOuterClassesWithStatic(ClassDeclarationSyntax classDeclaration)
	{
		List<(string Accessibility, string Name, bool IsStatic)> outerClasses = [];
		SyntaxNode? parent = classDeclaration.Parent;

		while (parent is ClassDeclarationSyntax outerClass)
		{
			outerClasses.Add((GeneratorHelpers.GetAccessibility(outerClass), outerClass.Identifier.Text, outerClass.Modifiers.Any(SyntaxKind.StaticKeyword)));
			parent = outerClass.Parent;
		}

		outerClasses.Reverse();
		return outerClasses;
	}

	private static Dictionary<string, string> GetCustomPropertyMappings(IMethodSymbol methodSymbol)
	{
		Dictionary<string, string> mappings = new(StringComparer.Ordinal);

		foreach (AttributeData attribute in methodSymbol.GetAttributes())
		{
			if (attribute.AttributeClass?.ToDisplayString() != PropertyMappingAttributeName)
				continue;

			ImmutableArray<TypedConstant> args = attribute.ConstructorArguments;
			if (args.Length == 2 && args[0].Value is string source && args[1].Value is string target)
			{
				mappings[target] = source;
			}
		}

		return mappings;
	}

	private static (ImmutableArray<AutoMapperPropertyMapping> Mappings, ImmutableArray<string> UnmappedProperties, ImmutableArray<(string TargetProperty, string SourceProperty)> TypeMismatchProperties) ResolvePropertyMappings(
		ITypeSymbol sourceType, ITypeSymbol targetType, Dictionary<string, string> customMappings)
	{
		ImmutableArray<AutoMapperPropertyMapping>.Builder builder = ImmutableArray.CreateBuilder<AutoMapperPropertyMapping>();
		ImmutableArray<string>.Builder unmappedBuilder = ImmutableArray.CreateBuilder<string>();
		ImmutableArray<(string, string)>.Builder typeMismatchBuilder = ImmutableArray.CreateBuilder<(string, string)>();

		IEnumerable<IPropertySymbol> targetProperties = GetSettableProperties(targetType);
		Dictionary<string, IPropertySymbol> sourceProperties = GetReadableProperties(sourceType)
			.ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

		foreach (IPropertySymbol targetProp in targetProperties)
		{
			string sourcePropName = customMappings.TryGetValue(targetProp.Name, out string? mapped) ? mapped : targetProp.Name;
			if (!sourceProperties.TryGetValue(sourcePropName, out IPropertySymbol? sourceProp))
			{
				unmappedBuilder.Add(targetProp.Name);
				continue;
			}

			// Check type compatibility (assignable)
			if (!IsAssignableFrom(targetProp.Type, sourceProp.Type))
			{
				typeMismatchBuilder.Add((targetProp.Name, sourcePropName));
				continue;
			}

			bool requiresNullForgiving =
				targetProp.NullableAnnotation == NullableAnnotation.NotAnnotated &&
				sourceProp.NullableAnnotation == NullableAnnotation.Annotated;

			builder.Add(new AutoMapperPropertyMapping(targetProp.Name, sourcePropName, requiresNullForgiving));
		}

		return (builder.ToImmutable(), unmappedBuilder.ToImmutable(), typeMismatchBuilder.ToImmutable());
	}

	private static bool IsAssignableFrom(ITypeSymbol target, ITypeSymbol source)
	{
		if (SymbolEqualityComparer.Default.Equals(target, source))
			return true;

		// Allow nullable target from non-nullable source
		if (target.NullableAnnotation == NullableAnnotation.Annotated &&
			SymbolEqualityComparer.Default.Equals(target.WithNullableAnnotation(NullableAnnotation.NotAnnotated), source))
			return true;

		// Allow implicit reference conversions (inheritance)
		if (target is INamedTypeSymbol namedTarget && source is INamedTypeSymbol namedSource)
		{
			ITypeSymbol? current = namedSource;
			while (current is not null)
			{
				if (SymbolEqualityComparer.Default.Equals(current, namedTarget))
					return true;
				current = current.BaseType;
			}

			// Check interfaces
			foreach (INamedTypeSymbol iface in namedSource.AllInterfaces)
			{
				if (SymbolEqualityComparer.Default.Equals(iface, namedTarget))
					return true;
			}
		}

		return false;
	}

	private static IEnumerable<IPropertySymbol> GetSettableProperties(ITypeSymbol type)
		=> GeneratorHelpers.GetPublicPropertySymbols(type, includeInherited: true, requireGetter: false, requireSetter: true);

	private static IEnumerable<IPropertySymbol> GetReadableProperties(ITypeSymbol type)
		=> GeneratorHelpers.GetPublicPropertySymbols(type, includeInherited: true, requireGetter: true, requireSetter: false);

	private void Execute(SourceProductionContext context, AutoMapperRequest? request)
	{
		if (request is null)
			return;

		// Report diagnostics for unmapped properties
		foreach (string unmappedProperty in request.UnmappedProperties)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.AutoMapperUnmappedPropertyDiagnostic,
				Location.None,
				unmappedProperty,
				request.TargetTypeName);
			context.ReportDiagnostic(diagnostic);
		}

		// Report diagnostics for type mismatches
		foreach ((string targetProperty, string sourceProperty) in request.TypeMismatchProperties)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.AutoMapperTypeMismatchDiagnostic,
				Location.None,
				targetProperty,
				request.TargetTypeName,
				sourceProperty);
			context.ReportDiagnostic(diagnostic);
		}

		SourceBuilder sb = new();

		sb.AppendAutoGeneratedWarning(GeneratorName);
		sb.AppendNullableEnable();
		sb.AppendLine();
		sb.OpenNamespace(request.Namespace);

		foreach ((string outerAccessibility, string outerName, bool outerIsStatic) in request.OuterClasses)
		{
			string outerStaticKeyword = outerIsStatic ? "static " : string.Empty;
			sb.AppendLine($"{outerAccessibility} {outerStaticKeyword}partial class {outerName}");
			sb.OpenBrace();
		}

		string staticKeyword = request.ContainingTypeIsStatic ? "static " : string.Empty;
		sb.AppendLine($"{request.ContainingTypeAccessibility} {staticKeyword}partial class {request.ContainingTypeName}");
		sb.OpenBrace();

		string methodStaticKeyword = request.MethodIsStatic ? "static " : string.Empty;
		sb.AppendLine($"{request.MethodAccessibility} {methodStaticKeyword}partial {request.TargetTypeDisplayName} {request.MethodName}({request.SourceTypeFullName} {request.ParameterName})");
		sb.OpenBrace();

		if (request.SourceIsNullable)
		{
			sb.AppendLine($"if ({request.ParameterName} is null)");
			sb.Indent();
			sb.AppendLine($"throw new System.ArgumentNullException(nameof({request.ParameterName}));");
			sb.Outdent();
			sb.AppendLine();
		}

		sb.AppendLine($"return new {request.TargetTypeFullName}");
		sb.OpenBrace();

		foreach (AutoMapperPropertyMapping mapping in request.PropertyMappings)
		{
			string nullForgiving = mapping.RequiresNullForgiving ? "!" : string.Empty;
			sb.AppendLine($"{mapping.TargetProperty} = {request.ParameterName}.{mapping.SourceProperty}{nullForgiving},");
		}

		sb.Outdent();
		sb.AppendLine("};");
		sb.CloseBrace();
		sb.CloseBrace();

		for (int i = request.OuterClasses.Count - 1; i >= 0; i--)
			sb.CloseBrace();

		sb.CloseNamespace();

		string hintName = request.OuterClasses.Count > 0
			? $"{string.Join(".", request.OuterClasses.Select(o => o.Name))}.{request.ContainingTypeName}.{request.MethodName}.AutoMapper.g.cs"
			: $"{request.ContainingTypeName}.{request.MethodName}.AutoMapper.g.cs";

		context.AddSource(hintName, sb.ToString());
	}
}
