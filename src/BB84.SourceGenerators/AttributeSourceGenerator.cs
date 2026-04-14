// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Helpers;

using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators;

/// <summary>
/// A source generator that emits the attribute source code into the consuming project's compilation
/// so that the attributes are available without requiring a direct assembly reference.
/// </summary>
/// <remarks>
/// The attribute source is read from embedded resources (the actual attribute <c>.cs</c> files)
/// and transformed at initialization time so that any changes to the attribute classes are
/// automatically reflected in the emitted code.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class AttributeSourceGenerator : IIncrementalGenerator
{
	private static readonly string GenerateAbstractionAttributeSource =
		AttributeSourceRewriter.ReadAndTransform("GenerateAbstractionAttribute.cs");

	private static readonly string GenerateBuilderAttributeSource =
		AttributeSourceRewriter.ReadAndTransform("GenerateBuilderAttribute.cs");

	private static readonly string GenerateEnumeratorExtensionsAttributeSource =
		AttributeSourceRewriter.ReadAndTransform("GenerateEnumeratorExtensionsAttribute.cs");

	private static readonly string GenerateIniFileAttributeSource =
		AttributeSourceRewriter.ReadAndTransform("GenerateIniFileAttribute.cs");

	private static readonly string GenerateIniFileSectionAttributeSource =
		AttributeSourceRewriter.ReadAndTransform("GenerateIniFileSectionAttribute.cs");

	private static readonly string GenerateIniFileValueAttributeSource =
		AttributeSourceRewriter.ReadAndTransform("GenerateIniFileValueAttribute.cs");

	private static readonly string GenerateNotificationsAttributeSource =
		AttributeSourceRewriter.ReadAndTransform("GenerateNotificationsAttribute.cs");

	private static readonly string GenerateToStringAttributeSource =
		AttributeSourceRewriter.ReadAndTransform("GenerateToStringAttribute.cs");

	/// <inheritdoc />
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource("GenerateAbstractionAttribute.g.cs", GenerateAbstractionAttributeSource);
			ctx.AddSource("GenerateBuilderAttribute.g.cs", GenerateBuilderAttributeSource);
			ctx.AddSource("GenerateEnumeratorExtensionsAttribute.g.cs", GenerateEnumeratorExtensionsAttributeSource);
			ctx.AddSource("GenerateIniFileAttribute.g.cs", GenerateIniFileAttributeSource);
			ctx.AddSource("GenerateIniFileSectionAttribute.g.cs", GenerateIniFileSectionAttributeSource);
			ctx.AddSource("GenerateIniFileValueAttribute.g.cs", GenerateIniFileValueAttributeSource);
			ctx.AddSource("GenerateNotificationsAttribute.g.cs", GenerateNotificationsAttributeSource);
			ctx.AddSource("GenerateToStringAttribute.g.cs", GenerateToStringAttributeSource);
		});
	}
}
