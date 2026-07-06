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
/// Runtime generator-driver tests for <see cref="AssemblyInformationGenerator"/>. These run the
/// generator in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class AssemblyInformationGeneratorDriverTests
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
[assembly: System.Reflection.AssemblyTitle(""Test Assembly"")]
[assembly: System.Reflection.AssemblyMetadata(""BuildCommit"", ""abc123"")]
[assembly: System.Reflection.AssemblyMetadata(""BuildHash"", ""0x001122"")]
[assembly: System.CLSCompliant(true)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""FriendA"")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""FriendB"")]

namespace TestNamespace
{
  [GenerateAssemblyInformation]
  public partial class InfoModel
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("const string Title"));
		Assert.Contains("const string Version", generated);
		Assert.Contains("const string Company", generated);
		Assert.Contains("const string Copyright", generated);
		Assert.Contains("const string FileVersion", generated);
		Assert.Contains("const string InformationalVersion", generated);
		Assert.Contains("public static readonly Dictionary<string, List<string>> Attributes", generated);
		Assert.DoesNotContain("[\"System.Reflection.AssemblyTitleAttribute\"] = \"Test Assembly\"", generated);
		Assert.Contains("[\"System.Reflection.AssemblyMetadataAttribute\"]", generated);
		Assert.Contains("[\"System.CLSCompliantAttribute\"] = new List<string> { \"True\" }", generated);
		Assert.Contains("[\"System.Runtime.CompilerServices.InternalsVisibleToAttribute\"] = new List<string> { \"FriendA\", \"FriendB\" }", generated);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "AssemblyInformationDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new AssemblyInformationGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
