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
/// Runtime generator-driver tests for <see cref="SingletonGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class SingletonGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ShouldGenerateLazySource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateSingleton]
	public partial class MySingleton
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class MySingleton"));
		Assert.Contains("Lazy<MySingleton>", generated);
		Assert.Contains("Instance", generated);
		Assert.Contains("private MySingleton()", generated);
	}

	[TestMethod]
	public void ShouldGenerateNonLazySource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateSingleton(false)]
	public partial class MyNonLazySingleton
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class MyNonLazySingleton"));
		Assert.DoesNotContain("System.Lazy<", generated);
		Assert.Contains("Instance { get; } = new MyNonLazySingleton();", generated);
		Assert.Contains("private MyNonLazySingleton()", generated);
	}

	[TestMethod]
	public void ShouldUseInterfaceType()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IMyService { }

	[GenerateSingleton]
	public partial class MyService : IMyService
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class MyService"));
		Assert.Contains("IMyService Instance", generated);
	}

	[TestMethod]
	public void ShouldReportDiagnosticForRequiredProperty()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateSingleton]
	public partial class BadSingleton
	{
		public required string Name { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0002"));
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "SingletonDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new SingletonGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
