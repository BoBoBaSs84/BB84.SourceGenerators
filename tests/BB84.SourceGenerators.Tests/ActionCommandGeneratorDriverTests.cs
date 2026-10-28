// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BB84.SourceGenerators.Tests;

/// <summary>
/// Runtime generator-driver tests for <see cref="ActionCommandGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class ActionCommandGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(INotifyPropertyChanged).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(ICommand).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ShouldEmitRuntimeTypes()
	{
		(_, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation("namespace TestNamespace { }");

		Assert.IsEmpty(compilationErrors, string.Join("\n", compilationErrors.Take(15)));
		Assert.Contains(s => s.Contains("public interface IActionCommand :"), generatedSources);
		Assert.Contains(s => s.Contains("public interface IAsyncActionCommand<T>"), generatedSources);
		Assert.Contains(s => s.Contains("public sealed class ActionCommand<T>"), generatedSources);
		Assert.Contains(s => s.Contains("public sealed class AsyncActionCommand<T>"), generatedSources);
	}

	[TestMethod]
	[DataRow("void Save()", "IActionCommand SaveCommand", "new global::BB84.SourceGenerators.Commands.ActionCommand(() => Save())")]
	[DataRow("void Save(int value)", "IActionCommand<int> SaveCommand", "ActionCommand<int>(parameter => Save(parameter))")]
	[DataRow("Task SaveAsync()", "IAsyncActionCommand SaveCommand", "AsyncActionCommand(cancellationToken => SaveAsync())")]
	[DataRow("Task SaveAsync(CancellationToken ct)", "IAsyncActionCommand SaveCommand", "AsyncActionCommand(cancellationToken => SaveAsync(cancellationToken))")]
	[DataRow("Task SaveAsync(string value)", "IAsyncActionCommand<string> SaveCommand", "AsyncActionCommand<string>((parameter, cancellationToken) => SaveAsync(parameter))")]
	[DataRow("Task SaveAsync(string value, CancellationToken ct)", "IAsyncActionCommand<string> SaveCommand", "AsyncActionCommand<string>((parameter, cancellationToken) => SaveAsync(parameter, cancellationToken))")]
	public void ShouldMapSignatureToCommand(string signature, string expectedProperty, string expectedFactory)
	{
		string body = signature.StartsWith("void", StringComparison.Ordinal) ? "{ }" : "=> Task.CompletedTask;";
		string source = $@"
using System.Threading;
using System.Threading.Tasks;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{{
	public partial class ViewModel
	{{
		[ActionCommand]
		private {signature} {body}
	}}
}}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.IsEmpty(diagnostics);
		Assert.IsEmpty(compilationErrors);
		string generated = generatedSources.Single(s => s.Contains("partial class ViewModel"));
		Assert.Contains(expectedProperty, generated);
		Assert.Contains(expectedFactory, generated);
	}

	[TestMethod]
	public void ShouldEmitOptions()
	{
		string source = @"
using System;
using System.Threading.Tasks;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public partial class ViewModel
	{
		private bool CanRun => true;

		[ActionCommand(Name = ""GoCommand"", CanExecute = nameof(CanRun), ExceptionHandler = nameof(OnError), AllowConcurrentExecutions = true, IncludeCancelCommand = true)]
		private Task RunAsync() => Task.CompletedTask;

		private void OnError(Exception exception) { }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.IsEmpty(diagnostics);
		Assert.IsEmpty(compilationErrors);
		string generated = generatedSources.Single(s => s.Contains("partial class ViewModel"));
		Assert.Contains("IAsyncActionCommand GoCommand", generated);
		Assert.Contains("canExecute: () => CanRun", generated);
		Assert.Contains("onException: exception => OnError(exception)", generated);
		Assert.Contains("allowConcurrentExecutions: true", generated);
		Assert.Contains("IActionCommand GoCancelCommand", generated);
	}

	[TestMethod]
	public void ShouldSupportNestedClassesAndRecords()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public partial class Outer
	{
		public partial record InnerModel
		{
			[ActionCommand]
			private void Run() { }
		}
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.IsEmpty(diagnostics);
		Assert.IsEmpty(compilationErrors);
		string generated = generatedSources.Single(s => s.Contains("RunCommand"));
		Assert.Contains("partial class Outer", generated);
		Assert.Contains("partial record InnerModel", generated);
	}

	[TestMethod]
	public void ShouldReportNonPartialContainingType()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class ViewModel
	{
		[ActionCommand]
		private void Run() { }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, _) = RunGenerator(source);

		Assert.AreEqual(DiagnosticSeverity.Error, diagnostics.Single(d => d.Id == "BB84SG0018").Severity);
	}

	[TestMethod]
	[DataRow("int Run() => 0;")]
	[DataRow("void Run(int a, int b) { }")]
	[DataRow("void Run(ref int a) { }")]
	[DataRow("void Run<T>(T value) { }")]
	[DataRow("Task<int> RunAsync() => Task.FromResult(0);")]
	[DataRow("Task RunAsync(CancellationToken a, int b) => Task.CompletedTask;")]
	public void ShouldReportUnsupportedSignature(string member)
	{
		string source = $@"
using System.Threading;
using System.Threading.Tasks;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{{
	public partial class ViewModel
	{{
		[ActionCommand]
		private {member}
	}}
}}";

		(ImmutableArray<Diagnostic> diagnostics, _) = RunGenerator(source);

		Assert.AreEqual(DiagnosticSeverity.Error, diagnostics.Single(d => d.Id == "BB84SG0019").Severity);
	}

	[TestMethod]
	[DataRow("Missing")]
	[DataRow("NotBool")]
	[DataRow("WrongParameter")]
	public void ShouldReportInvalidCanExecute(string canExecute)
	{
		string source = $@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{{
	public partial class ViewModel
	{{
		private int NotBool => 0;
		private bool WrongParameter(string value) => true;

		[ActionCommand(CanExecute = ""{canExecute}"")]
		private void Run(int value) {{ }}
	}}
}}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.AreEqual(DiagnosticSeverity.Error, diagnostics.Single(d => d.Id == "BB84SG0020").Severity);
		Assert.DoesNotContain(s => s.Contains("RunCommand"), generatedSources);
	}

	[TestMethod]
	public void ShouldReportInstanceGuardForStaticCommand()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public partial class ViewModel
	{
		private bool CanRun => true;

		[ActionCommand(CanExecute = nameof(CanRun))]
		private static void Run() { }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, _) = RunGenerator(source);

		Assert.IsTrue(diagnostics.Any(d => d.Id == "BB84SG0020"));
	}

	[TestMethod]
	public void ShouldResolveGuardGeneratedByNotifications()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial class ViewModel
	{
		[NotifyCanExecuteChanged(nameof(RunCommand))]
		private bool _canRun;

		[ActionCommand(CanExecute = nameof(CanRun))]
		private void Run() { }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.IsEmpty(diagnostics);
		Assert.IsEmpty(compilationErrors);
		Assert.Contains(s => s.Contains("canExecute: () => CanRun"), generatedSources);
		Assert.Contains(s => s.Contains("RunCommand.RaiseCanExecuteChanged();"), generatedSources);
	}

	[TestMethod]
	public void ShouldRaiseCanExecuteChangedFromPartialProperty()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial class ViewModel
	{
		[NotifyCanExecuteChanged(nameof(RunCommand), nameof(StopCommand))]
		public partial bool IsReady { get; set; }

		[ActionCommand(CanExecute = nameof(IsReady))]
		private void Run() { }

		[ActionCommand]
		private void Stop() { }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.IsEmpty(diagnostics);
		Assert.IsEmpty(compilationErrors);
		string generated = generatedSources.Single(s => s.Contains("partial bool IsReady"));
		Assert.Contains("RunCommand.RaiseCanExecuteChanged();", generated);
		Assert.Contains("StopCommand.RaiseCanExecuteChanged();", generated);
	}

	[TestMethod]
	[DataRow("Missing")]
	[DataRow("WrongSignature")]
	public void ShouldReportInvalidExceptionHandler(string handler)
	{
		string source = $@"
using System.Threading.Tasks;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{{
	public partial class ViewModel
	{{
		private void WrongSignature(string message) {{ }}

		[ActionCommand(ExceptionHandler = ""{handler}"")]
		private Task RunAsync() => Task.CompletedTask;
	}}
}}";

		(ImmutableArray<Diagnostic> diagnostics, _) = RunGenerator(source);

		Assert.AreEqual(DiagnosticSeverity.Error, diagnostics.Single(d => d.Id == "BB84SG0021").Severity);
	}

	[TestMethod]
	public void ShouldWarnForAsyncOnlyOptionsOnSyncCommand()
	{
		string source = @"
using System;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public partial class ViewModel
	{
		[ActionCommand(ExceptionHandler = nameof(OnError), AllowConcurrentExecutions = true, IncludeCancelCommand = true)]
		private void Run() { }

		private void OnError(Exception exception) { }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.AreEqual(3, diagnostics.Count(d => d.Id == "BB84SG0022" && d.Severity == DiagnosticSeverity.Warning));
		Assert.IsEmpty(compilationErrors);
		Assert.Contains(s => s.Contains("RunCommand"), generatedSources);
	}

	[TestMethod]
	[DataRow("public object RunCommand => null;")]
	[DataRow("private object _runCommand;")]
	[DataRow("[ActionCommand] private void Run(int value) { }")]
	public void ShouldReportNameConflict(string member)
	{
		string source = $@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{{
	public partial class ViewModel
	{{
		{member}

		[ActionCommand]
		private void Run() {{ }}
	}}
}}";

		(ImmutableArray<Diagnostic> diagnostics, _) = RunGenerator(source);

		Assert.IsTrue(diagnostics.Any(d => d.Id == "BB84SG0023" && d.Severity == DiagnosticSeverity.Error));
	}

	[TestMethod]
	public void ShouldWarnForUnknownNotifyCanExecuteChangedCommand()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial class ViewModel
	{
		[NotifyCanExecuteChanged(""MissingCommand"")]
		private bool _canRun;
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.AreEqual(DiagnosticSeverity.Warning, diagnostics.Single(d => d.Id == "BB84SG0024").Severity);
		Assert.IsEmpty(compilationErrors);
		Assert.DoesNotContain(s => s.Contains("MissingCommand.RaiseCanExecuteChanged"), generatedSources);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, _) = RunGeneratorWithCompilation(source);
		return (diagnostics, generatedSources);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources, ImmutableArray<Diagnostic> CompilationErrors) RunGeneratorWithCompilation(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "ActionCommandDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new NotificationsGenerator(), new ActionCommandGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];
		// Errors in the emitted attribute sources are unrelated to the command generator and are ignored here.
		ImmutableArray<Diagnostic> compilationErrors = [.. outputCompilation.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.Where(d => d.Location.SourceTree?.FilePath.Contains(nameof(AttributeSourceGenerator)) != true)];

		return (diagnostics, generatedSources, compilationErrors);
	}
}
