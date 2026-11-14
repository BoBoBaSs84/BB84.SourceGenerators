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
/// Runtime generator-driver tests for <see cref="CommandArgumentGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class CommandArgumentGeneratorDriverTests
{
	private const string AssemblyName = "CommandArgumentDriverTestAssembly";

	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Environment).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ShouldGenerateCompilableSourceForAllSupportedTypes()
	{
		string source = @"
#nullable enable
using System;
using System.Text;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public enum Level { Low, High }

	[Flags]
	public enum Features { None = 0, A = 1, B = 2 }

	[GenerateCommandArgument]
	public sealed partial class AllTypes
	{
		public string Text { get; }
		public bool Flag { get; }
		public bool? Switch { get; }
		public char Letter { get; }
		public byte Byte { get; }
		public sbyte SByte { get; }
		public short Short { get; }
		public ushort UShort { get; }
		public int Int { get; }
		public uint UInt { get; } = 1;
		public long Long { get; } = 2;
		public ulong ULong { get; } = 3;
		public float Float { get; } = 1.5f;
		public double Double { get; } = 2.5;
		public decimal Decimal { get; } = 3.5m;
		public DateTime DateTime { get; }
		public DateTimeOffset DateTimeOffset { get; }
		public TimeSpan TimeSpan { get; }
		public Guid Guid { get; }
		public Level Level { get; } = Level.High;
		public Features Features { get; }
		public Encoding? Encoding { get; }
		public int? NullableInt { get; }
		public string? NullableText { get; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		string generated = GetGeneratedSource(generatedSources, "AllTypes");
		Assert.Contains("public static AllTypes? Parse(string[] args)", generated);
		Assert.Contains("public static bool TryParse(string[] args, out AllTypes? result, out IReadOnlyList<string> errors)", generated);
		Assert.Contains("private AllTypes(", generated);
		Assert.Contains("private static bool TryGetEncoding(string value, out global::System.Text.Encoding result)", generated);
		Assert.Contains($"\"Usage: {AssemblyName} [options]\\n\" +", generated);
	}

	[TestMethod]
	public void ShouldOmitEncodingHelperWhenNoEncodingOption()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public partial class Options { public int Count { get; } }";

		(_, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		Assert.DoesNotContain("TryGetEncoding", GetGeneratedSource(generatedSources, "Options"));
	}

	[TestMethod]
	public void ShouldGenerateForTypeWithoutOptions()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public partial class Empty { }";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		string generated = GetGeneratedSource(generatedSources, "Empty");
		Assert.Contains($"\"Usage: {AssemblyName} [options]\";", generated);
		Assert.Contains("private Empty()", generated);
	}

	[TestMethod]
	public void ShouldSkipComputedAndNonPublicProperties()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public partial class Options
{
	public string Name { get; } = ""x"";
	public string Upper => Name.ToUpperInvariant();
	public int Length { get { return Name.Length; } }
	internal object? Hidden { get; set; }
	public static object? Shared { get; set; }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		string generated = GetGeneratedSource(generatedSources, "Options");
		Assert.Contains("private Options(string name)", generated);
		Assert.DoesNotContain("--upper", generated);
		Assert.DoesNotContain("--length", generated);
	}

	[TestMethod]
	public void ShouldSkipExcludedProperties()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument(ExcludeProperties = new[] { nameof(Cache), nameof(Machine), ""Missing"" })]
public partial class Options
{
	public string Name { get; }
	public object? Cache { get; set; }
	public string Machine { get; } = System.Environment.MachineName;
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		string generated = GetGeneratedSource(generatedSources, "Options");
		Assert.Contains("private Options(string name)", generated);
		Assert.DoesNotContain("--cache", generated);
		Assert.DoesNotContain("--machine", generated);
	}

	[TestMethod]
	public void ShouldEscapeKeywordParameterNames()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public partial class Options
{
	public string? Class { get; }
	public int Namespace { get; }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		string generated = GetGeneratedSource(generatedSources, "Options");
		Assert.Contains("private Options(string? @class, int @namespace)", generated);
		Assert.Contains("case \"--class\":", generated);
	}

	[TestMethod]
	public void ShouldAllowPrivateConstructorsAndRecords()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public sealed partial class WithPrivate
{
	private WithPrivate() { }
	public string? Name { get; }
}

[GenerateCommandArgument]
public partial record Settings
{
	public string? Name { get; init; }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		Assert.Contains("partial record Settings", GetGeneratedSource(generatedSources, "Settings"));
	}

	[TestMethod]
	public void ShouldMarkConstructorWhenTypeHasRequiredMembers()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public sealed partial class Options
{
	public required string Name { get; init; }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		Assert.Contains("[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]", GetGeneratedSource(generatedSources, "Options"));
	}

	[TestMethod]
	public void ShouldBuildUsageFromSummariesAndDefaults()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

