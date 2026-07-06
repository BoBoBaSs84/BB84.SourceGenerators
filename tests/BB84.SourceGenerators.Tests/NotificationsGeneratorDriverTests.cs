// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BB84.SourceGenerators.Tests;

/// <summary>
/// Runtime generator-driver tests for <see cref="NotificationsGenerator"/>. These run the generator
/// in-process so its emitted source and diagnostics can be asserted directly.
/// </summary>
[TestClass]
public sealed class NotificationsGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(INotifyPropertyChanged).Assembly.Location),
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
	[GenerateNotifications]
	public partial class NotifyModel
	{
		private int _id;
		private string _name;
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("INotifyPropertyChanged"));
		Assert.Contains("INotifyPropertyChanging", generated);
	}

	[TestMethod]
	public void ShouldExcludeFieldWithExcludeAttribute()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial class ExcludeModel
	{
		private int _id;
		[ExcludeFromNotification]
		private string _secret;
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class ExcludeModel"));
		Assert.Contains("Id", generated);
		Assert.DoesNotContain("Secret", generated);
	}

	[TestMethod]
	public void ShouldGenerateAlsoNotifyForProperties()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial class AlsoNotifyModel
	{
		[AlsoNotifyFor(nameof(FullName))]
		private string _firstName;
		[AlsoNotifyFor(nameof(FullName))]
		private string _lastName;
		private string _fullName;
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class AlsoNotifyModel"));
		Assert.Contains("RaisePropertyChanged(nameof(FullName))", generated);
		Assert.Contains("RaisePropertyChanging(nameof(FullName))", generated);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "NotificationsDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new NotificationsGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
