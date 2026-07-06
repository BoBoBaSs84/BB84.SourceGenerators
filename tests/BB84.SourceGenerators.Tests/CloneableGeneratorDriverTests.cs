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
/// Runtime generator-driver tests for <see cref="CloneableGenerator"/>. These run the generator
/// in-process (so the deep-clone branch matrix is instrumented and its emitted source asserted),
/// complementing <c>CloneableGeneratorTests</c> which executes pre-compiled models.
/// </summary>
[TestClass]
public sealed class CloneableGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	// A reusable [GenerateCloneable] element type so collection elements are detected as deep-cloneable.
	private const string ItemType = @"
	[GenerateCloneable]
	public partial class Item { public int Value { get; set; } }";

	[TestMethod]
	public void ShouldGenerateCloneDeepCloneAndICloneableForClass()
	{
		string src = Generate(@"
	[GenerateCloneable]
	public partial class BasicModel { public int Id { get; set; } }", "partial class BasicModel");

		Assert.Contains("partial class BasicModel : ICloneable", src);
		Assert.Contains("public BasicModel Clone()", src);
		Assert.Contains("BasicModel clone = new BasicModel();", src);
		Assert.Contains("clone.Id = this.Id;", src);
		Assert.Contains("public BasicModel DeepClone()", src);
		Assert.Contains("object ICloneable.Clone()", src);
		Assert.Contains("=> DeepClone();", src);
	}

	[TestMethod]
	public void ShouldGenerateStructCloneUsingValueCopy()
	{
		string src = Generate(@"
	[GenerateCloneable]
	public partial struct Point { public int X { get; set; } }", "partial struct Point");

		Assert.Contains("partial struct Point : ICloneable", src);
		Assert.Contains("Point clone = this;", src);
		Assert.Contains("public Point DeepClone()", src);
	}

	[TestMethod]
	public void ListOfCloneableElementsShouldDeepCloneEach()
	{
		string src = Generate(ItemType + @"
	[GenerateCloneable]
	public partial class Bag { public System.Collections.Generic.List<Item> Items { get; set; } }", "partial class Bag");

		Assert.Contains("clone.Items = this.Items.Select(x => x.DeepClone()).ToList();", src);
		Assert.Contains("using System.Linq;", src);
		Assert.Contains("using System.Collections.Generic;", src);
	}

	[TestMethod]
	public void NullableListOfNonCloneableElementsShouldCopyWithNullGuard()
	{
		string src = Generate(@"
	[GenerateCloneable]
	public partial class NumberBag { public System.Collections.Generic.List<int>? Numbers { get; set; } }", "partial class NumberBag");

		Assert.Contains("clone.Numbers = this.Numbers is not null ? new List<int>(this.Numbers) : null;", src);
	}

	[TestMethod]
	public void DictionaryWithCloneableValuesShouldDeepCloneValues()
	{
		string src = Generate(ItemType + @"
	[GenerateCloneable]
	public partial class Registry { public System.Collections.Generic.Dictionary<string, Item> Map { get; set; } }", "partial class Registry");

		Assert.Contains("clone.Map = this.Map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepClone());", src);
	}

	[TestMethod]
	public void ArrayOfCloneableElementsShouldDeepCloneToArray()
	{
		string src = Generate(ItemType + @"
	[GenerateCloneable]
	public partial class ItemArray { public Item[] Items { get; set; } }", "partial class ItemArray");

		Assert.Contains("clone.Items = this.Items.Select(x => x.DeepClone()).ToArray();", src);
	}

	[TestMethod]
	public void ArrayOfNonCloneableElementsShouldUseArrayClone()
	{
		string src = Generate(@"
	[GenerateCloneable]
	public partial class IntArray { public int[] Data { get; set; } }", "partial class IntArray");

		Assert.Contains("clone.Data = (int[])this.Data.Clone();", src);
	}

	[TestMethod]
	public void ImmutableArrayOfNonCloneableElementsShouldShallowCopy()
	{
		string src = Generate(@"
	[GenerateCloneable]
	public partial class ImmHolder { public System.Collections.Immutable.ImmutableArray<int> Values { get; set; } }", "partial class ImmHolder");

		Assert.Contains("clone.Values = this.Values;", src);
	}

	[TestMethod]
	public void ReadOnlyCollectionOfCloneableElementsShouldRebuildAsReadOnly()
	{
		string src = Generate(ItemType + @"
	[GenerateCloneable]
	public partial class RoHolder { public System.Collections.Generic.IReadOnlyList<Item> Items { get; set; } }", "partial class RoHolder");

		Assert.Contains("clone.Items = this.Items.Select(x => x.DeepClone()).ToList().AsReadOnly();", src);
	}

	[TestMethod]
	public void NestedCloneablePropertyShouldDeepCloneWithNullConditional()
	{
		string src = Generate(ItemType + @"
	[GenerateCloneable]
	public partial class Parent { public Item Child { get; set; } }", "partial class Parent");

		Assert.Contains("clone.Child = this.Child?.DeepClone();", src);
	}

	[TestMethod]
	public void ExcludedPropertyShouldNotBeCloned()
	{
		string src = Generate(@"
	[GenerateCloneable(""Secret"")]
	public partial class Secured { public int Id { get; set; } public int Secret { get; set; } }", "partial class Secured");

		Assert.Contains("clone.Id = this.Id;", src);
		Assert.DoesNotContain("clone.Secret", src);
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
			assemblyName: "CloneableDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new CloneableGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> diagnostics);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		return runResult.GeneratedTrees
			.Select(t => t.GetText().ToString())
			.First(s => s.Contains(marker));
	}
}
