// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BB84.SourceGenerators.Tests;

/// <summary>
/// Runtime generator-driver tests for <see cref="ValidatorGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class ValidatorGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(RequiredAttribute).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ShouldGenerateSource()
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
  	public string? Name { get; set; }

  	[RegularExpression(@""^[^@\s]+@[^@\s]+\.[^@\s]+$"")]
  	public string? Email { get; set; }

  	[EmailAddress]
  	public string? CompanyEmail { get; set; }

  	[Url]
  	public string? Website { get; set; }

  	[Phone]
  	public string? PhoneNumber { get; set; }

  	[CreditCard]
  	public string? CC { get; set; }

  	[Range(1, 150)]
  	public int Age { get; set; }

  	[StringLength(500, MinimumLength = 3)]
  	public string? Bio { get; set; }

  	[MinLength(5)]
  	[MaxLength(50)]
  	public string? Password { get; set; }

  	[Compare(nameof(Password))]
  	public string? ConfirmPassword { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("Dictionary<string, List<string>> Validate()"));
		Assert.Contains("Validate()", generated);
	}

	[TestMethod]
	public void EmptyClassShouldGenerateSource()
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

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	[TestMethod]
	public void AbstractClassShouldReportDiagnostic()
	{
		string source = @"
using System.ComponentModel.DataAnnotations;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateValidator]
	public abstract partial class AbstractModel
	{
		[Range(1, int.MaxValue)]
		public int Id { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0001"));
	}

	[TestMethod]
	public void ShouldIncludeInheritedProperties()
	{
		string source = @"
using System.ComponentModel.DataAnnotations;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public abstract class AbstractModel
	{
		[Range(1, int.MaxValue)]
		public int Id { get; set; }
	}

	[GenerateValidator]
	public partial class ConcreteModel : AbstractModel
	{
		[Required]
		public string Name { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class ConcreteModel"));
		Assert.Contains("Name", generated);
		Assert.Contains("Id", generated);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "ValidatorDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new ValidatorGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
