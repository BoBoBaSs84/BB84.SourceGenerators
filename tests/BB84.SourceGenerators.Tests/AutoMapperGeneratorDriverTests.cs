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
/// Runtime generator-driver tests for <see cref="AutoMapperGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class AutoMapperGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ShouldGenerateBasicMapping()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class SourceDto
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public class TargetModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public partial class OrderMapper
	{
		[GenerateAutoMapper]
		public partial TargetModel Map(SourceDto source);
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);

		string generated = generatedSources.First(s => s.Contains("partial TargetModel Map("));
		Assert.Contains("Id = source.Id", generated);
		Assert.Contains("Name = source.Name", generated);
	}

	[TestMethod]
	public void ShouldSupportPropertyMappingAttribute()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class SourceDto
	{
		public int Id { get; set; }
		public string CustomerName { get; set; }
	}

	public class TargetModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public partial class OrderMapper
	{
		[GenerateAutoMapper]
		[PropertyMapping(""CustomerName"", ""Name"")]
		public partial TargetModel Map(SourceDto source);
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);

		string generated = generatedSources.First(s => s.Contains("partial TargetModel Map("));
		Assert.Contains("Name = source.CustomerName", generated);
	}

	[TestMethod]
	public void ShouldReportUnmappedPropertyDiagnostic()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class SourceDto
	{
		public int Id { get; set; }
	}

	public class TargetModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public partial class OrderMapper
	{
		[GenerateAutoMapper]
		public partial TargetModel Map(SourceDto source);
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning && d.Id == "BB84SG0007"));
	}

	[TestMethod]
	public void ShouldHandleNullableSourceParameter()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class SourceDto
	{
		public int Id { get; set; }
	}

	public class TargetModel
	{
		public int Id { get; set; }
	}

	public partial class OrderMapper
	{
		[GenerateAutoMapper]
		public partial TargetModel Map(SourceDto? source);
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);

		string generated = generatedSources.First(s => s.Contains("partial TargetModel Map("));
		Assert.Contains("if (source is null)", generated);
		Assert.Contains("throw new System.ArgumentNullException(nameof(source))", generated);
	}

	[TestMethod]
	public void ShouldReportTypeMismatchDiagnostic()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class SourceDto
	{
		public string Id { get; set; }
	}

	public class TargetModel
	{
		public int Id { get; set; }
	}

	public partial class OrderMapper
	{
		[GenerateAutoMapper]
		public partial TargetModel Map(SourceDto source);
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0008"));
	}

	[TestMethod]
	public void ShouldReportDiagnosticForNonPartialMethod()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class SourceDto
	{
		public int Id { get; set; }
	}

	public class TargetModel
	{
		public int Id { get; set; }
	}

	public partial class OrderMapper
	{
		[GenerateAutoMapper]
		public TargetModel Map(SourceDto source) => new TargetModel();
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0010"));
	}

	[TestMethod]
	public void ShouldReportDiagnosticForNonPartialContainingClass()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class SourceDto
	{
		public int Id { get; set; }
	}

	public class TargetModel
	{
		public int Id { get; set; }
	}

	public class OrderMapper
	{
		[GenerateAutoMapper]
		public partial TargetModel Map(SourceDto source);
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0009"));
	}

	[TestMethod]
	public void ShouldMapInheritedProperties()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class BaseDto
	{
		public int Id { get; set; }
	}

	public class SourceDto : BaseDto
	{
		public string Name { get; set; }
	}

	public class TargetModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public partial class OrderMapper
	{
		[GenerateAutoMapper]
		public partial TargetModel Map(SourceDto source);
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);

		string generated = generatedSources.First(s => s.Contains("partial TargetModel Map("));
		Assert.Contains("Id = source.Id", generated);
		Assert.Contains("Name = source.Name", generated);
	}

	[TestMethod]
	public void GeneratedCodeShouldCompileSuccessfully()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class SourceDto
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public double Price { get; set; }
	}

	public class TargetModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public double Price { get; set; }
	}

	public partial class OrderMapper
	{
		[GenerateAutoMapper]
		[PropertyMapping(""Name"", ""Name"")]
		public partial TargetModel Map(SourceDto source);
	}
}";

		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "IntegrationTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new AutoMapperGenerator()];
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

		// The output compilation should have no errors in the AutoMapper-generated source
		ImmutableArray<Diagnostic> compilationDiagnostics = outputCompilation.GetDiagnostics();
		ImmutableArray<Diagnostic> autoMapperErrors = [.. compilationDiagnostics.Where(d =>
			d.Severity == DiagnosticSeverity.Error &&
			d.Location.GetLineSpan().Path.Contains("AutoMapper"))];
		Assert.IsEmpty(autoMapperErrors, string.Join("\n", autoMapperErrors.Select(e => e.ToString())));
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "AutoMapperDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new AutoMapperGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
