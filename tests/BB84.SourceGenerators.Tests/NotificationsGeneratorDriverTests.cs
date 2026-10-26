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
		[AlsoNotify(nameof(FullName))]
		private string _firstName;
		[AlsoNotify(nameof(FullName))]
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

	[TestMethod]
	public void ShouldGenerateSourceForRecordClass()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial record NotifyRecordModel
	{
		private int _id;
		private string _name;
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("NotifyRecordModel"));
		Assert.Contains("partial record NotifyRecordModel", generated);
		Assert.Contains("INotifyPropertyChanged", generated);
	}

	[TestMethod]
	public void ShouldReportDiagnosticForPositionalRecord()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial record PositionalNotifyModel(int Id, string Name);
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Diagnostic diagnostic = diagnostics.Single(d => d.Id == "BB84SG0014");
		Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
		Assert.IsFalse(generatedSources.Any(s => s.Contains("PositionalNotifyModel")));
	}

	[TestMethod]
	public void ShouldImplementPartialPropertiesRespectingAccessibility()
	{
		string source = @"
#nullable enable
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial class PartialModel
	{
		public partial string Name { get; set; }
		internal partial int Count { get; set; }
		protected internal partial string? Notes { get; set; }
		public partial int Version { get; private set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsEmpty(compilationErrors, string.Join(Environment.NewLine, compilationErrors));
		string generated = generatedSources.First(s => s.Contains("partial class PartialModel"));
		Assert.Contains("private string _name = default!;", generated);
		Assert.Contains("public partial string Name", generated);
		Assert.Contains("internal partial int Count", generated);
		Assert.Contains("protected internal partial string? Notes", generated);
		Assert.Contains("public partial int Version", generated);
		Assert.Contains("private set", generated);
		Assert.Contains("RaisePropertyChanged(nameof(Name))", generated);
		Assert.Contains("RaisePropertyChanging(nameof(Version))", generated);
	}

	[TestMethod]
	public void ShouldImplementPartialPropertyWithAlsoNotifyAndExclude()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications(hasChanged: true)]
	public partial class PartialAttributeModel
	{
		[AlsoNotify(nameof(FullName))]
		public partial string FirstName { get; set; }

		[ExcludeFromNotification]
		public partial string Secret { get; set; }

		public string FullName => FirstName;
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsEmpty(compilationErrors, string.Join(Environment.NewLine, compilationErrors));
		string generated = generatedSources.First(s => s.Contains("partial class PartialAttributeModel"));
		Assert.Contains("RaisePropertyChanged(nameof(FirstName), nameof(FullName))", generated);
		Assert.Contains("set => _secret = value;", generated);
		Assert.DoesNotContain("nameof(Secret)", generated);
	}

	[TestMethod]
	public void ShouldResolvePartialPropertyTypesFromOtherNamespaces()
	{
		string source = @"
using System.Collections.Generic;
using BB84.SourceGenerators.Attributes;
using OtherNamespace;

namespace OtherNamespace
{
	public class Foo { }
}

namespace TestNamespace
{
	[GenerateNotifications]
	public partial class PartialTypeModel
	{
		public partial List<Foo> Items { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsEmpty(compilationErrors, string.Join(Environment.NewLine, compilationErrors));
		string generated = generatedSources.First(s => s.Contains("partial class PartialTypeModel"));
		Assert.Contains("global::System.Collections.Generic.List<global::OtherNamespace.Foo>", generated);
	}

	[TestMethod]
	public void ShouldIgnoreNonPartialProperties()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial class NonPartialModel
	{
		private int _id;
		public string Name { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, ImmutableArray<Diagnostic> compilationErrors) = RunGeneratorWithCompilation(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsEmpty(compilationErrors, string.Join(Environment.NewLine, compilationErrors));
		string generated = generatedSources.First(s => s.Contains("partial class NonPartialModel"));
		Assert.DoesNotContain("nameof(Name)", generated);
	}

	[TestMethod]
	[DataRow("{ get; }")]
	[DataRow("{ get; init; }")]
	[DataRow("{ set; }")]
	public void ShouldReportDiagnosticForPartialPropertyWithoutGetAndSet(string accessors)
	{
		string source = $@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{{
	[GenerateNotifications]
	public partial class InvalidAccessorModel
	{{
		public partial string Name {accessors}
	}}
}}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator(source);

		Diagnostic diagnostic = diagnostics.Single(d => d.Id == "BB84SG0015");
		Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
		string generated = generatedSources.First(s => s.Contains("partial class InvalidAccessorModel"));
		Assert.DoesNotContain("_name", generated);
	}

	[TestMethod]
	public void ShouldReportDiagnosticForBackingFieldConflict()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial class ConflictModel
	{
		[ExcludeFromNotification]
		private string _name;
		public partial string Name { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, _) = RunGenerator(source);

		Diagnostic diagnostic = diagnostics.Single(d => d.Id == "BB84SG0016");
		Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
	}

	[TestMethod]
	public void ShouldReportDiagnosticForStaticPartialProperty()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateNotifications]
	public partial class StaticModel
	{
		public static partial string Name { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, _) = RunGenerator(source);

		Diagnostic diagnostic = diagnostics.Single(d => d.Id == "BB84SG0017");
		Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator(string source)
	{
		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, _) = RunGeneratorWithCompilation(source);
		return (diagnostics, generatedSources);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources, ImmutableArray<Diagnostic> CompilationErrors) RunGeneratorWithCompilation(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "NotificationsDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new NotificationsGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];
		// Errors in the emitted attribute sources are unrelated to the notifications generator and are ignored here.
		ImmutableArray<Diagnostic> compilationErrors = [.. outputCompilation.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.Where(d => d.Location.SourceTree?.FilePath.Contains(nameof(AttributeSourceGenerator)) != true)];

		return (diagnostics, generatedSources, compilationErrors);
	}
}
