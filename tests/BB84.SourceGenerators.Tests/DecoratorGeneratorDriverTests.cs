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
/// Runtime generator-driver tests for <see cref="DecoratorGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class DecoratorGeneratorDriverTests
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
	public interface IMyService
	{
		string GetValue();
		string Name { get; set; }
	}

	[GenerateDecorator]
	public partial class MyServiceDecorator : IMyService
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class MyServiceDecorator"));
		Assert.Contains("_inner", generated);
		Assert.Contains("virtual", generated);
		Assert.Contains("GetValue()", generated);
		Assert.Contains("Name", generated);
		Assert.Contains("MyServiceDecorator(IMyService inner)", generated);
	}

	[TestMethod]
	public void ShouldReportDiagnosticForNoInterface()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateDecorator]
	public partial class BadDecorator
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0003"));
	}

	[TestMethod]
	public void ShouldHandleMultipleInterfaces()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IFirst
	{
		void DoFirst();
	}

	public interface ISecond
	{
		int DoSecond();
	}

	[GenerateDecorator]
	public partial class MultiDecorator : IFirst, ISecond
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class MultiDecorator"));
		Assert.Contains("DoFirst()", generated);
		Assert.Contains("DoSecond()", generated);
	}

	[TestMethod]
	public void ShouldHandleGenericMethods()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IGenericService
	{
		T Transform<T>(T input) where T : class;
		TResult Convert<TInput, TResult>(TInput input) where TResult : new();
	}

	[GenerateDecorator]
	public partial class GenericDecorator : IGenericService
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class GenericDecorator"));
		Assert.Contains("Transform<T>", generated);
		Assert.Contains("Convert<TInput, TResult>", generated);
		Assert.Contains("where T : class", generated);
		Assert.Contains("where TResult : new()", generated);
	}

	[TestMethod]
	public void ShouldGeneratePartialMethodHooks()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IMyService
	{
		string GetValue();
		void DoWork();
	}

	[GenerateDecorator]
	public partial class HookedDecorator : IMyService
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class HookedDecorator"));
		Assert.Contains("partial void OnGetValueExecuting();", generated);
		Assert.Contains("partial void OnGetValueExecuted();", generated);
		Assert.Contains("partial void OnDoWorkExecuting();", generated);
		Assert.Contains("partial void OnDoWorkExecuted();", generated);
	}

	[TestMethod]
	public void ShouldHandleEventDelegation()
	{
		string source = @"
using System;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IEventService
	{
		event EventHandler Changed;
		void DoWork();
	}

	[GenerateDecorator]
	public partial class EventDecorator : IEventService
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class EventDecorator"));
		Assert.Contains("event EventHandler Changed", generated);
		Assert.Contains("add =>", generated);
		Assert.Contains("remove =>", generated);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "DecoratorDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new DecoratorGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
