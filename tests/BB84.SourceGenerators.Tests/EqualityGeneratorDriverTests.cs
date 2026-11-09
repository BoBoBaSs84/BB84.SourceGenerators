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
/// Runtime generator-driver tests for <see cref="EqualityGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class EqualityGeneratorDriverTests
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
  [GenerateEquality]
  public partial class EqModel
  {
    public int Id { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("Equals"));
		Assert.Contains("IEquatable", generated);
		Assert.Contains("GetHashCode", generated);
		Assert.Contains("operator ==", generated);
	}

	[TestMethod]
	public void EmptyClassShouldGenerateSource()
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

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	[TestMethod]
	public void ShouldExcludeNameofProperties()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEquality(nameof(EqExModel.Secret))]
	public partial class EqExModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Secret { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class EqExModel"));
		Assert.Contains("Id", generated);
		Assert.Contains("Name", generated);
		Assert.DoesNotContain("Secret", generated);
	}

	[TestMethod]
	public void ShouldIncludeInheritedPropertiesWhenRequested()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  public class BaseModel
  {
    public int BaseId { get; set; }
  }

  [GenerateEquality(IncludeInherited = true)]
  public partial class DerivedModel : BaseModel
  {
    public string Name { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string generated = generatedSources.First(s => s.Contains("partial class DerivedModel"));
		Assert.Contains("BaseId", generated);
		Assert.Contains("Name", generated);
	}

	[TestMethod]
	public void ShouldNotIncludeInheritedPropertiesByDefault()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  public class BaseModel
  {
    public int BaseId { get; set; }
  }

  [GenerateEquality]
  public partial class DerivedModel : BaseModel
  {
    public string Name { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string generated = generatedSources.First(s => s.Contains("partial class DerivedModel"));
		Assert.DoesNotContain("BaseId", generated);
		Assert.Contains("Name", generated);
	}

	[TestMethod]
	public void ShouldEmitElementWiseComparisonForCollections()
	{
		string source = @"
using System.Collections.Generic;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEquality]
  public partial class CollectionModel
  {
    public List<string> Tags { get; set; }
    public int[] Codes { get; set; }
    public Dictionary<string, int> Scores { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string generated = generatedSources.First(s => s.Contains("partial class CollectionModel"));
		Assert.Contains("CollectionEquals(Tags, other.Tags)", generated);
		Assert.Contains("CollectionEquals(Codes, other.Codes)", generated);
		Assert.Contains("DictionaryEquals(Scores, other.Scores)", generated);
		Assert.Contains("SequenceEqual", generated);
	}

	[TestMethod]
	public void ShouldGenerateStructSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEquality]
  public partial struct EqStruct
  {
    public int Id { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string generated = generatedSources.First(s => s.Contains("partial struct EqStruct"));
		Assert.Contains("public partial struct EqStruct : IEquatable<EqStruct>", generated);
		Assert.Contains("=> obj is EqStruct other && Equals(other);", generated);
		Assert.Contains("public bool Equals(EqStruct other)", generated);
		Assert.Contains("public static bool operator ==(EqStruct left, EqStruct right)", generated);
		Assert.Contains("=> left.Equals(right);", generated);
		Assert.Contains("public static bool operator !=(EqStruct left, EqStruct right)", generated);
		Assert.Contains("=> !left.Equals(right);", generated);
		Assert.DoesNotContain("ReferenceEquals", generated);
		Assert.DoesNotContain("is null", generated);
	}

	[TestMethod]
	public void EmptyStructShouldReturnConstantHashCode()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEquality]
  public partial struct EmptyEqStruct
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string generated = generatedSources.First(s => s.Contains("partial struct EmptyEqStruct"));
		Assert.Contains("return 0;", generated);
		Assert.DoesNotContain("base.GetHashCode()", generated);
	}

	[TestMethod]
	public void ShouldNestStructInsideOuterClass()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  public partial class Outer
  {
    [GenerateEquality]
    internal partial struct Inner
    {
      public int Id { get; set; }
    }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string generated = generatedSources.First(s => s.Contains("partial struct Inner"));
		Assert.Contains("public partial class Outer", generated);
	}

	[TestMethod]
	public void ShouldNotGenerateForRecordStruct()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEquality]
  public partial record struct EqRecordStruct(int Id);
}";

		(_, string[] generatedSources) = RunGenerator(source);

		Assert.IsFalse(generatedSources.Any(s => s.Contains("EqRecordStruct")));
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "EqualityDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new EqualityGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