public enum Mode { Fast, Slow }

[GenerateCommandArgument]
public partial class Options
{
	/// <summary>
	/// The ""input"" file, e.g. C:\temp.
	/// </summary>
	public string Input { get; }

	/// <summary>The mode.</summary>
	public Mode Mode { get; } = Mode.Slow;

	public string Prefix { get; } = """";

	/// <summary>Enables tracing.</summary>
	public bool Trace { get; }
}";

		(_, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		string generated = GetGeneratedSource(generatedSources, "Options");
		Assert.Contains("\"  --input <input>     The \\\"input\\\" file, e.g. C:\\\\temp.\\n\" +", generated);
		Assert.Contains("\"  --mode [mode]       The mode. Default: Slow.\\n\" +", generated);
		Assert.Contains("\"  --prefix [prefix]   Optional.\\n\" +", generated);
		Assert.Contains("\"  --trace             Enables tracing.\";", generated);
	}

	[TestMethod]
	public void ShouldReportTypeNotPartial()
		=> AssertSingleDiagnostic(@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public class Options { public string Name { get; } }", "BB84SG0035");

	[TestMethod]
	[DataRow("public Options() { }", DisplayName = "Public constructor")]
	[DataRow("internal Options(int value) { }", DisplayName = "Internal constructor")]
	[DataRow("protected Options(string value) { }", DisplayName = "Protected constructor")]
	public void ShouldReportNonPrivateConstructor(string constructor)
		=> AssertSingleDiagnostic($@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public partial class Options
{{
	{constructor}
	public string Name {{ get; set; }}
}}", "BB84SG0036");

	[TestMethod]
	public void ShouldReportPositionalRecord()
		=> AssertSingleDiagnostic(@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public partial record Options(string Name);", "BB84SG0036");

	[TestMethod]
	public void ShouldReportUnsupportedType()
		=> AssertSingleDiagnostic(@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public partial class Options { public object Value { get; } }", "BB84SG0037");

	[TestMethod]
	public void ShouldReportDuplicateOptionName()
		=> AssertSingleDiagnostic(@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public partial class Options
{
	public string Name { get; }
	public string name { get; }
}", "BB84SG0038");

	[TestMethod]
	public void ShouldReportNonConstantInitializer()
		=> AssertSingleDiagnostic(@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public partial class Options { public string Name { get; } = System.Environment.MachineName; }", "BB84SG0039");

	[TestMethod]
	public void ShouldNotGenerateForAbstractType()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace;

[GenerateCommandArgument]
public abstract partial class Options { public string Name { get; set; } }";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, _) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		Assert.IsFalse(generatedSources.Any(s => s.Contains("public static bool TryParse")));
	}

	private static void AssertSingleDiagnostic(string source, string id)
	{
		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, _) = RunGenerator(source);

		Assert.HasCount(1, diagnostics, string.Join(Environment.NewLine, diagnostics));
		Assert.AreEqual(id, diagnostics[0].Id);
		Assert.AreEqual(DiagnosticSeverity.Error, diagnostics[0].Severity);
		Assert.IsFalse(generatedSources.Any(s => s.Contains("public static bool TryParse")));
	}

	/// <summary>
	/// Asserts that the generated source compiles. Only checked on .NET: the in-memory compilation of the
	/// .NET Framework targets lacks types such as <c>IsExternalInit</c> and <c>ExcludeFromCodeCoverageAttribute</c>.
	/// </summary>
	private static void AssertCompiles(ImmutableArray<Diagnostic> compilationDiagnostics, bool allowWarnings)
	{
#if NET
		DiagnosticSeverity minimum = allowWarnings ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;
		Assert.IsEmpty(compilationDiagnostics.Where(d => d.Severity >= minimum), string.Join(Environment.NewLine, compilationDiagnostics));
#else
		_ = compilationDiagnostics;
		_ = allowWarnings;
#endif
	}

	private static string GetGeneratedSource(string[] generatedSources, string typeName)
		=> generatedSources.Single(s => s.Contains($"partial class {typeName}") || s.Contains($"partial record {typeName}"));

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources, ImmutableArray<Diagnostic> CompilationDiagnostics) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse));

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: AssemblyName,
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new CommandArgumentGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators.Select(GeneratorExtensions.AsSourceGenerator), parseOptions: (CSharpParseOptions)syntaxTree.Options);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources, outputCompilation.GetDiagnostics());
	}
}
