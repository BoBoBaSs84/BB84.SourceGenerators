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
/// Runtime generator-driver tests for <see cref="ToStringGenerator"/>. These run the generator
/// in-process (so the format/order/collection/null-placeholder branches are instrumented and their
/// emitted source asserted), complementing <c>ToStringGeneratorTests</c> which executes compiled models.
/// </summary>
[TestClass]
public sealed class ToStringGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ShouldGenerateToStringOverrideWithStringBuilder()
	{
		string src = Generate(@"
	[GenerateToString]
	public partial class BasicModel { public int Id { get; set; } public string Name { get; set; } }", "partial class BasicModel");

		Assert.Contains("public override string ToString()", src);
		Assert.Contains("System.Text.StringBuilder sb = new();", src);
		Assert.Contains("sb.Append(\"BasicModel { \");", src);
		Assert.Contains("sb.Append($\"{nameof(Id)} = \");", src);
		Assert.Contains("return sb.ToString();", src);
	}

	[TestMethod]
	public void EmptyClassShouldReturnLiteral()
	{
		string src = Generate(@"
	[GenerateToString]
	public partial class EmptyModel { }", "partial class EmptyModel");

		Assert.Contains("return \"EmptyModel { }\";", src);
	}

	[TestMethod]
	public void FormattableShouldImplementIFormattable()
	{
		string src = Generate(@"
	[GenerateToString(Formattable = true)]
	public partial class FormattableModel { public int Value { get; set; } }", "partial class FormattableModel");

		Assert.Contains("partial class FormattableModel : IFormattable", src);
		Assert.Contains("public string ToString(string? format, IFormatProvider? formatProvider)", src);
		Assert.Contains("=> ToString(null, null);", src);
		Assert.Contains("Value.ToString(format, formatProvider)", src);
	}

	[TestMethod]
	public void CustomSeparatorShouldBeEmittedBetweenProperties()
	{
		string src = Generate(@"
	[GenerateToString(Separator = "" | "")]
	public partial class SeparatorModel { public int A { get; set; } public int B { get; set; } }", "partial class SeparatorModel");

		Assert.Contains("sb.Append(\" | \");", src);
	}

	[TestMethod]
	public void NullPlaceholderShouldGuardNullableProperty()
	{
		string src = Generate(@"
	[GenerateToString(NullPlaceholder = ""N/A"")]
	public partial class NullModel { public string? Name { get; set; } }", "partial class NullModel");

		Assert.Contains("(Name?.ToString() ?? \"N/A\")", src);
	}

	[TestMethod]
	public void FormatAttributeShouldEmitFormattedToString()
	{
		string src = Generate(@"
	[GenerateToString]
	public partial class FormatModel { [ToStringFormat(""X"")] public int Code { get; set; } }", "partial class FormatModel");

		Assert.Contains("Code.ToString(\"X\")", src);
	}

	[TestMethod]
	public void OrderAttributeShouldReorderProperties()
	{
		string src = Generate(@"
	[GenerateToString]
	public partial class OrderModel
	{
		public int First { get; set; }
		[ToStringOrder(0)] public int Second { get; set; }
	}", "partial class OrderModel");

		int second = src.IndexOf("nameof(Second)", StringComparison.Ordinal);
		int first = src.IndexOf("nameof(First)", StringComparison.Ordinal);
		Assert.IsTrue(second >= 0 && first > second, $"Expected Second before First, got {second}/{first}");
	}

	[TestMethod]
	public void CollectionCountFormatShouldEmitCountExpression()
	{
		string src = Generate(@"
	[GenerateToString]
	public partial class CountModel { public System.Collections.Generic.List<int> Items { get; set; } }", "partial class CountModel");

		Assert.Contains("\"Count = \" + Items.Count", src);
	}

	[TestMethod]
	public void CollectionElementsFormatWithDictionaryShouldEmitHelper()
	{
		string src = Generate(@"
	[GenerateToString(CollectionFormat = CollectionFormat.Elements)]
	public partial class DictModel { public System.Collections.Generic.Dictionary<string, int> Map { get; set; } }", "partial class DictModel");

		Assert.Contains("DictionaryToString(Map)", src);
		Assert.Contains("private static string DictionaryToString", src);
	}

	[TestMethod]
	public void CollectionTypeAndCountFormatShouldEmitTypeName()
	{
		string src = Generate(@"
	[GenerateToString(CollectionFormat = CollectionFormat.TypeAndCount)]
	public partial class TypeCountModel { public System.Collections.Generic.List<int> Items { get; set; } }", "partial class TypeCountModel");

		Assert.Contains("Items.GetType().Name", src);
	}

	[TestMethod]
	public void ConstructorExcludedPropertiesShouldNotBeEmitted()
	{
		string src = Generate(@"
	[GenerateToString(""Secret"", nameof(ExcludeModel.Internal))]
	public partial class ExcludeModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Secret { get; set; }
		public string Internal { get; set; }
	}", "partial class ExcludeModel");

		Assert.Contains("nameof(Id)", src);
		Assert.Contains("nameof(Name)", src);
		Assert.DoesNotContain("Secret", src);
		Assert.DoesNotContain("Internal", src);
	}

	[TestMethod]
	public void IncludeInheritedShouldAppendBaseProperties()
	{
		string src = Generate(@"
	public class BaseModel { public int BaseProp { get; set; } }

	[GenerateToString(IncludeInherited = true)]
	public partial class DerivedModel : BaseModel { public int Own { get; set; } }", "partial class DerivedModel");

		Assert.Contains("nameof(BaseProp)", src);
		Assert.Contains("nameof(Own)", src);
	}

	[TestMethod]
	public void RecordShouldOverridePrintMembers()
	{
		string src = Generate(@"
	[GenerateToString]
	public partial record RecordModel { public int Id { get; set; } public string Name { get; set; } }", "partial record RecordModel");

		Assert.Contains("partial record RecordModel", src);
		Assert.Contains("protected virtual bool PrintMembers(System.Text.StringBuilder builder)", src);
		Assert.Contains("bool appended = false;", src);
		Assert.Contains("builder.Append($\"{nameof(Id)} = \");", src);
		Assert.DoesNotContain("public override string ToString()", src);
	}

	[TestMethod]
	public void PositionalRecordShouldOverridePrintMembers()
	{
		string src = Generate(@"
	[GenerateToString]
	public partial record PositionalRecordModel(int Id, string Name);", "partial record PositionalRecordModel");

		Assert.Contains("nameof(Id)", src);
		Assert.Contains("nameof(Name)", src);
		Assert.Contains("PrintMembers", src);
	}

	[TestMethod]
	public void FormattableRecordShouldOverrideToStringDirectly()
	{
		string src = Generate(@"
	[GenerateToString(Formattable = true)]
	public partial record FormattableRecordModel { public int Value { get; set; } }", "partial record FormattableRecordModel");

		Assert.Contains("partial record FormattableRecordModel : IFormattable", src);
		Assert.Contains("public override string ToString()", src);
		Assert.DoesNotContain("PrintMembers", src);
	}

	[TestMethod]
	public void DerivedRecordPrintMembersShouldOverrideAndCallBase()
	{
		string src = Generate(@"
	[GenerateToString]
	public partial record BaseRecordModel { public int Id { get; set; } }

	[GenerateToString]
	public partial record DerivedRecordModel : BaseRecordModel { public string Name { get; set; } }", "partial record DerivedRecordModel");

		Assert.Contains("protected override bool PrintMembers(System.Text.StringBuilder builder)", src);
		Assert.Contains("bool appended = base.PrintMembers(builder);", src);
	}

	[TestMethod]
	public void StructShouldOverrideToString()
	{
		string src = Generate(@"
	[GenerateToString]
	public partial struct StructModel { public int Id { get; set; } public string Name { get; set; } }", "partial struct StructModel");

		Assert.Contains("public partial struct StructModel", src);
		Assert.Contains("public override string ToString()", src);
		Assert.Contains("nameof(Id)", src);
		Assert.Contains("nameof(Name)", src);
		Assert.DoesNotContain("PrintMembers", src);
	}

	[TestMethod]
	public void FormattableStructShouldImplementIFormattable()
	{
		string src = Generate(@"
	[GenerateToString(Formattable = true)]
	public partial struct FormattableStructModel { public decimal Total { get; set; } }", "partial struct FormattableStructModel");

		Assert.Contains("public partial struct FormattableStructModel : IFormattable", src);
		Assert.Contains("public string ToString(string? format, IFormatProvider? formatProvider)", src);
	}

	[TestMethod]
	public void NestedStructShouldBeEmittedInsideOuterClass()
	{
		string src = Generate(@"
	public partial class Outer
	{
		[GenerateToString]
		internal partial struct Inner { public int Id { get; set; } }
	}", "partial struct Inner");

		Assert.Contains("public partial class Outer", src);
		Assert.Contains("internal partial struct Inner", src);
	}

	[TestMethod]
	[DataRow("[GenerateToString] public partial class KeywordModel { public List<int> Class { get; set; } }")]
	[DataRow("[GenerateToString] public partial class LocalModel { public List<int> Sb { get; set; } }")]
	[DataRow("[GenerateToString] public partial class CaseModel { public List<int> Items { get; set; } public List<int> ITEMS { get; set; } }")]
	[DataRow("[GenerateToString] public partial class NameofModel { public List<int> Nameof { get; set; } }")]
	[DataRow("[GenerateToString] public partial class ShadowModel { public List<int> Items { get; set; } public int items { get; set; } }")]
	[DataRow("[GenerateToString(Formattable = true)] public partial class FormatModel { public List<int> Format { get; set; } public int Value { get; set; } }")]
	[DataRow("[GenerateToString] public partial record BuilderModel { public List<int> Builder { get; set; } public List<int> Appended { get; set; } }")]
	public void CollectionLocalsShouldCompileForConflictingPropertyNames(string declaration)
	{
		ImmutableArray<Diagnostic> errors = CompileGenerated($"using System.Collections.Generic;\n{declaration}");

		Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
	}

	[TestMethod]
	public void CollectionLocalsShouldEscapeKeywordsAndAvoidCollisions()
	{
		string src = Generate(@"
	[GenerateToString]
	public partial class ConflictModel { public System.Collections.Generic.List<int> Class { get; set; } public System.Collections.Generic.List<int> Sb { get; set; } public System.Collections.Generic.List<int> MyItems { get; set; } }", "partial class ConflictModel");

		Assert.Contains("string @class = ", src);
		Assert.Contains("string sb1 = ", src);
		Assert.Contains("string myItems = ", src);
		Assert.Contains("sb.Append(@class);", src);
		Assert.Contains("sb.Append(sb1);", src);
	}

	[TestMethod]
	[DataRow("[BB84.SourceGenerators.Attributes.GenerateToString(\"Secret\", Formattable = true, Separator = \" | \")]")]
	[DataRow("[global::BB84.SourceGenerators.Attributes.GenerateToStringAttribute(\"Secret\", Formattable = true, Separator = \" | \")]")]
	[DataRow("[ToStringAlias(\"Secret\", Formattable = true, Separator = \" | \")]")]
	public void OptionsShouldBeReadForQualifiedAndAliasedAttributes(string attribute)
	{
		string src = Generate($@"
	using ToStringAlias = BB84.SourceGenerators.Attributes.GenerateToStringAttribute;
	{attribute}
	public partial class QualifiedModel {{ public int Id {{ get; set; }} public int Name {{ get; set; }} public int Secret {{ get; set; }} }}", "partial class QualifiedModel");

		Assert.Contains("partial class QualifiedModel : IFormattable", src);
		Assert.Contains("sb.Append(\" | \");", src);
		Assert.DoesNotContain("nameof(Secret)", src);
	}

	[TestMethod]
	public void NamedStringArgumentsShouldNotExcludeProperties()
	{
		string src = Generate(@"
	[GenerateToString(Separator = ""Name"", NullPlaceholder = ""Id"")]
	public partial class NamedArgumentModel { public int Id { get; set; } public string Name { get; set; } }", "partial class NamedArgumentModel");

		Assert.Contains("nameof(Id)", src);
		Assert.Contains("nameof(Name)", src);
	}

	private static ImmutableArray<Diagnostic> CompileGenerated(string members)
	{
		string source = $@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{{{members}
}}";
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "ToStringDriverTestAssembly",
			syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		GeneratorDriver driver = CSharpGeneratorDriver.Create(new AttributeSourceGenerator(), new ToStringGenerator());
		_ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out _);

		// Only the generated ToString sources matter; the injected attribute sources need references
		// (e.g. ExcludeFromCodeCoverageAttribute) that are not available on every test target.
		return [.. output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error
			&& d.Location.SourceTree?.FilePath.EndsWith(".ToString.g.cs", StringComparison.Ordinal) == true)];
	}

	private static string Generate(string members, string marker)
	{
		string source = $@"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{{{members}
}}";
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "ToStringDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new ToStringGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		return runResult.GeneratedTrees
			.Select(t => t.GetText().ToString())
			.First(s => s.Contains(marker));
	}
}
