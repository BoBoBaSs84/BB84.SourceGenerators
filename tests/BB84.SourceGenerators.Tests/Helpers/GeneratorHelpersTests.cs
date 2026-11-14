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

	// NOTE: The helpers guarded by "#if !NETFRAMEWORK" below return or accept System.ValueTuple.
	// Invoking them across the netstandard2.0 -> net4x assembly boundary throws MissingMethodException
	// under Mono (the Linux CI host for the .NET Framework targets), whose ValueTuple identity does not
	// unify with the generator assembly's. The logic is runtime-independent and fully covered by the
	// modern-.NET run, so these cases are excluded on .NET Framework.
#if !NETFRAMEWORK
	[TestMethod]
	public void GetAttributeNamesShouldReturnMetadataFullAndShortNames()
	{
		(string metadataName, string fullName, string shortName) = GeneratorHelpers.GetAttributeNames<SampleWidgetAttribute>();

		Assert.AreEqual("BB84.SourceGenerators.Tests.Helpers.SampleWidgetAttribute", metadataName);
		Assert.AreEqual("SampleWidgetAttribute", fullName);
		Assert.AreEqual("SampleWidget", shortName);
	}
#endif

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

#if !NETFRAMEWORK // See the ValueTuple/Mono note above.
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
#endif

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

#if !NETFRAMEWORK // See the ValueTuple/Mono note above.
	[TestMethod]
	public void TypeTargetInfoCreateShouldPopulateNameNamespaceAndAccessibility()
	{
		const string source = @"
namespace My.Space
{
	internal class Model { }
}";
		CSharpCompilation compilation = Compile(source);
		(_, ClassDeclarationSyntax syntax, SemanticModel model) = FindClass(compilation, "Model");

		TypeTargetInfo type = TypeTargetInfo.Create(syntax, model.GetDeclaredSymbol(syntax)!);

		Assert.AreEqual("Model", type.Name);
		Assert.AreEqual("My.Space", type.NamespaceName);
		Assert.AreEqual("internal", type.Accessibility);
		Assert.IsEmpty(type.OuterClasses);
		Assert.IsNotNull(type.IdentifierLocation);
	}

	[TestMethod]
	public void TypeTargetInfoCreateShouldCaptureOuterClassesAndCompareByValue()
	{
		const string source = @"
namespace My.Space
{
	public partial class Outer
	{
		internal partial class Model { }
	}
}";
		CSharpCompilation compilation = Compile(source);
		(_, ClassDeclarationSyntax syntax, SemanticModel model) = FindClass(compilation, "Model");
		INamedTypeSymbol symbol = model.GetDeclaredSymbol(syntax)!;

		TypeTargetInfo first = TypeTargetInfo.Create(syntax, symbol);
		TypeTargetInfo second = TypeTargetInfo.Create(syntax, symbol);

		Assert.HasCount(1, first.OuterClasses);
		Assert.AreEqual(("public", "Outer"), first.OuterClasses[0]);
		Assert.AreEqual(first, second);
		Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
	}
