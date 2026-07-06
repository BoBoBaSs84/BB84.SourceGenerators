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
/// Runtime generator-driver tests for <see cref="DisposableGenerator"/>. Unlike
/// <c>DisposableGeneratorTests</c> (which exercises pre-compiled models), these run the
/// generator in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class DisposableGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	// Provides System.IAsyncDisposable regardless of target framework (corlib on modern .NET,
	// Microsoft.Bcl.AsyncInterfaces transitively on .NET Framework test hosts).
	private static readonly MetadataReference AsyncDisposableReference =
		MetadataReference.CreateFromFile(typeof(IAsyncDisposable).Assembly.Location);

	[TestMethod]
	public void ShouldGenerateDisposePatternForBasicClass()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateDisposable]
	public partial class BasicModel
	{
		[DisposeResource]
		private readonly System.IDisposable _resource;
	}
}";
		(ImmutableArray<Diagnostic> diagnostics, string[] generated) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string src = generated.First(s => s.Contains("partial class BasicModel"));
		Assert.Contains(": IDisposable", src);
		Assert.Contains("private bool _disposed;", src);
		Assert.Contains("protected void ThrowIfDisposed()", src);
		Assert.Contains("protected virtual void Dispose(bool disposing)", src);
		Assert.Contains("public void Dispose()", src);
		Assert.Contains("GC.SuppressFinalize(this);", src);
		Assert.Contains("_resource?.Dispose();", src);
	}

	[TestMethod]
	public void SealedClassShouldEmitPrivateNonVirtualMembers()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateDisposable]
	public sealed partial class SealedModel
	{
		[DisposeResource]
		private readonly System.IDisposable _resource;
	}
}";
		(ImmutableArray<Diagnostic> diagnostics, string[] generated) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string src = generated.First(s => s.Contains("partial class SealedModel"));
		Assert.Contains("private void ThrowIfDisposed()", src);
		Assert.Contains("private void Dispose(bool disposing)", src);
		Assert.DoesNotContain("protected virtual", src);
	}

	[TestMethod]
	public void FinalizerOptionShouldEmitFinalizer()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateDisposable(generateFinalizer: true)]
	public partial class FinalizerModel
	{
		[DisposeResource]
		private readonly System.IDisposable _resource;
	}
}";
		(ImmutableArray<Diagnostic> diagnostics, string[] generated) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string src = generated.First(s => s.Contains("partial class FinalizerModel"));
		Assert.Contains("~FinalizerModel()", src);
		Assert.Contains("Dispose(false);", src);
	}

	[TestMethod]
	public void AsyncOptionShouldEmitAsyncDisposableWhenAvailable()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateDisposable(async: true)]
	public partial class AsyncModel
	{
		[DisposeResource]
		private readonly System.IDisposable _resource;
	}
}";
		(ImmutableArray<Diagnostic> diagnostics, string[] generated) = RunGenerator(source, AsyncDisposableReference);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsEmpty(diagnostics.Where(d => d.Id == "BB84SG0006"));
		string src = generated.First(s => s.Contains("partial class AsyncModel"));
		Assert.Contains("IDisposable, IAsyncDisposable", src);
		Assert.Contains("using System.Threading.Tasks;", src);
		Assert.Contains("public async ValueTask DisposeAsync()", src);
		Assert.Contains("DisposeAsyncCore()", src);
	}

#if NETFRAMEWORK
	// System.IAsyncDisposable does not exist in the .NET Framework mscorlib, so a compilation
	// referencing only mscorlib reproduces the "not available" scenario. On modern targets corlib
	// always provides the type, so this branch cannot occur there and the test is compiled only for
	// .NET Framework. (The shared References cannot be reused here: their netstandard facade forwards
	// IAsyncDisposable on hosts running this build under Mono, which would suppress the diagnostic.)
	[TestMethod]
	public void AsyncOptionWithoutIAsyncDisposableShouldReportDiagnosticAndSkipAsync()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateDisposable(async: true)]
	public partial class UnsupportedAsyncModel
	{
		[DisposeResource]
		private readonly System.IDisposable _resource;
	}
}";
		// Use a bare mscorlib-only reference set so System.IAsyncDisposable is genuinely absent.
		// The shared References include the netstandard facade, which forwards IAsyncDisposable on
		// hosts running the .NET Framework build under Mono - resolving the type and suppressing the
		// diagnostic. mscorlib (the .NET Framework corlib) never declares IAsyncDisposable.
		MetadataReference[] references =
		[
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		];
		(ImmutableArray<Diagnostic> diagnostics, string[] generated) = RunGeneratorWithReferences(source, references);

		ImmutableArray<Diagnostic> warnings = [.. diagnostics.Where(d => d.Id == "BB84SG0006")];
		Assert.IsNotEmpty(warnings);
		Assert.AreEqual(DiagnosticSeverity.Warning, warnings[0].Severity);

		string src = generated.First(s => s.Contains("partial class UnsupportedAsyncModel"));
		Assert.DoesNotContain("DisposeAsync", src);
		Assert.DoesNotContain("IAsyncDisposable", src);
	}
#endif

	[TestMethod]
	public void OrderedDisposalShouldEmitFieldsInOrder()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateDisposable]
	public partial class OrderedModel
	{
		[DisposeResource]
		private readonly System.IDisposable _first;
		[DisposeResource(order: 1)]
		private readonly System.IDisposable _second;
		[DisposeResource(order: 2)]
		private readonly System.IDisposable _third;
	}
}";
		(ImmutableArray<Diagnostic> diagnostics, string[] generated) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string src = generated.First(s => s.Contains("partial class OrderedModel"));
		int second = src.IndexOf("_second?.Dispose();", StringComparison.Ordinal);
		int third = src.IndexOf("_third?.Dispose();", StringComparison.Ordinal);
		int first = src.IndexOf("_first?.Dispose();", StringComparison.Ordinal);
		Assert.IsTrue(second >= 0 && third > second && first > third,
			$"Expected order second < third < first, got {second}/{third}/{first}");
	}

	[TestMethod]
	public void NestedClassShouldWrapOuterPartialClasses()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public partial class Outer
	{
		[GenerateDisposable]
		public partial class Inner
		{
			[DisposeResource]
			private readonly System.IDisposable _resource;
		}
	}
}";
		(ImmutableArray<Diagnostic> diagnostics, string[] generated) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string src = generated.First(s => s.Contains("partial class Inner"));
		Assert.Contains("partial class Outer", src);
		Assert.Contains("partial class Inner", src);
	}

	[TestMethod]
	public void ClassWithoutDisposeResourceFieldsShouldStillEmitScaffold()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateDisposable]
	public partial class EmptyModel
	{
	}
}";
		(ImmutableArray<Diagnostic> diagnostics, string[] generated) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		string src = generated.First(s => s.Contains("partial class EmptyModel"));
		Assert.Contains("public void Dispose()", src);
		Assert.Contains("Dispose(true);", src);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(
		string source, params MetadataReference[] extraReferences)
		=> RunGeneratorWithReferences(source, [.. References, .. extraReferences]);

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGeneratorWithReferences(
		string source, MetadataReference[] references)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "DisposableDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new DisposableGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
