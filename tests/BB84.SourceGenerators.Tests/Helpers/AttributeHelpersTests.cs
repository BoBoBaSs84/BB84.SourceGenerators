// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Reflection;

using BB84.SourceGenerators.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators.Tests.Helpers;

[TestClass]
public sealed class AttributeHelpersTests
{
	private const string ConfigAttributeName = "Sample.ConfigAttribute";

	private const string AttributeSource = @"
namespace Sample
{
	internal enum Mode { Slow, Fast }

	internal sealed class ConfigAttribute : System.Attribute
	{
		public ConfigAttribute() { }
		public ConfigAttribute(int count) { }
		public ConfigAttribute(bool enabled, string name = ""default"") { }
		public ConfigAttribute(params string[] names) { }
		public string Level { get; set; }
		public Mode Mode { get; set; }
		public string[] Names { get; set; }
	}

	internal sealed class OtherAttribute : System.Attribute { }
}

namespace Other
{
	internal sealed class ConfigAttribute : System.Attribute { }
}
";

	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	[DataRow("using Sample;\n[Config(Level = \"High\")] public class Target { }")]
	[DataRow("using Sample;\n[ConfigAttribute(Level = \"High\")] public class Target { }")]
	[DataRow("[Sample.Config(Level = \"High\")] public class Target { }")]
	[DataRow("[global::Sample.ConfigAttribute(Level = \"High\")] public class Target { }")]
	[DataRow("using C = Sample.ConfigAttribute;\n[C(Level = \"High\")] public class Target { }")]
	public void GetAttributeShouldMatchRegardlessOfSpelling(string source)
	{
		INamedTypeSymbol target = GetClass(source, "Target");

		AttributeData? attribute = AttributeHelpers.GetAttribute(target, ConfigAttributeName);

		Assert.IsNotNull(attribute);
		Assert.AreEqual("High", AttributeHelpers.GetNamedArgument(attribute, "Level", "default"));
	}

	[TestMethod]
	public void GetAttributeShouldNotMatchAttributeWithSameSimpleNameInOtherNamespace()
	{
		INamedTypeSymbol target = GetClass("[Other.Config] public class Target { }", "Target");

		Assert.IsNull(AttributeHelpers.GetAttribute(target, ConfigAttributeName));
		Assert.IsFalse(AttributeHelpers.HasAttribute(target, ConfigAttributeName));
		Assert.IsTrue(AttributeHelpers.HasAttribute(target, "Other.ConfigAttribute"));
	}

	[TestMethod]
	public void GetAttributeShouldReturnNullWhenAttributeMissing()
	{
		INamedTypeSymbol target = GetClass("[Sample.Other] public class Target { }", "Target");

		Assert.IsNull(AttributeHelpers.GetAttribute(target, ConfigAttributeName));
		Assert.IsFalse(AttributeHelpers.HasAttribute(target, ConfigAttributeName));
	}

	[TestMethod]
	public void GetConstructorArgumentShouldReadPositionalArgumentsIncludingDefaults()
	{
		INamedTypeSymbol target = GetClass("[Sample.Config(true)] public class Target { }", "Target");
		AttributeData attribute = AttributeHelpers.GetAttribute(target, ConfigAttributeName)!;

		Assert.IsTrue(AttributeHelpers.GetConstructorArgument(attribute, 0, false));
		Assert.AreEqual("default", AttributeHelpers.GetConstructorArgument(attribute, 1, "fallback"));
		Assert.AreEqual("fallback", AttributeHelpers.GetConstructorArgument(attribute, 5, "fallback"));
		Assert.AreEqual(0, AttributeHelpers.GetConstructorArgument(attribute, 0, 0));
	}

	[TestMethod]
	public void GetConstructorArgumentBySymbolShouldReturnDefaultWhenAttributeMissing()
	{
		INamedTypeSymbol withAttribute = GetClass("[Sample.Config(true)] public class Target { }", "Target");
		INamedTypeSymbol withoutAttribute = GetClass("public class Target { }", "Target");

		Assert.IsTrue(AttributeHelpers.GetConstructorArgument(withAttribute, ConfigAttributeName, 0, false));
		Assert.IsFalse(AttributeHelpers.GetConstructorArgument(withoutAttribute, ConfigAttributeName, 0, false));
	}

