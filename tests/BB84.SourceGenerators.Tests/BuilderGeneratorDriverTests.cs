// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BB84.SourceGenerators.Tests;

/// <summary>
/// Runtime generator-driver tests for <see cref="BuilderGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class BuilderGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateBuilder]
  public partial class BuildModel
  {
    public int Id { get; set; }
    public string Name { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("BuildModelBuilder"));
		Assert.Contains("BuildModelBuilder", generated);
		Assert.Contains("WithId", generated);
		Assert.Contains("WithName", generated);
		Assert.Contains("Build()", generated);
	}

	[TestMethod]
	public void EmptyClassShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateBuilder]
  public partial class EmptyBuildModel
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "BuilderDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new BuilderGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
