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
	/// <summary>
	/// The attribute type names, in emission order. Each name is both the embedded resource
	/// (<c>{Name}.cs</c>) and the hint name (<c>{Name}.g.cs</c>) of the emitted source.
	/// </summary>
	private static readonly string[] AttributeNames =
	[
		"ActionCommandAttribute",
		"AlsoNotifyAttribute",
		"DisposeResourceAttribute",
		"ExcludeFromNotificationAttribute",
		"GenerateAbstractionAttribute",
		"GenerateAssemblyInformationAttribute",
		"GenerateAutoMapperAttribute",
		"GenerateBuilderAttribute",
		"GenerateCloneableAttribute",
		"GenerateDecoratorAttribute",
		"GenerateDisposableAttribute",
		"GenerateEnumeratorExtensionsAttribute",
		"GenerateEqualityAttribute",
		"GenerateFactoryAttribute",
		"GenerateFactoryKeyAttribute",
		"GenerateIniFileAttribute",
		"GenerateIniFileSectionAttribute",
		"GenerateIniFileValueAttribute",
		"GenerateNotificationsAttribute",
		"GenerateSingletonAttribute",
		"GenerateToStringAttribute",
		"GenerateValidatorAttribute",
		"ToStringFormatAttribute",
		"ToStringOrderAttribute",
		"PropertyMappingAttribute",
		"NotifyCanExecuteChangedAttribute",
	];

	private static readonly (string HintName, string Source)[] AttributeSources =
		[.. AttributeNames.Select(static name => ($"{name}.g.cs", AttributeSourceRewriter.ReadAndTransform($"{name}.cs")))];

	/// <inheritdoc />
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static ctx =>
		{
			foreach ((string hintName, string source) in AttributeSources)
				ctx.AddSource(hintName, source);
		});
	}
}
