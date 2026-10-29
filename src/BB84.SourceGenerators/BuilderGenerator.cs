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
/// Responsible for generating a fluent builder class for classes marked
/// with the <see cref="GenerateBuilderAttribute"/> attribute.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class BuilderGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateBuilderAttribute>();

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<BuilderRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassNode, Transform, TrackingNames.Builder, Execute);

	private static GeneratorResult<BuilderRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not ClassDeclarationSyntax classDeclaration || context.TargetSymbol is not INamedTypeSymbol classSymbol)
			return null;

		ImmutableArray<PropertyDescriptor> properties = GeneratorHelpers.GetPropertyDescriptors(classSymbol, requireSetter: true);

		if (properties.Length == 0)
			return null;

		return GeneratorResult<BuilderRequest>.FromRequest(new BuilderRequest(TypeTargetInfo.Create(classDeclaration, classSymbol), properties.ToEquatableArray()));
	}

	private static void Execute(SourceProductionContext context, BuilderRequest request)
	{
		TypeTargetInfo type = request.Type;
		string builderClassName = $"{type.Name}Builder";

		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.BuilderGeneratorFullName, type.NamespaceName, type.OuterClasses, []);

		AppendBuilderClass(sb, type.Name, builderClassName, type.Accessibility, request.Properties.AsImmutableArray());

		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, builderClassName, null, sb);
	}

	private static void AppendBuilderClass(SourceBuilder sb, string className, string builderClassName, string accessibility, ImmutableArray<PropertyDescriptor> properties)
	{
		sb.AppendSummary($"A fluent builder for creating instances of <see cref=\"{className}\"/>.");
		sb.OpenSealedClass(accessibility, builderClassName);

		(PropertyDescriptor Property, string FieldName, string TypeName)[] members =
		[
			.. properties.Select(static prop => (
				prop,
				GeneratorHelpers.PropertyNameToFieldName(prop.Name),
				prop.IsReferenceType && prop.IsNullable ? $"{prop.TypeName}?" : prop.TypeName))
		];

		// Fields
		foreach ((PropertyDescriptor prop, string fieldName, string typeName) in members)
		{
			string initializer = prop.IsReferenceType && !prop.IsNullable ? " = default!;" : ";";
			sb.AppendLine($"private {typeName} {fieldName}{initializer}");
		}

		sb.AppendLine();

		// With methods
		foreach ((PropertyDescriptor prop, string fieldName, string typeName) in members)
		{
			sb.AppendSummary($"Sets the value for the <see cref=\"{className}.{prop.Name}\"/> property.");
			sb.AppendLine("/// <param name=\"value\">The value to set.</param>");
			sb.AppendLine("/// <returns>The current builder instance.</returns>");
			sb.AppendLine($"public {builderClassName} With{prop.Name}({typeName} value)");
			sb.OpenBrace();
			sb.AppendLine($"{fieldName} = value;");
			sb.AppendLine("return this;");
			sb.CloseBrace();
			sb.AppendLine();
		}

		// Build method
		sb.AppendSummary($"Creates a new instance of <see cref=\"{className}\"/> with the configured property values.");
		sb.AppendLine($"/// <returns>A new <see cref=\"{className}\"/> instance.</returns>");
		sb.AppendLine($"public {className} Build()");
		sb.OpenBrace();
		sb.AppendLine($"return new {className}()");
		sb.OpenBrace();

		for (int i = 0; i < members.Length; i++)
		{
			(PropertyDescriptor prop, string fieldName, _) = members[i];
			string separator = i < members.Length - 1 ? "," : string.Empty;
			sb.AppendLine($"{prop.Name} = {fieldName}{separator}");
		}

		sb.Outdent();
		sb.AppendLine("};");
		sb.CloseBrace();
		sb.CloseClass();
	}
}
