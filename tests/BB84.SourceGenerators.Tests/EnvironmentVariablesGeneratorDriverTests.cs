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
/// Runtime generator-driver tests for <see cref="EnvironmentVariablesGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class EnvironmentVariablesGeneratorDriverTests
{
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
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public enum Level { Low, High }

	[Flags]
	public enum Features { None = 0, A = 1, B = 2 }

	[GenerateEnvironmentVariables(""App"")]
	public sealed partial class AllTypes
	{
		public string Text { get; init; } = ""text"";
		public bool Flag { get; init; }
		public char Letter { get; init; }
		public byte Byte { get; init; }
		public sbyte SByte { get; init; }
		public short Short { get; init; }
		public ushort UShort { get; init; }
		public int Int { get; init; }
		public uint UInt { get; init; } = 1;
		public long Long { get; init; } = 2;
		public ulong ULong { get; init; } = 3;
		public float Float { get; init; } = 1.5f;
		public double Double { get; init; } = 2.5;
		public decimal Decimal { get; init; } = 3.5m;
		public DateTime DateTime { get; init; }
		public DateTimeOffset DateTimeOffset { get; init; }
		public TimeSpan TimeSpan { get; init; }
		public Guid Guid { get; init; }
		public Level Level { get; init; } = Level.High;
		public Features Features { get; init; }
		public int? NullableInt { get; init; }
		public string? NullableText { get; init; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		string generated = GetGeneratedSource(generatedSources, "AllTypes");
		Assert.Contains("public static AllTypes Read()", generated);
		Assert.Contains("public static bool TryRead(Func<string, string?> lookup, out AllTypes? result, out IReadOnlyList<string> errors)", generated);
		Assert.Contains("lookup(\"APP_NULLABLE_INT\")", generated);
		Assert.Contains("float floatValue = 1.5F;", generated);
		Assert.Contains("(global::TestNamespace.Level)(1)", generated);
	}

	[TestMethod]
	public void ShouldBuildVariableNamesFromNamingOptionsAndOverrides()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""MyApp"", NamingStyle = EnvironmentVariableNamingStyle.AsIs, Separator = ""__"")]
	public partial class AsIsSettings
	{
		public string? ConnectTimeoutMs { get; set; }

		[EnvironmentVariableName(""ENCRYPT"")]
		public string? Encrypt { get; set; }
	}

	[GenerateEnvironmentVariables(""MyApp"")]
	public partial class SnakeSettings
	{
		public string? ConnectTimeoutMs { get; set; }
		public string? HTTPPort { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: true);
		string asIs = GetGeneratedSource(generatedSources, "AsIsSettings");
		Assert.Contains("lookup(\"MyApp__ConnectTimeoutMs\")", asIs);
		Assert.Contains("lookup(\"ENCRYPT\")", asIs);
		string snake = GetGeneratedSource(generatedSources, "SnakeSettings");
		Assert.Contains("lookup(\"MYAPP_CONNECT_TIMEOUT_MS\")", snake);
		Assert.Contains("lookup(\"MYAPP_HTTP_PORT\")", snake);
	}

	[TestMethod]
	public void ShouldIncludeInheritedPropertiesOnlyWhenRequested()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public abstract record BaseSettings
	{
		public string? Server { get; init; }
	}

	[GenerateEnvironmentVariables(""App"", IncludeInherited = true)]
	public sealed partial record WithInherited : BaseSettings
	{
		public int? Port { get; init; }
	}

	[GenerateEnvironmentVariables(""App"")]
	public sealed partial record WithoutInherited : BaseSettings
	{
		public int? Port { get; init; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: true);
		Assert.Contains("lookup(\"APP_SERVER\")", GetGeneratedSource(generatedSources, "WithInherited"));
		Assert.DoesNotContain("lookup(\"APP_SERVER\")", GetGeneratedSource(generatedSources, "WithoutInherited"));
	}

	[TestMethod]
	public void ShouldSkipExcludedProperties()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"", ExcludeProperties = new[] { nameof(Cache) })]
	public partial class Settings
	{
		public string? Name { get; set; }
		public object? Cache { get; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: true);
		Assert.DoesNotContain("APP_CACHE", GetGeneratedSource(generatedSources, "Settings"));
	}

	[TestMethod]
	public void ShouldImplementPartialProperties()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"", Writable = true)]
	public static partial class Live
	{
		public static partial string? Token { get; set; }
		public static partial int Port { get; private set; }
		public static partial bool? Verbose { get; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		string generated = GetGeneratedSource(generatedSources, "Live");
		Assert.Contains("public static partial string? Token", generated);
		Assert.Contains("private set => Environment.SetEnvironmentVariable(\"APP_PORT\", value.ToString(CultureInfo.InvariantCulture));", generated);
		Assert.DoesNotContain("public static bool TryRead", generated);
	}

	[TestMethod]
	public void ShouldSkipComputedProperties()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"")]
	public partial class Settings
	{
		public string? Name { get; set; }
		public string Upper => Name?.ToUpperInvariant() ?? string.Empty;
		public int Length { get { return Name?.Length ?? 0; } }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		string generated = GetGeneratedSource(generatedSources, "Settings");
		Assert.DoesNotContain("APP_UPPER", generated);
		Assert.DoesNotContain("APP_LENGTH", generated);
	}

	[TestMethod]
	public void ShouldReportDiagnosticForNonPartialType()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"")]
	public class Settings
	{
		public string Name { get; set; }
	}
}";

		AssertSingleDiagnostic(source, "BB84SG0026");
	}

	[TestMethod]
	[DataRow("")]
	[DataRow(" ")]
	[DataRow("A=B")]
	public void ShouldReportDiagnosticForInvalidPrefix(string prefix)
	{
		string source = $@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{{
	[GenerateEnvironmentVariables(""{prefix}"")]
	public partial class Settings
	{{
		public string Name {{ get; set; }}
	}}
}}";

		AssertSingleDiagnostic(source, "BB84SG0027");
	}

	[TestMethod]
	public void ShouldReportDiagnosticForInvalidOverrideName()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"")]
	public partial class Settings
	{
		[EnvironmentVariableName("""")]
		public string Name { get; set; }
	}
}";

		AssertSingleDiagnostic(source, "BB84SG0027");
	}

	[TestMethod]
	public void ShouldReportDiagnosticForUnsupportedType()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"")]
	public partial class Settings
	{
		public object Value { get; set; }
	}
}";

		AssertSingleDiagnostic(source, "BB84SG0028");
	}

	[TestMethod]
	public void ShouldReportDiagnosticForGetOnlyProperty()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"")]
	public partial class Settings
	{
		public string Name { get; }
	}
}";

		AssertSingleDiagnostic(source, "BB84SG0029");
	}

	[TestMethod]
	public void ShouldReportDiagnosticForInaccessibleInheritedSetter()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public abstract class BaseSettings
	{
		public string Name { get; private set; }
	}

	[GenerateEnvironmentVariables(""App"", IncludeInherited = true)]
	public partial class Settings : BaseSettings
	{
	}
}";

		AssertSingleDiagnostic(source, "BB84SG0029");
	}

	[TestMethod]
	public void ShouldReportDiagnosticForPositionalRecord()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"")]
	public partial record Settings(string Name);
}";

		AssertSingleDiagnostic(source, "BB84SG0030");
	}

	[TestMethod]
	public void ShouldReportDiagnosticForDuplicateVariableName()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"")]
	public partial class Settings
	{
		public string Name { get; set; }

		[EnvironmentVariableName(""app_name"")]
		public string OtherName { get; set; }
	}
}";

		AssertSingleDiagnostic(source, "BB84SG0031");
	}

	[TestMethod]
	public void ShouldReportDiagnosticForPartialSetterWithoutWritable()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"")]
	public partial class Settings
	{
		public partial string Name { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, _, _) = RunGenerator(source);

		Assert.HasCount(1, diagnostics);
		Assert.AreEqual("BB84SG0032", diagnostics[0].Id);
	}

	[TestMethod]
	public void ShouldReportDiagnosticForExcludedRequiredMember()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"", ExcludeProperties = new[] { ""Secret"" })]
	public partial class Settings
	{
		public string Name { get; set; }
		public required string Secret { get; set; }
	}
}";

		AssertSingleDiagnostic(source, "BB84SG0033");
	}

	[TestMethod]
	public void ShouldReportDiagnosticForNonConstantInitializerOnInitOnlyProperty()
	{
		string source = @"
using System;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"")]
	public partial class Settings
	{
		public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
	}
}";

		AssertSingleDiagnostic(source, "BB84SG0034");
	}

	[TestMethod]
	public void ShouldAssignNonConstantInitializerPropertyAfterConstruction()
	{
		string source = @"
#nullable enable
using System;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnvironmentVariables(""App"")]
	public partial class Settings
	{
		public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationDiagnostics) = RunGenerator(source);

		Assert.IsEmpty(diagnostics);
		AssertCompiles(compilationDiagnostics, allowWarnings: false);
		Assert.Contains("result.Timeout = timeoutValue;", GetGeneratedSource(generatedSources, "Settings"));
	}

	private static void AssertSingleDiagnostic(string source, string id)
	{
		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, _) = RunGenerator(source);

		Assert.HasCount(1, diagnostics, string.Join(Environment.NewLine, diagnostics));
		Assert.AreEqual(id, diagnostics[0].Id);
		Assert.AreEqual(DiagnosticSeverity.Error, diagnostics[0].Severity);
		Assert.IsFalse(generatedSources.Any(s => s.Contains("public static bool TryRead")));
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
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "EnvironmentVariablesDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new EnvironmentVariablesGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators.Select(GeneratorExtensions.AsSourceGenerator), parseOptions: (CSharpParseOptions)syntaxTree.Options);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources, outputCompilation.GetDiagnostics());
	}
}
