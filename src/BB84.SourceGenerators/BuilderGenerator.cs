// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;

using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Models;

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
		=> GeneratorHelpers.RegisterClassGenerator(context, AttributeNames.MetadataName, Execute);

	private void Execute(SourceProductionContext context, (ClassDeclarationSyntax ClassSyntax, SemanticModel SemanticModel)? input)
	{
		if (!GeneratorHelpers.TryCreateContext(input, out GeneratorContext ctx))
			return;

		string builderClassName = $"{ctx.ClassName}Builder";

		ImmutableArray<PropertyDescriptor> properties = GeneratorHelpers.GetPropertyDescriptors(ctx.ClassSymbol, requireSetter: true);

		if (properties.Length == 0)
			return;

		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.BuilderGeneratorFullName, ctx.NamespaceName, ctx.OuterClasses, []);

		AppendBuilderClass(sb, ctx.ClassName, builderClassName, ctx.Accessibility, properties);

		sb.EndTypeFile(ctx.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, ctx.OuterClasses, builderClassName, null, sb);
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
