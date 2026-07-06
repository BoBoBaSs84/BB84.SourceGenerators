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
/// Runtime generator-driver tests for <see cref="EnumeratorExtensionsGenerator"/>. These run the
/// generator in-process so its emitted extension source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class EnumeratorExtensionsGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ShouldDetectDescriptionAttributeWhenUnqualified()
	{
		string source = @"
using System.ComponentModel;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnumeratorExtensions]
	public enum MyEnum
	{
		[Description(""No value"")]
		None = 0,
		[DescriptionAttribute(""One value"")]
		One = 1,
		[System.ComponentModel.Description(""Two value"")]
		Two = 2,
		Three = 3
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);

		string generated = generatedSources.First(s => s.Contains("GetDescriptionFast(this MyEnum value)"));
		Assert.Contains("MyEnum.None => \"No value\"", generated);
		Assert.Contains("MyEnum.One => \"One value\"", generated);
		Assert.Contains("MyEnum.Two => \"Two value\"", generated);
		Assert.Contains("MyEnum.Three => nameof(MyEnum.Three)", generated);
	}

	[TestMethod]
	public void ShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEnumeratorExtensions]
  public enum MyEnum
  {
    None = 0,
    One = 1,
    Two = 2
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("MyEnumExtensions"));
		Assert.Contains("ToStringFast", generated);
		Assert.Contains("IsDefinedFast", generated);
		Assert.Contains("GetNamesFast", generated);
		Assert.Contains("GetValuesFast", generated);
	}

	[TestMethod]
	public void EmptyEnumShouldNotGenerate()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEnumeratorExtensions]
  public enum EmptyEnum
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		// Empty enum should not produce generator output (generator returns early)
		string[] nonAttributeSources = [.. generatedSources.Where(s => !s.Contains("class") || s.Contains("Extensions")).Where(s => s.Contains("EmptyEnumExtensions"))];
		Assert.IsEmpty(nonAttributeSources, "Empty enum should not produce extension methods.");
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "EnumeratorExtensionsDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new EnumeratorExtensionsGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
