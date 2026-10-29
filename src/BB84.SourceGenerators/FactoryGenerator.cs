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
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators;

/// <summary>
/// Responsible for generating the factory pattern for classes marked
/// with the <see cref="GenerateFactoryAttribute"/> attribute. The generated code
/// discovers all concrete implementations of a target interface at compile time
/// and emits a <c>Create(string key)</c> method that maps keys to concrete types.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class FactoryGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateFactoryAttribute>();
	private static readonly string FactoryKeyAttributeName = typeof(GenerateFactoryKeyAttribute).FullName;

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<FactoryRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassNode, Transform, TrackingNames.Factory, Execute);

	private static GeneratorResult<FactoryRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not ClassDeclarationSyntax classDeclaration || context.TargetSymbol is not INamedTypeSymbol classSymbol)
			return null;

		TypeTargetInfo type = TypeTargetInfo.Create(classDeclaration, classSymbol);

		// Get the interface type from the attribute
		INamedTypeSymbol? interfaceSymbol = GetInterfaceTypeFromAttribute(classSymbol);
		if (interfaceSymbol is null)
			return null;

		if (interfaceSymbol.TypeKind != TypeKind.Interface)
			return GeneratorResult<FactoryRequest>.FromDiagnostics(type.CreateDiagnostic(DiagnosticDescriptors.FactoryRequiresInterfaceTypeDiagnostic, type.Name, interfaceSymbol.Name));

		List<DiagnosticInfo> diagnostics = [];
		List<(string TypeName, string Key)> implementations = DiscoverImplementations(diagnostics, type, context.SemanticModel.Compilation, interfaceSymbol);

		if (diagnostics.Count > 0)
			return GeneratorResult<FactoryRequest>.FromDiagnostics([.. diagnostics]);

		FactoryRequest request = new(
			type,
			interfaceSymbol.ToFullyQualifiedDisplayString(),
			interfaceSymbol.ToMinimalDisplayString(),
			implementations.ToEquatableArray());

		return GeneratorResult<FactoryRequest>.FromRequest(request);
	}

	private static void Execute(SourceProductionContext context, FactoryRequest request)
	{
		TypeTargetInfo type = request.Type;
		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.FactoryGeneratorFullName, type.NamespaceName, type.OuterClasses, []);

		AppendPartialClass(sb, type.Name, type.Accessibility, request.InterfaceFullName, request.InterfaceShortName, request.Implementations);

		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, type.Name, "Factory", sb);
	}

	/// <summary>
	/// Discovers all concrete, publicly constructible implementations of the interface in the compilation,
	/// adding a diagnostic for each duplicate factory key.
	/// </summary>
	/// <param name="diagnostics">The list the duplicate key diagnostics are added to.</param>
	/// <param name="type">The factory class.</param>
	/// <param name="compilation">The compilation to search.</param>
	/// <param name="interfaceSymbol">The target interface.</param>
	/// <returns>The fully qualified implementation type names and their keys.</returns>
	private static List<(string TypeName, string Key)> DiscoverImplementations(List<DiagnosticInfo> diagnostics, TypeTargetInfo type, Compilation compilation, INamedTypeSymbol interfaceSymbol)
	{
		List<(string TypeName, string Key)> implementations = [];
		HashSet<string> seenKeys = new(StringComparer.Ordinal);

		foreach (INamedTypeSymbol candidate in GetAllNamedTypes(compilation.GlobalNamespace))
		{
			if (candidate.TypeKind != TypeKind.Class || candidate.IsAbstract || candidate.IsStatic)
				continue;

			if (!ImplementsInterface(candidate, interfaceSymbol))
				continue;

			if (!HasParameterlessConstructor(candidate))
				continue;

			string key = GetFactoryKey(candidate);

			if (!seenKeys.Add(key))
			{
				diagnostics.Add(type.CreateDiagnostic(DiagnosticDescriptors.FactoryDuplicateKeyDiagnostic, key, interfaceSymbol.Name));
				continue;
			}

			implementations.Add((candidate.ToFullyQualifiedDisplayString(), key));
		}

		return implementations;
	}

	private static void AppendPartialClass(
		SourceBuilder sb,
		string className,
		string accessibility,
		string interfaceFullName,
		string interfaceShortName,
		EquatableArray<(string TypeName, string Key)> implementations)
	{
		sb.OpenClass(accessibility, className);

		AppendCreateMethod(sb, interfaceFullName, interfaceShortName, implementations);
		sb.AppendLine();
		AppendGetKeysMethod(sb, implementations);

		sb.CloseClass();
	}

	private static void AppendCreateMethod(SourceBuilder sb, string interfaceFullName, string interfaceShortName, EquatableArray<(string TypeName, string Key)> implementations)
	{
		sb.AppendSummary($"Creates an instance of <see cref=\"{interfaceShortName}\"/> based on the specified key.");
		sb.AppendLine("/// <param name=\"key\">The key identifying the implementation to create.</param>");
		sb.AppendLine($"/// <returns>A new instance of <see cref=\"{interfaceShortName}\"/>.</returns>");
		sb.AppendLine("/// <exception cref=\"System.ArgumentException\">Thrown when the key does not match any known implementation.</exception>");
		sb.AppendLine($"public static {interfaceFullName} Create(string key)");
		sb.OpenBrace();
		sb.AppendLine("return key switch");
		sb.OpenBrace();

		foreach ((string fullTypeName, string key) in implementations)
		{
			sb.AppendLine($"\"{GeneratorHelpers.EscapeString(key)}\" => new {fullTypeName}(),");
		}

		sb.AppendLine("_ => throw new System.ArgumentException($\"No implementation registered for key '{{key}}'.\", nameof(key)),");
		sb.Outdent();
		sb.AppendLine("};");
		sb.CloseBrace();
	}

	private static void AppendGetKeysMethod(SourceBuilder sb, EquatableArray<(string TypeName, string Key)> implementations)
	{
		sb.AppendSummary("Gets all registered factory keys.");
		sb.AppendLine("/// <returns>An enumerable of all registered keys.</returns>");
		sb.AppendLine("public static System.Collections.Generic.IEnumerable<string> GetKeys()");
		sb.OpenBrace();

		foreach ((_, string key) in implementations)
		{
			sb.AppendLine($"yield return \"{GeneratorHelpers.EscapeString(key)}\";");
		}

		if (implementations.Count == 0)
			sb.AppendLine("yield break;");

		sb.CloseBrace();
	}

	private static INamedTypeSymbol? GetInterfaceTypeFromAttribute(INamedTypeSymbol classSymbol)
	{
		foreach (AttributeData attribute in classSymbol.GetAttributes())
		{
			if (attribute.AttributeClass?.ToDisplayString() != AttributeNames.MetadataName)
				continue;

			if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is INamedTypeSymbol typeSymbol)
				return typeSymbol;
		}

		return null;
	}

	private static string GetFactoryKey(INamedTypeSymbol type)
	{
		foreach (AttributeData attribute in type.GetAttributes())
		{
			if (attribute.AttributeClass?.ToDisplayString() != FactoryKeyAttributeName)
				continue;

			if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string key)
				return key;
		}

		return type.Name;
	}

	private static bool ImplementsInterface(INamedTypeSymbol type, INamedTypeSymbol interfaceSymbol)
	{
		foreach (INamedTypeSymbol iface in type.AllInterfaces)
		{
			if (SymbolEqualityComparer.Default.Equals(iface, interfaceSymbol))
				return true;
		}

		return false;
	}

	private static bool HasParameterlessConstructor(INamedTypeSymbol type)
	{
		foreach (IMethodSymbol constructor in type.Constructors)
		{
			if (constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public)
				return true;
		}

		// If no explicit constructors, the compiler provides a default parameterless one
		return type.Constructors.Length == 0;
	}

	private static IEnumerable<INamedTypeSymbol> GetAllNamedTypes(INamespaceSymbol namespaceSymbol)
	{
		foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
		{
			yield return type;

			foreach (INamedTypeSymbol nested in GetNestedTypes(type))
				yield return nested;
		}

		foreach (INamespaceSymbol child in namespaceSymbol.GetNamespaceMembers())
		{
			foreach (INamedTypeSymbol type in GetAllNamedTypes(child))
				yield return type;
		}
	}

	private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
	{
		foreach (INamedTypeSymbol nested in type.GetTypeMembers())
		{
			yield return nested;

			foreach (INamedTypeSymbol deepNested in GetNestedTypes(nested))
				yield return deepNested;
		}
	}
}
