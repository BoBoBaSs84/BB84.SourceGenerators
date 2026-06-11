// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;
using System.Reflection;

using BB84.SourceGenerators.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class AttributeSourceGeneratorAttributeCoverageTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	private static readonly string[] GeneratedAttributeSources = RunAttributeGenerator();

	[TestMethod]
	public void AttributeSourceGeneratorShouldGenerateAllEmbeddedAttributes()
	{
		string[] expectedAttributeClassNames =
		[
			nameof(AlsoNotifyAttribute),
			nameof(DisposeResourceAttribute),
			nameof(ExcludeFromNotificationAttribute),
			nameof(GenerateAbstractionAttribute),
			nameof(GenerateAssemblyInformationAttribute),
			nameof(GenerateBuilderAttribute),
			nameof(GenerateCloneableAttribute),
			nameof(GenerateDecoratorAttribute),
			nameof(GenerateDisposableAttribute),
			nameof(GenerateEqualityAttribute),
			nameof(GenerateFactoryAttribute),
			nameof(GenerateFactoryKeyAttribute),
			nameof(GenerateEnumeratorExtensionsAttribute),
			nameof(GenerateIniFileAttribute),
			nameof(GenerateNotificationsAttribute),
			nameof(GenerateSingletonAttribute),
			nameof(GenerateIniFileSectionAttribute),
			nameof(GenerateIniFileValueAttribute),
			nameof(GenerateToStringAttribute),
			nameof(GenerateValidatorAttribute),
			nameof(GenerateAutoMapperAttribute),
			nameof(PropertyMappingAttribute),
			nameof(ToStringFormatAttribute),
		];

		Assert.HasCount(expectedAttributeClassNames.Length, GeneratedAttributeSources,
			$"Expected exactly {expectedAttributeClassNames.Length} generated attribute sources, but found {GeneratedAttributeSources.Length}.");

		foreach (string attributeClassName in expectedAttributeClassNames)
		{
			Assert.Contains(
				source => source.Contains($"class {attributeClassName}"), GeneratedAttributeSources,
				$"Expected generated source for '{attributeClassName}' was not found.");
		}
	}

	private static string[] RunAttributeGenerator()
	{
		const string source = "namespace TestNamespace { public sealed class Marker { } }";

		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "AttributeCoverageTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		GeneratorDriver driver = CSharpGeneratorDriver.Create(new AttributeSourceGenerator());
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation _, out ImmutableArray<Diagnostic> diagnostics);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		return [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];
	}
}
