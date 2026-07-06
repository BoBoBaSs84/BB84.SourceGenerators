// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;
using System.Reflection;

using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators.Tests.Helpers;

[TestClass]
public sealed class GeneratorHelpersTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	#region Pure helpers

	[TestMethod]
	public void StripAttributeSuffixShouldRemoveSuffixWhenPresent()
		=> Assert.AreEqual("GenerateToString", GeneratorHelpers.StripAttributeSuffix("GenerateToStringAttribute"));

	[TestMethod]
	public void StripAttributeSuffixShouldReturnInputWhenNoSuffix()
		=> Assert.AreEqual("GenerateToString", GeneratorHelpers.StripAttributeSuffix("GenerateToString"));

	[TestMethod]
	public void GetAccessibilityKeywordShouldMapEveryAccessibility()
	{
		Assert.AreEqual("public", GeneratorHelpers.GetAccessibilityKeyword(Accessibility.Public));
		Assert.AreEqual("internal", GeneratorHelpers.GetAccessibilityKeyword(Accessibility.Internal));
		Assert.AreEqual("protected", GeneratorHelpers.GetAccessibilityKeyword(Accessibility.Protected));
		Assert.AreEqual("protected internal", GeneratorHelpers.GetAccessibilityKeyword(Accessibility.ProtectedOrInternal));
		Assert.AreEqual("private protected", GeneratorHelpers.GetAccessibilityKeyword(Accessibility.ProtectedAndInternal));
		Assert.AreEqual("private", GeneratorHelpers.GetAccessibilityKeyword(Accessibility.Private));
		Assert.AreEqual("public", GeneratorHelpers.GetAccessibilityKeyword(Accessibility.NotApplicable));
	}

	[TestMethod]
	public void EscapeStringShouldEscapeBackslashesAndQuotes()
		=> Assert.AreEqual("a\\\\b\\\"c", GeneratorHelpers.EscapeString("a\\b\"c"));

	[TestMethod]
	public void EscapeVerbatimStringShouldDoubleQuotesOnly()
		=> Assert.AreEqual("a\\b\"\"c", GeneratorHelpers.EscapeVerbatimString("a\\b\"c"));

	[TestMethod]
	public void GetAttributeNamesShouldReturnMetadataFullAndShortNames()
	{
		(string metadataName, string fullName, string shortName) = GeneratorHelpers.GetAttributeNames<SampleWidgetAttribute>();

		Assert.AreEqual("BB84.SourceGenerators.Tests.Helpers.SampleWidgetAttribute", metadataName);
		Assert.AreEqual("SampleWidgetAttribute", fullName);
		Assert.AreEqual("SampleWidget", shortName);
	}

	#endregion

	#region Accessibility / nesting / namespace

	[TestMethod]
	public void GetAccessibilityShouldReadDeclaredModifierAndDefaultToInternal()
	{
		const string source = @"
public class PublicType { }
internal class InternalType { }
class BareType { }";
		CSharpCompilation compilation = Compile(source);

		Assert.AreEqual("public", GeneratorHelpers.GetAccessibility(FindClass(compilation, "PublicType").Syntax));
		Assert.AreEqual("internal", GeneratorHelpers.GetAccessibility(FindClass(compilation, "InternalType").Syntax));
		Assert.AreEqual("internal", GeneratorHelpers.GetAccessibility(FindClass(compilation, "BareType").Syntax));
	}

	[TestMethod]
	public void GetOuterClassesShouldReturnNestingChainFromOutermostToInnermost()
	{
		const string source = @"
public class Outer
{
	internal class Middle
	{
		public class Inner { }
	}
}";
		CSharpCompilation compilation = Compile(source);

		List<(string Accessibility, string Name)> outer = GeneratorHelpers.GetOuterClasses(FindClass(compilation, "Inner").Syntax);

		Assert.HasCount(2, outer);
		Assert.AreEqual(("public", "Outer"), outer[0]);
		Assert.AreEqual(("internal", "Middle"), outer[1]);
	}

	[TestMethod]
	public void GetOuterClassesShouldBeEmptyForTopLevelType()
	{
		CSharpCompilation compilation = Compile("public class TopLevel { }");

		Assert.IsEmpty(GeneratorHelpers.GetOuterClasses(FindClass(compilation, "TopLevel").Syntax));
	}

	#endregion

	#region Attribute argument extraction

	[TestMethod]
	public void GetExcludedPropertiesShouldCollectStringConstructorArguments()
	{
		const string source = @"
using System;

internal sealed class ExcludeSampleAttribute : Attribute
{
	public ExcludeSampleAttribute(params string[] names) { }
}

[ExcludeSample(""Secret"", ""Internal"")]
public class Target { }";
		CSharpCompilation compilation = Compile(source);
		(_, ClassDeclarationSyntax syntax, SemanticModel model) = FindClass(compilation, "Target");

		HashSet<string> excluded = GeneratorHelpers.GetExcludedProperties(syntax, model, "ExcludeSampleAttribute");

		Assert.HasCount(2, excluded);
		Assert.Contains("Secret", excluded);
		Assert.Contains("Internal", excluded);
	}

	[TestMethod]
	public void GetNamedArgumentValueShouldReadNamedPropertyArgument()
	{
		const string source = @"
using System;

internal sealed class ConfigAttribute : Attribute
{
	public ConfigAttribute(string mode = """") { }
	public string? Level { get; set; }
}

[Config(""fast"", Level = ""High"")]
public class Target { }";
		CSharpCompilation compilation = Compile(source);
		(_, ClassDeclarationSyntax syntax, SemanticModel model) = FindClass(compilation, "Target");

		string value = GeneratorHelpers.GetNamedArgumentValue(syntax, model, "Config", "ConfigAttribute", "Level", "default");

		Assert.AreEqual("High", value);
	}

	[TestMethod]
	public void GetNamedArgumentValueShouldReturnDefaultWhenAttributeMissing()
	{
		CSharpCompilation compilation = Compile("public class Target { }");
		(_, ClassDeclarationSyntax syntax, SemanticModel model) = FindClass(compilation, "Target");

		string value = GeneratorHelpers.GetNamedArgumentValue(syntax, model, "Config", "ConfigAttribute", "Level", "fallback");

		Assert.AreEqual("fallback", value);
	}

	[TestMethod]
	public void GetConstructorArgumentValueShouldReadPositionalArgumentBySymbol()
	{
		const string source = @"
using System;

internal sealed class ConfigAttribute : Attribute
{
	public ConfigAttribute(bool enabled) { }
}

[Config(true)]
public class Target { }";
		CSharpCompilation compilation = Compile(source);
		INamedTypeSymbol symbol = FindClass(compilation, "Target").Symbol;

		Assert.IsTrue(GeneratorHelpers.GetConstructorArgumentValue(symbol, "ConfigAttribute", 0, false));
		Assert.IsFalse(GeneratorHelpers.GetConstructorArgumentValue(symbol, "ConfigAttribute", 5, false));
	}

	#endregion

	#region Context creation

	[TestMethod]
	public void TryCreateContextShouldReturnFalseForNullInput()
	{
		Assert.IsFalse(GeneratorHelpers.TryCreateContext(null, out GeneratorContext context));
		Assert.IsNull(context);
	}

	[TestMethod]
	public void TryCreateContextShouldPopulateNameNamespaceAndAccessibility()
	{
		const string source = @"
namespace My.Space
{
	internal class Model { }
}";
		CSharpCompilation compilation = Compile(source);
		(_, ClassDeclarationSyntax syntax, SemanticModel model) = FindClass(compilation, "Model");

		bool created = GeneratorHelpers.TryCreateContext((syntax, model), out GeneratorContext context);

		Assert.IsTrue(created);
		Assert.AreEqual("Model", context.ClassName);
		Assert.AreEqual("My.Space", context.NamespaceName);
		Assert.AreEqual("internal", context.Accessibility);
	}

	#endregion

	#region Property discovery

	[TestMethod]
	public void GetPublicPropertySymbolsShouldFilterStaticIndexerAndNonPublicMembers()
	{
		CSharpCompilation compilation = Compile(PropertyModelSource);
		INamedTypeSymbol symbol = FindClass(compilation, "DerivedModel").Symbol;

		List<string> names = [.. GeneratorHelpers.GetPublicPropertySymbols(symbol).Select(p => p.Name)];

		Assert.Contains("ReadWrite", names);
		Assert.Contains("ReadOnlyProp", names);
		Assert.Contains("PrivateSet", names);
		Assert.DoesNotContain("StaticProp", names);
		Assert.DoesNotContain("Hidden", names);
		Assert.DoesNotContain("BaseProp", names);
	}

	[TestMethod]
	public void GetPublicPropertySymbolsShouldHonorRequireSetter()
	{
		CSharpCompilation compilation = Compile(PropertyModelSource);
		INamedTypeSymbol symbol = FindClass(compilation, "DerivedModel").Symbol;

		List<string> names = [.. GeneratorHelpers
			.GetPublicPropertySymbols(symbol, requireSetter: true)
			.Select(p => p.Name)];

		Assert.Contains("ReadWrite", names);
		Assert.DoesNotContain("ReadOnlyProp", names);
		Assert.DoesNotContain("PrivateSet", names);
	}

	[TestMethod]
	public void GetPublicPropertySymbolsShouldIncludeInheritedWhenRequested()
	{
		CSharpCompilation compilation = Compile(PropertyModelSource);
		INamedTypeSymbol symbol = FindClass(compilation, "DerivedModel").Symbol;

		List<string> names = [.. GeneratorHelpers
			.GetPublicPropertySymbols(symbol, includeInherited: true)
			.Select(p => p.Name)];

		Assert.Contains("ReadWrite", names);
		Assert.Contains("BaseProp", names);
	}

	[TestMethod]
	public void GetPropertyDescriptorsShouldHonorExcludedProperties()
	{
		CSharpCompilation compilation = Compile(PropertyModelSource);
		INamedTypeSymbol symbol = FindClass(compilation, "DerivedModel").Symbol;

		ImmutableArray<PropertyDescriptor> descriptors =
			GeneratorHelpers.GetPropertyDescriptors(symbol, excludedProperties: ["ReadOnlyProp"]);
		List<string> names = [.. descriptors.Select(d => d.Name)];

		Assert.Contains("ReadWrite", names);
		Assert.DoesNotContain("ReadOnlyProp", names);
	}

	[TestMethod]
	public void GetPropertyDescriptorsShouldClassifyCollectionKinds()
	{
		CSharpCompilation compilation = Compile(CollectionModelSource);
		INamedTypeSymbol symbol = FindClass(compilation, "CollectionModel").Symbol;

		ImmutableArray<PropertyDescriptor> descriptors =
			GeneratorHelpers.GetPropertyDescriptors(symbol, detectCollections: true);

		Assert.AreEqual(CollectionKind.Array, Find(descriptors, "Arr").CollectionKind);
		Assert.AreEqual(CollectionKind.List, Find(descriptors, "Lst").CollectionKind);
		Assert.AreEqual(CollectionKind.Dictionary, Find(descriptors, "Map").CollectionKind);
		Assert.AreEqual(CollectionKind.ImmutableArray, Find(descriptors, "Imm").CollectionKind);
		Assert.AreEqual(CollectionKind.ReadOnlyCollection, Find(descriptors, "Ro").CollectionKind);
		Assert.AreEqual(CollectionKind.None, Find(descriptors, "Scalar").CollectionKind);
	}

	#endregion

	#region Helpers

	private const string PropertyModelSource = @"
public class BaseModel
{
	public int BaseProp { get; set; }
}

public sealed class DerivedModel : BaseModel
{
	public int ReadWrite { get; set; }
	public int ReadOnlyProp { get; }
	public int PrivateSet { get; private set; }
	public static int StaticProp { get; set; }
	private int Hidden { get; set; }
	public int this[int i] => i;
}";

	private const string CollectionModelSource = @"
using System.Collections.Generic;
using System.Collections.Immutable;

public sealed class CollectionModel
{
	public int[] Arr { get; set; }
	public List<string> Lst { get; set; }
	public Dictionary<string, int> Map { get; set; }
	public ImmutableArray<int> Imm { get; set; }
	public IReadOnlyList<int> Ro { get; set; }
	public int Scalar { get; set; }
}";

	private static PropertyDescriptor Find(ImmutableArray<PropertyDescriptor> descriptors, string name)
		=> descriptors.Single(d => d.Name == name);

	private static CSharpCompilation Compile(string source)
		=> CSharpCompilation.Create(
			assemblyName: "GeneratorHelpersTestAssembly",
			syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

	private static (INamedTypeSymbol Symbol, ClassDeclarationSyntax Syntax, SemanticModel Model) FindClass(
		CSharpCompilation compilation, string className)
	{
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		SemanticModel model = compilation.GetSemanticModel(tree);
		ClassDeclarationSyntax syntax = tree.GetRoot()
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.First(c => c.Identifier.Text == className);
		INamedTypeSymbol symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(syntax)!;
		return (symbol, syntax, model);
	}

	#endregion
}

/// <summary>A local attribute used to exercise <c>GetAttributeNames</c> without touching injected attribute types.</summary>
#pragma warning disable CA1018 // Mark attributes with AttributeUsageAttribute
internal sealed class SampleWidgetAttribute : Attribute { }
#pragma warning restore CA1018 // Mark attributes with AttributeUsageAttribute
