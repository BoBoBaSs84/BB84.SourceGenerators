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
using BB84.SourceGenerators.Models;
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
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateAutoMapperAttribute>();
	private static readonly string PropertyMappingAttributeName = typeof(PropertyMappingAttribute).FullName;

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<AutoMapperRequest>(context, AttributeNames.MetadataName, static (node, _) => node is MethodDeclarationSyntax, TransformMethod, TrackingNames.AutoMapper, EmitMapper);

	private static GeneratorResult<AutoMapperRequest>? TransformMethod(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not MethodDeclarationSyntax methodSyntax)
			return null;

		SemanticModel semanticModel = context.SemanticModel;

		if (semanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken) is not IMethodSymbol methodSymbol)
			return null;

		string methodName = methodSymbol.Name;
		string containingTypeName = methodSymbol.ContainingType?.Name ?? string.Empty;

		// Must be a partial method
		if (!methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
			return GeneratorResult<AutoMapperRequest>.FromDiagnostics(DiagnosticInfo.Create(DiagnosticDescriptors.AutoMapperRequiresPartialMethodDiagnostic, null, methodName));

		// Must have exactly one parameter (the source)
		if (methodSymbol.Parameters.Length != 1)
			return null;

		// Must have a non-void return type (the target)
		if (methodSymbol.ReturnsVoid)
			return null;

		// Containing type must be partial
		if (methodSymbol.ContainingType is not INamedTypeSymbol containingType)
			return null;

		if (methodSyntax.Parent is not ClassDeclarationSyntax containingClassSyntax)
			return null;

		if (!containingClassSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
			return GeneratorResult<AutoMapperRequest>.FromDiagnostics(DiagnosticInfo.Create(DiagnosticDescriptors.AutoMapperRequiresPartialClassDiagnostic, null, methodName, containingType.Name));

		return BuildResult(methodSymbol, containingType, containingClassSyntax);
	}

	private static GeneratorResult<AutoMapperRequest> BuildResult(IMethodSymbol methodSymbol, INamedTypeSymbol containingType, ClassDeclarationSyntax containingClassSyntax)
	{
		IParameterSymbol sourceParameter = methodSymbol.Parameters[0];
		ITypeSymbol sourceType = sourceParameter.Type;
		ITypeSymbol targetType = methodSymbol.ReturnType;

		// Gather property mappings from [PropertyMapping] attributes
		Dictionary<string, string> customMappings = GetCustomPropertyMappings(methodSymbol);

		// Resolve property mappings
		(ImmutableArray<AutoMapperPropertyMapping> mappings, ImmutableArray<string> unmappedProperties, ImmutableArray<(string, string)> typeMismatchProperties) = ResolvePropertyMappings(sourceType, targetType, customMappings);

		string methodAccessibility = GeneratorHelpers.GetAccessibilityKeyword(methodSymbol.DeclaredAccessibility);

		bool sourceIsNullable = sourceParameter.NullableAnnotation == NullableAnnotation.Annotated;

		bool containingTypeIsStatic = containingClassSyntax.Modifiers.Any(SyntaxKind.StaticKeyword);

		// Diagnostics for unmapped properties, then for type mismatches
		string targetTypeName = targetType.Name;
		EquatableArray<DiagnosticInfo> diagnostics = unmappedProperties
			.Select(unmapped => DiagnosticInfo.Create(DiagnosticDescriptors.AutoMapperUnmappedPropertyDiagnostic, null, unmapped, targetTypeName))
			.Concat(typeMismatchProperties.Select(mismatch => DiagnosticInfo.Create(DiagnosticDescriptors.AutoMapperTypeMismatchDiagnostic, null, mismatch.Item1, targetTypeName, mismatch.Item2)))
			.ToEquatableArray();

		AutoMapperRequest request = new(
			containingClassSyntax.GetNamespace(),
			GeneratorHelpers.GetAccessibility(containingClassSyntax),
			containingType.Name,
			containingTypeIsStatic,
			GetOuterClassesWithStatic(containingClassSyntax).ToEquatableArray(),
			methodAccessibility,
			methodSymbol.IsStatic,
			methodSymbol.Name,
			sourceParameter.Name,
			sourceType.ToFullyQualifiedDisplayString(),
			targetType.ToFullyQualifiedDisplayString(),
			targetType.ToMinimalDisplayString(),
			sourceIsNullable,
			mappings.ToEquatableArray()
			);

		return new GeneratorResult<AutoMapperRequest>(request, diagnostics);
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
		ITypeSymbol sourceType,
		ITypeSymbol targetType,
		Dictionary<string, string> customMappings)
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

	private static void EmitMapper(SourceProductionContext context, AutoMapperRequest request)
	{
		SourceBuilder sb = new();

		sb.AppendAutoGeneratedWarning(GeneratorNames.AutoMapperGeneratorFullName);
		sb.AppendNullableEnable();
		sb.AppendLine();
		sb.OpenNamespace(request.Namespace);

		sb.OpenOuterClasses(request.OuterClasses);

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

		sb.CloseOuterClasses(request.OuterClasses);
		sb.CloseNamespace();

		string hintName = GeneratorHelpers.BuildHintName(request.OuterClasses, $"{request.ContainingTypeName}.{request.MethodName}", "AutoMapper");

		context.AddSource(hintName, sb.ToString());
	}
}
