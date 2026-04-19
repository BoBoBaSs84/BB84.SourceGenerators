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

[TestClass]
public sealed class GeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ToStringGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateToString]
  public partial class MyModel
  {
    public int Id { get; set; }
    public string Name { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ToStringGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources, "Expected at least one generated source.");
		string generated = generatedSources.First(s => s.Contains("override string ToString()"));
		Assert.Contains("Id", generated);
		Assert.Contains("Name", generated);
	}

	[TestMethod]
	public void ToStringGeneratorEmptyClassShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateToString]
  public partial class EmptyModel
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ToStringGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("override string ToString()"));
		Assert.Contains("EmptyModel", generated);
	}

	[TestMethod]
	public void EqualityGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEquality]
  public partial class EqModel
  {
    public int Id { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<EqualityGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("Equals"));
		Assert.Contains("IEquatable", generated);
		Assert.Contains("GetHashCode", generated);
		Assert.Contains("operator ==", generated);
	}

	[TestMethod]
	public void EqualityGeneratorEmptyClassShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEquality]
  public partial class EmptyEqModel
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<EqualityGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	[TestMethod]
	public void BuilderGeneratorShouldGenerateSource()
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

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<BuilderGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("BuildModelBuilder"));
		Assert.Contains("BuildModelBuilder", generated);
		Assert.Contains("WithId", generated);
		Assert.Contains("WithName", generated);
		Assert.Contains("Build()", generated);
	}

	[TestMethod]
	public void BuilderGeneratorEmptyClassShouldGenerateSource()
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

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<BuilderGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	[TestMethod]
	public void ValidatorGeneratorShouldGenerateSource()
	{
		string source = @"
using System.ComponentModel.DataAnnotations;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateValidator]
  public partial class ValModel
  {
    [Required]
    public string Name { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ValidatorGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("List<string> Validate()"));
		Assert.Contains("Validate()", generated);
	}

	[TestMethod]
	public void ValidatorGeneratorEmptyClassShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateValidator]
  public partial class EmptyValModel
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ValidatorGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	[TestMethod]
	public void CloneableGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateCloneable]
  public partial class CloneModel
  {
    public int Id { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<CloneableGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("DeepClone"));
		Assert.Contains("Clone()", generated);
		Assert.Contains("DeepClone()", generated);
	}

	[TestMethod]
	public void EnumeratorExtensionsGeneratorShouldGenerateSource()
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

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<EnumeratorExtensionsGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("MyEnumExtensions"));
		Assert.Contains("ToStringFast", generated);
		Assert.Contains("IsDefinedFast", generated);
		Assert.Contains("GetNamesFast", generated);
		Assert.Contains("GetValuesFast", generated);
	}

	[TestMethod]
	public void EnumeratorExtensionsGeneratorEmptyEnumShouldNotGenerate()
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

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<EnumeratorExtensionsGenerator>(source);

		// Empty enum should not produce generator output (generator returns early)
		string[] nonAttributeSources = [.. generatedSources.Where(s => !s.Contains("class") || s.Contains("Extensions")).Where(s => s.Contains("EmptyEnumExtensions"))];
		Assert.IsEmpty(nonAttributeSources, "Empty enum should not produce extension methods.");
	}

	[TestMethod]
	public void NotificationPropertiesGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateNotifications]
  public partial class NotifyModel
  {
    private int _id;
    private string _name;
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<NotificationPropertiesGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("INotifyPropertyChanged"));
		Assert.Contains("INotifyPropertyChanging", generated);
	}

	[TestMethod]
	public void AssemblyInformationGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateAssemblyInformation]
  public partial class InfoModel
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<AssemblyInformationGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("const string Title"));
		Assert.Contains("const string Version", generated);
		Assert.Contains("const string Company", generated);
		Assert.Contains("const string Copyright", generated);
		Assert.Contains("const string FileVersion", generated);
		Assert.Contains("const string InformationalVersion", generated);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator<TGenerator>(string source)
		where TGenerator : IIncrementalGenerator, new()
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "TestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		// AttributeSourceGenerator must run to inject attribute definitions into the compilation
		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new TGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