	[TestMethod]
	public void GetNamedArgumentShouldReadEnumAsUnderlyingValueAndDefaultWhenUnset()
	{
		INamedTypeSymbol target = GetClass("[Sample.Config(Mode = Sample.Mode.Fast)] public class Target { }", "Target");
		AttributeData attribute = AttributeHelpers.GetAttribute(target, ConfigAttributeName)!;

		Assert.AreEqual(1, AttributeHelpers.GetNamedArgument(attribute, "Mode", 0));
		Assert.AreEqual("unset", AttributeHelpers.GetNamedArgument(attribute, "Level", "unset"));
	}

	[TestMethod]
	public void GetExcludedPropertiesShouldReadParamsConstructorArgument()
	{
		INamedTypeSymbol target = GetClass("[Sample.Config(\"Secret\", nameof(Target.Internal), Level = \"Ignored\")] public class Target { public int Internal { get; set; } }", "Target");
		AttributeData attribute = AttributeHelpers.GetAttribute(target, ConfigAttributeName)!;

		HashSet<string> excluded = AttributeHelpers.GetExcludedProperties(attribute);

		Assert.IsTrue(excluded.SetEquals(["Secret", "Internal"]));
	}

	[TestMethod]
	public void GetExcludedPropertiesShouldBeEmptyWithoutParamsArgument()
	{
		INamedTypeSymbol target = GetClass("[Sample.Config(Level = \"High\")] public class Target { }", "Target");
		AttributeData attribute = AttributeHelpers.GetAttribute(target, ConfigAttributeName)!;

		Assert.IsEmpty(AttributeHelpers.GetExcludedProperties(attribute));
		Assert.IsNull(AttributeHelpers.GetConstructorArrayArgument(attribute, 0));
	}

	[TestMethod]
	public void GetExcludedPropertiesShouldReadNamedArrayArgument()
	{
		INamedTypeSymbol withNames = GetClass("[Sample.Config(Names = new[] { \"A\", \"B\" })] public class Target { }", "Target");
		INamedTypeSymbol withoutNames = GetClass("[Sample.Config] public class Target { }", "Target");

		HashSet<string> excluded = AttributeHelpers.GetExcludedProperties(AttributeHelpers.GetAttribute(withNames, ConfigAttributeName)!, "Names");

		Assert.IsTrue(excluded.SetEquals(["A", "B"]));
		Assert.IsEmpty(AttributeHelpers.GetExcludedProperties(AttributeHelpers.GetAttribute(withoutNames, ConfigAttributeName)!, "Names"));
	}

	[TestMethod]
	public void TryGetSingleCtorArgShouldRequireExactlyOneArgumentOfType()
	{
		INamedTypeSymbol single = GetClass("[Sample.Config(5)] public class Target { }", "Target");
		INamedTypeSymbol two = GetClass("[Sample.Config(true, \"Second\")] public class Target { }", "Target");

		Assert.IsTrue(AttributeHelpers.TryGetSingleCtorArg(single, ConfigAttributeName, out int count));
		Assert.AreEqual(5, count);
		Assert.IsFalse(AttributeHelpers.TryGetSingleCtorArg(single, ConfigAttributeName, out string _));
		Assert.IsFalse(AttributeHelpers.TryGetSingleCtorArg(two, ConfigAttributeName, out bool _));
	}

	private static INamedTypeSymbol GetClass(string source, string className)
	{
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "AttributeHelpersTestAssembly",
			syntaxTrees: [CSharpSyntaxTree.ParseText(AttributeSource), CSharpSyntaxTree.ParseText(source)],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		SyntaxTree tree = compilation.SyntaxTrees[1];
		ClassDeclarationSyntax syntax = tree.GetRoot()
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.First(c => c.Identifier.Text == className);

		return compilation.GetSemanticModel(tree).GetDeclaredSymbol(syntax)!;
	}
}
