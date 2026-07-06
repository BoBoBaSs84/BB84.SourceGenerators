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
	public void IncludeInheritedShouldAppendBaseProperties()
	{
		string src = Generate(@"
	public class BaseModel { public int BaseProp { get; set; } }

	[GenerateToString(IncludeInherited = true)]
	public partial class DerivedModel : BaseModel { public int Own { get; set; } }", "partial class DerivedModel");

		Assert.Contains("nameof(BaseProp)", src);
		Assert.Contains("nameof(Own)", src);
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