#endif

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

	#region Shared generator helpers

	[TestMethod]
	[DataRow("_name", "Name")]
	[DataRow("name", "Name")]
	[DataRow("m_count", "M_count")]
	[DataRow("_", "")]
	public void FieldNameToPropertyNameShouldStripUnderscoreAndCapitalize(string fieldName, string expected)
		=> Assert.AreEqual(expected, GeneratorHelpers.FieldNameToPropertyName(fieldName));

	[TestMethod]
	public void PropertyNameToFieldNameShouldPrefixUnderscoreAndLowercase()
		=> Assert.AreEqual("_firstName", GeneratorHelpers.PropertyNameToFieldName("FirstName"));

	[TestMethod]
	public void BuildHintNameWithoutOuterClassesShouldAppendOptionalFeature()
	{
		Assert.AreEqual("Point.g.cs", GeneratorHelpers.BuildHintName("Point"));
		Assert.AreEqual("Point.Cloneable.g.cs", GeneratorHelpers.BuildHintName("Point", "Cloneable"));
	}

	[TestMethod]
	public void FindAttributesShouldMatchFullAndShortNamesInDeclarationOrder()
	{
		const string source = @"
[Widget(1), Other, WidgetAttribute(2)]
[Widget(3)]
public class Target { }";
		ClassDeclarationSyntax syntax = CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.CancellationToken)
			.GetRoot(TestContext.CancellationToken)
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.Single();

		string[] matches = [.. GeneratorHelpers.FindAttributes(syntax.AttributeLists, "WidgetAttribute", "Widget")
			.Select(static attribute => attribute.ArgumentList!.Arguments[0].ToString())];

		string[] expected = ["1", "2", "3"];
		Assert.AreSequenceEqual(expected, matches);
	}

	[TestMethod]
	public void GetRefKindModifierShouldOnlyEmitRequestedModifiers()
	{
		const string source = @"
public class Target
{
	public void M(int plain, ref int r, out int o, in int i, params int[] p) { o = 0; }
}";
		CSharpCompilation compilation = Compile(source);
		IMethodSymbol method = FindClass(compilation, "Target").Symbol.GetMembers("M").OfType<IMethodSymbol>().Single();

		string[] minimal = [.. method.Parameters.Select(static p => GeneratorHelpers.GetRefKindModifier(p, includeIn: false, includeParams: false, includeRefReadOnly: false))];
		string[] full = [.. method.Parameters.Select(static p => GeneratorHelpers.GetRefKindModifier(p, includeIn: true, includeParams: true, includeRefReadOnly: true))];

		string[] expectedMinimal = ["", "ref ", "out ", "", ""];
		string[] expectedFull = ["", "ref ", "out ", "in ", "params "];
		Assert.AreSequenceEqual(expectedMinimal, minimal);
		Assert.AreSequenceEqual(expectedFull, full);
	}

	[TestMethod]
	[DataRow("Token", "TOKEN")]
	[DataRow("ConnectTimeoutMs", "CONNECT_TIMEOUT_MS")]
	[DataRow("HTTPPort", "HTTP_PORT")]
	[DataRow("MaxRetries2Count", "MAX_RETRIES2_COUNT")]
	[DataRow("Already_Snake", "ALREADY_SNAKE")]
	[DataRow("lowerCamel", "LOWER_CAMEL")]
	public void ToUpperSnakeCaseShouldSplitWordsAndKeepAcronyms(string name, string expected)
		=> Assert.AreEqual(expected, GeneratorHelpers.ToUpperSnakeCase(name));

	[TestMethod]
	public void GetPropertyInitializerShouldReturnInitializerOrNull()
	{
		CSharpCompilation compilation = Compile("public class C { public int A { get; set; } = 42; public int B { get; set; } }");
		(INamedTypeSymbol symbol, _, _) = FindClass(compilation, "C");

		EqualsValueClauseSyntax? initializer = GeneratorHelpers.GetPropertyInitializer((IPropertySymbol)symbol.GetMembers("A").Single());

		Assert.AreEqual("42", initializer?.Value.ToString());
		Assert.IsNull(GeneratorHelpers.GetPropertyInitializer((IPropertySymbol)symbol.GetMembers("B").Single()));
	}

	[TestMethod]
	public void FormatLiteralShouldProduceTypedLiterals()
	{
		CSharpCompilation compilation = Compile("public enum E { A, B } public class C { public E Value { get; set; } }");
		(INamedTypeSymbol symbol, _, _) = FindClass(compilation, "C");
		ITypeSymbol enumType = ((IPropertySymbol)symbol.GetMembers("Value").Single()).Type;
		ITypeSymbol intType = compilation.GetSpecialType(SpecialType.System_Int32);

		Assert.AreEqual("null", GeneratorHelpers.FormatLiteral(null, intType));
		Assert.AreEqual("\"a\\\"b\"", GeneratorHelpers.FormatLiteral("a\"b", intType));
		Assert.AreEqual("'x'", GeneratorHelpers.FormatLiteral('x', intType));
		Assert.AreEqual("true", GeneratorHelpers.FormatLiteral(true, intType));
		Assert.AreEqual("1.5F", GeneratorHelpers.FormatLiteral(1.5f, intType));
		Assert.AreEqual("0.5D", GeneratorHelpers.FormatLiteral(0.5, intType));
		Assert.AreEqual("0.1M", GeneratorHelpers.FormatLiteral(0.1m, intType));
		Assert.AreEqual("5L", GeneratorHelpers.FormatLiteral(5L, intType));
		Assert.AreEqual("5UL", GeneratorHelpers.FormatLiteral(5UL, intType));
		Assert.AreEqual("5U", GeneratorHelpers.FormatLiteral(5U, intType));
		Assert.AreEqual("double.NaN", GeneratorHelpers.FormatLiteral(double.NaN, intType));
		Assert.AreEqual("(global::E)(1)", GeneratorHelpers.FormatLiteral(1, enumType));
	}

	[TestMethod]
	public void ValueConversionsShouldClassifySupportedTypes()
	{
		CSharpCompilation compilation = Compile(@"
using System;
[Flags] public enum F { A = 1 }
public enum E { A }
public class C
{
	public string S { get; set; }
	public int? I { get; set; }
	public double D { get; set; }
	public decimal M { get; set; }
	public TimeSpan T { get; set; }
	public Guid G { get; set; }
	public E Enum { get; set; }
	public F Flags { get; set; }
	public object O { get; set; }
}");
		(INamedTypeSymbol symbol, _, _) = FindClass(compilation, "C");

		ValueConversionKind? Kind(string name)
			=> ValueConversions.TryGetKind(((IPropertySymbol)symbol.GetMembers(name).Single()).Type, out ValueConversionKind kind) ? kind : null;

		Assert.AreEqual(ValueConversionKind.String, Kind("S"));
		Assert.AreEqual(ValueConversionKind.Integer, Kind("I"));
		Assert.AreEqual(ValueConversionKind.FloatingPoint, Kind("D"));
		Assert.AreEqual(ValueConversionKind.Decimal, Kind("M"));
		Assert.AreEqual(ValueConversionKind.TimeSpan, Kind("T"));
		Assert.AreEqual(ValueConversionKind.Guid, Kind("G"));
		Assert.AreEqual(ValueConversionKind.Enum, Kind("Enum"));
		Assert.AreEqual(ValueConversionKind.FlagsEnum, Kind("Flags"));
		Assert.IsNull(Kind("O"));
	}

	[TestMethod]
	public void ValueConversionsShouldBuildExpressions()
	{
		Assert.AreEqual("int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)", ValueConversions.GetTryParseExpression(ValueConversionKind.Integer, "int", "raw", "parsed"));
		Assert.AreEqual("(global::System.Enum.TryParse<global::E>(raw, true, out global::E parsed) && global::System.Enum.IsDefined(typeof(global::E), parsed))", ValueConversions.GetTryParseExpression(ValueConversionKind.Enum, "global::E", "raw", "parsed"));
		Assert.AreEqual("value.ToString(\"c\", CultureInfo.InvariantCulture)", ValueConversions.GetFormatExpression(ValueConversionKind.TimeSpan, "value"));
		Assert.AreEqual("(value ? \"true\" : \"false\")", ValueConversions.GetFormatExpression(ValueConversionKind.Boolean, "value"));
		Assert.AreEqual("value", ValueConversions.GetFormatExpression(ValueConversionKind.String, "value"));
	}

	[TestMethod]
	public void ValueConversionsShouldSupportEncodingOnlyWhenAllowed()
	{
		CSharpCompilation compilation = Compile("public class C { public System.Text.Encoding E { get; set; } }");
		(INamedTypeSymbol symbol, _, _) = FindClass(compilation, "C");
		ITypeSymbol type = ((IPropertySymbol)symbol.GetMembers("E").Single()).Type;

		Assert.IsFalse(ValueConversions.TryGetKind(type, out _));
		Assert.IsTrue(ValueConversions.TryGetKind(type, out ValueConversionKind kind, allowEncoding: true));
		Assert.AreEqual(ValueConversionKind.Encoding, kind);
		Assert.AreEqual("TryGetEncoding(raw, out global::System.Text.Encoding parsed)", ValueConversions.GetTryParseExpression(kind, "global::System.Text.Encoding", "raw", "parsed"));
		Assert.AreEqual("value.WebName", ValueConversions.GetFormatExpression(kind, "value"));
	}

	[TestMethod]
	public void ValueConversionsCreateShouldUnwrapNullableValueTypes()
	{
		CSharpCompilation compilation = Compile("public class C { public int? I { get; set; } public string S { get; set; } }");
		(INamedTypeSymbol symbol, _, _) = FindClass(compilation, "C");

		ValueConversion nullableInt = ValueConversions.Create(((IPropertySymbol)symbol.GetMembers("I").Single()).Type, ValueConversionKind.Integer);
		ValueConversion text = ValueConversions.Create(((IPropertySymbol)symbol.GetMembers("S").Single()).Type, ValueConversionKind.String);

		Assert.AreEqual(new ValueConversion(ValueConversionKind.Integer, "int", "Int32", true), nullableInt);
		Assert.AreEqual(new ValueConversion(ValueConversionKind.String, "string", "String", false), text);
	}

	[TestMethod]
	public void GetExcludedPropertiesShouldReadNamedArrayArgument()
	{
		CSharpCompilation compilation = Compile(@"
public class ExcludeAttribute : System.Attribute { public string[] Names { get; set; } }
[Exclude(Names = new[] { ""A"", ""B"" })] public class C { }
[Exclude] public class D { }");
		(INamedTypeSymbol withNames, _, _) = FindClass(compilation, "C");
		(INamedTypeSymbol withoutNames, _, _) = FindClass(compilation, "D");

		HashSet<string> excluded = GeneratorHelpers.GetExcludedProperties(withNames.GetAttributes().Single(), "Names");

		Assert.IsTrue(excluded.SetEquals(["A", "B"]));
		Assert.IsEmpty(GeneratorHelpers.GetExcludedProperties(withoutNames.GetAttributes().Single(), "Names"));
	}

	[TestMethod]
	[DataRow("FilePath", "filePath")]
	[DataRow("X", "x")]
	[DataRow("already", "already")]
	[DataRow("", "")]
	public void ToCamelCaseShouldLowercaseFirstLetter(string name, string expected)
		=> Assert.AreEqual(expected, GeneratorHelpers.ToCamelCase(name));

	[TestMethod]
	public void GetXmlSummaryCommentShouldFlattenSummary()
	{
		CSharpCompilation compilation = Compile(@"
public class C
{
	/// <summary>
	/// First line.
	/// Second line.
	/// </summary>
	public int A { get; set; }

	/// <remarks>No summary.</remarks>
	public int B { get; set; }

	public int C2 { get; set; }
}");
		(INamedTypeSymbol symbol, _, _) = FindClass(compilation, "C");

		Assert.AreEqual("First line. Second line.", GeneratorHelpers.GetXmlSummaryComment(symbol.GetMembers("A").Single()));
		Assert.IsNull(GeneratorHelpers.GetXmlSummaryComment(symbol.GetMembers("B").Single()));
		Assert.IsNull(GeneratorHelpers.GetXmlSummaryComment(symbol.GetMembers("C2").Single()));
	}

	[TestMethod]
	public void IsComputedPropertyShouldDetectBodies()
	{
		CSharpCompilation compilation = Compile("public class C { public int A { get; } public int B => 1; public int D { get { return 1; } } }");
		(INamedTypeSymbol symbol, _, _) = FindClass(compilation, "C");

		Assert.IsFalse(GeneratorHelpers.IsComputedProperty((IPropertySymbol)symbol.GetMembers("A").Single()));
		Assert.IsTrue(GeneratorHelpers.IsComputedProperty((IPropertySymbol)symbol.GetMembers("B").Single()));
		Assert.IsTrue(GeneratorHelpers.IsComputedProperty((IPropertySymbol)symbol.GetMembers("D").Single()));
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
		INamedTypeSymbol symbol = model.GetDeclaredSymbol(syntax)!;
		return (symbol, syntax, model);
	}

	public TestContext TestContext { get; set; }

	#endregion
}

#if !NETFRAMEWORK // Only referenced by the ValueTuple-guarded GetAttributeNames test above.
/// <summary>A local attribute used to exercise <c>GetAttributeNames</c> without touching injected attribute types.</summary>
#pragma warning disable CA1018 // Mark attributes with AttributeUsageAttribute
internal sealed class SampleWidgetAttribute : Attribute { }
#pragma warning restore CA1018 // Mark attributes with AttributeUsageAttribute
#endif
