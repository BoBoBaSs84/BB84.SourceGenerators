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
		=> GeneratorHelpers.RegisterClassGenerator(context, AttributeNames.MetadataName, Execute);

	private void Execute(
		SourceProductionContext context,
		(ClassDeclarationSyntax ClassSyntax, SemanticModel SemanticModel)? input)
	{
		if (!GeneratorHelpers.TryCreateContext(input, out GeneratorContext ctx))
			return;

		Compilation compilation = ctx.SemanticModel.Compilation;

		// Get the interface type from the attribute
		INamedTypeSymbol? interfaceSymbol = GetInterfaceTypeFromAttribute(ctx.ClassSymbol);
		if (interfaceSymbol is null)
			return;

		if (interfaceSymbol.TypeKind != TypeKind.Interface)
		{
			GeneratorHelpers.ReportAtClass(context, ctx.ClassDeclaration, DiagnosticDescriptors.FactoryRequiresInterfaceTypeDiagnostic, ctx.ClassName, interfaceSymbol.Name);
			return;
		}

		if (DiscoverImplementations(context, ctx, compilation, interfaceSymbol) is not { } implementations)
			return;

		string interfaceName = interfaceSymbol.ToFullyQualifiedDisplayString();
		string interfaceShortName = interfaceSymbol.ToMinimalDisplayString();

		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.FactoryGeneratorFullName, ctx.NamespaceName, ctx.OuterClasses, []);

		AppendPartialClass(sb, ctx.ClassName, ctx.Accessibility, interfaceName, interfaceShortName, implementations);

		sb.EndTypeFile(ctx.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, ctx.OuterClasses, ctx.ClassName, "Factory", sb);
	}

	/// <summary>
	/// Discovers all concrete, publicly constructible implementations of the interface in the compilation,
	/// reporting a diagnostic for each duplicate factory key.
	/// </summary>
	/// <param name="context">The source production context used to report diagnostics.</param>
	/// <param name="ctx">The generator context of the factory class.</param>
	/// <param name="compilation">The compilation to search.</param>
	/// <param name="interfaceSymbol">The target interface.</param>
	/// <returns>The implementations and their keys, or <see langword="null"/> when duplicate keys were found.</returns>
	private static List<(INamedTypeSymbol Type, string Key)>? DiscoverImplementations(SourceProductionContext context, GeneratorContext ctx, Compilation compilation, INamedTypeSymbol interfaceSymbol)
	{
		List<(INamedTypeSymbol Type, string Key)> implementations = [];
		HashSet<string> seenKeys = new(StringComparer.Ordinal);
		bool hasDuplicates = false;

		foreach (INamedTypeSymbol type in GetAllNamedTypes(compilation.GlobalNamespace))
		{
			if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic)
				continue;

			if (!ImplementsInterface(type, interfaceSymbol))
				continue;

			if (!HasParameterlessConstructor(type))
				continue;

			string key = GetFactoryKey(type);

			if (!seenKeys.Add(key))
			{
				GeneratorHelpers.ReportAtClass(context, ctx.ClassDeclaration, DiagnosticDescriptors.FactoryDuplicateKeyDiagnostic, key, interfaceSymbol.Name);
				hasDuplicates = true;
				continue;
			}

			implementations.Add((type, key));
		}

		return hasDuplicates ? null : implementations;
	}

	private static void AppendPartialClass(
		SourceBuilder sb,
		string className,
		string accessibility,
		string interfaceFullName,
		string interfaceShortName,
		List<(INamedTypeSymbol Type, string Key)> implementations)
	{
		sb.OpenClass(accessibility, className);

		AppendCreateMethod(sb, interfaceFullName, interfaceShortName, implementations);
		sb.AppendLine();
		AppendGetKeysMethod(sb, implementations);

		sb.CloseClass();
	}

	private static void AppendCreateMethod(SourceBuilder sb, string interfaceFullName, string interfaceShortName, List<(INamedTypeSymbol Type, string Key)> implementations)
	{
		sb.AppendSummary($"Creates an instance of <see cref=\"{interfaceShortName}\"/> based on the specified key.");
		sb.AppendLine("/// <param name=\"key\">The key identifying the implementation to create.</param>");
		sb.AppendLine($"/// <returns>A new instance of <see cref=\"{interfaceShortName}\"/>.</returns>");
		sb.AppendLine("/// <exception cref=\"System.ArgumentException\">Thrown when the key does not match any known implementation.</exception>");
		sb.AppendLine($"public static {interfaceFullName} Create(string key)");
		sb.OpenBrace();
		sb.AppendLine("return key switch");
		sb.OpenBrace();

		foreach ((INamedTypeSymbol type, string key) in implementations)
		{
			string fullTypeName = type.ToFullyQualifiedDisplayString();
			sb.AppendLine($"\"{GeneratorHelpers.EscapeString(key)}\" => new {fullTypeName}(),");
		}

		sb.AppendLine("_ => throw new System.ArgumentException($\"No implementation registered for key '{{key}}'.\", nameof(key)),");
		sb.Outdent();
		sb.AppendLine("};");
		sb.CloseBrace();
	}

	private static void AppendGetKeysMethod(SourceBuilder sb, List<(INamedTypeSymbol Type, string Key)> implementations)
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
