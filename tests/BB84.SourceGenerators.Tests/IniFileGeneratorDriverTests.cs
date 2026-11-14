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
/// Runtime generator-driver tests for <see cref="IniFileGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class IniFileGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ShouldReportDiagnosticForNullableSectionProperty()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class MySection
	{
		[GenerateIniFileValue]
		public string? Name { get; set; }
	}

	[GenerateIniFile]
	internal sealed partial class MyConfig
	{
		[GenerateIniFileSection]
		public MySection? Section { get; set; } = new();
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0011"));
	}

	[TestMethod]
	public void ShouldReportDiagnosticForUninitializedSectionProperty()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class MySection
	{
		[GenerateIniFileValue]
		public string Name { get; set; }
	}

	[GenerateIniFile]
	internal sealed partial class MyConfig
	{
		[GenerateIniFileSection]
		public MySection Section { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0012"));
	}

	[TestMethod]
	public void ShouldReportDiagnosticForNoPublicGetter()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class MySection
	{
		[GenerateIniFileValue]
		public string Name { get; set; }
	}

	[GenerateIniFile]
	internal sealed partial class MyConfig
	{
		[GenerateIniFileSection]
		public MySection Section { private get; set; } = new();
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0013"));
	}

	[TestMethod]
	public void ShouldNotReportDiagnosticForValidSectionProperty()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class MySection
	{
		[GenerateIniFileValue]
		public string Name { get; set; }
	}

	[GenerateIniFile]
	internal sealed partial class MyConfig
	{
		[GenerateIniFileSection]
		public MySection Section { get; set; } = new();
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	[TestMethod]
	public void ShouldReadOptionsOfQualifiedAttribute()
	{
		string source = @"
namespace TestNamespace
{
	public class MySection
	{
		/// <summary>
		/// The display name.
		/// </summary>
		[BB84.SourceGenerators.Attributes.GenerateIniFileValue(""DisplayName"")]
		public string Name { get; set; }
	}

	[BB84.SourceGenerators.Attributes.GenerateIniFile(System.StringComparison.Ordinal, "":"", SerializeComments = true)]
	internal sealed partial class MyConfig
	{
		[BB84.SourceGenerators.Attributes.GenerateIniFileSection]
		public MySection Section { get; set; } = new();
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string generated = generatedSources.First(s => s.Contains("partial class MyConfig"));
		Assert.Contains("StringComparison.Ordinal))", generated);
		Assert.Contains("sb.AppendLine(\"; The display name.\");", generated);
		Assert.Contains("sb.AppendLine(\"DisplayName=\"", generated);
	}

	[TestMethod]
	public void ShouldUseSharedValueConversions()
	{
		string source = @"
using System;
using System.Collections.Generic;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class MySection
	{
		[GenerateIniFileValue]
		public double Ratio { get; set; }

		[GenerateIniFileValue]
		public DateTime LastRun { get; set; }

		[GenerateIniFileValue]
		public bool Enabled { get; set; }

		[GenerateIniFileValue]
		public List<TimeSpan> Durations { get; set; }
	}

	[GenerateIniFile]
	internal sealed partial class MyConfig
	{
		[GenerateIniFileSection]
		public MySection Section { get; set; } = new();
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string generated = generatedSources.First(s => s.Contains("partial class MyConfig"));
		Assert.Contains("double.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture)", generated);
		Assert.Contains("DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)", generated);
		Assert.Contains("bool.Parse(value.Trim() == \"1\" ? \"true\" : value.Trim() == \"0\" ? \"false\" : value)", generated);
		Assert.Contains("TimeSpan.Parse(e.Trim(), CultureInfo.InvariantCulture)", generated);
		Assert.Contains("instance.Section.Ratio.ToString(\"R\", CultureInfo.InvariantCulture)", generated);
		Assert.Contains("instance.Section.LastRun.ToString(\"O\", CultureInfo.InvariantCulture)", generated);
		Assert.Contains("(instance.Section.Enabled ? \"true\" : \"false\")", generated);
		Assert.Contains("Select(e => e.ToString(\"c\", CultureInfo.InvariantCulture))", generated);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "IniFileDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new IniFileGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
