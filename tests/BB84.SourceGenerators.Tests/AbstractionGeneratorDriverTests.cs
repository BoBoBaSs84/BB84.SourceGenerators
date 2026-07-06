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
/// Runtime generator-driver tests for <see cref="AbstractionGenerator"/>. Unlike
/// <c>AbstractionGeneratorTests</c> (which exercises pre-compiled providers), these run the
/// generator in-process so its emitted interface and implementation source can be asserted directly.
/// </summary>
[TestClass]
public sealed class AbstractionGeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void ShouldGenerateInterfaceAndImplementationForStaticClass()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public static class Calculator
	{
		public static int Value { get; set; }
		public static int Add(int a, int b) => a + b;
	}

	[GenerateAbstraction(typeof(Calculator), typeof(ICalculatorProvider), typeof(CalculatorProvider))]
	internal sealed partial class CalculatorProvider { }

	public partial interface ICalculatorProvider { }
}";
		string[] generated = RunGenerator(source);

		string iface = generated.First(s => s.Contains("partial interface ICalculatorProvider"));
		Assert.Contains("int Value { get; set; }", iface);
		Assert.Contains("int Add(int a, int b);", iface);

		string impl = generated.First(s => s.Contains("partial class CalculatorProvider"));
		Assert.Contains("partial class CalculatorProvider : ICalculatorProvider", impl);
		Assert.Contains("Calculator.Value", impl);
		Assert.Contains("Calculator.Add(a, b)", impl);
	}

	[TestMethod]
	public void ShouldExcludeSpecifiedProperties()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public static class Settings
	{
		public static int Visible { get; set; }
		public static int Hidden { get; set; }
	}

	[GenerateAbstraction(typeof(Settings), typeof(ISettingsProvider), typeof(SettingsProvider),
		ExcludeProperties = new string[] { ""Hidden"" })]
	internal sealed partial class SettingsProvider { }

	public partial interface ISettingsProvider { }
}";
		string[] generated = RunGenerator(source);

		string iface = generated.First(s => s.Contains("partial interface ISettingsProvider"));
		Assert.Contains("Visible", iface);
		Assert.DoesNotContain("Hidden", iface);
	}

	[TestMethod]
	public void ShouldExcludeSpecifiedMethods()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public static class Service
	{
		public static void Keep() { }
		public static void Drop() { }
	}

	[GenerateAbstraction(typeof(Service), typeof(IServiceProvider2), typeof(ServiceProvider2),
		ExcludeMethods = new string[] { ""Drop"" })]
	internal sealed partial class ServiceProvider2 { }

	public partial interface IServiceProvider2 { }
}";
		string[] generated = RunGenerator(source);

		string iface = generated.First(s => s.Contains("partial interface IServiceProvider2"));
		Assert.Contains("Keep()", iface);
		Assert.DoesNotContain("Drop()", iface);
	}

	[TestMethod]
	public void ShouldEmitOutParameterModifier()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public static class Parser
	{
		public static bool TryParse(string text, out int result) { result = 0; return false; }
	}

	[GenerateAbstraction(typeof(Parser), typeof(IParserProvider), typeof(ParserProvider))]
	internal sealed partial class ParserProvider { }

	public partial interface IParserProvider { }
}";
		string[] generated = RunGenerator(source);

		string iface = generated.First(s => s.Contains("partial interface IParserProvider"));
		Assert.Contains("out int result", iface);

		string impl = generated.First(s => s.Contains("partial class ParserProvider"));
		Assert.Contains("out result", impl);
	}

	[TestMethod]
	public void ShouldEmitParamsRefAndInModifiers()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public static class Modifiers
	{
		public static int Sum(params int[] values) => 0;
		public static void ByRef(ref int x) { }
		public static void ByIn(in int x) { }
	}

	[GenerateAbstraction(typeof(Modifiers), typeof(IModifiersProvider), typeof(ModifiersProvider))]
	internal sealed partial class ModifiersProvider { }

	public partial interface IModifiersProvider { }
}";
		string[] generated = RunGenerator(source);

		string iface = generated.First(s => s.Contains("partial interface IModifiersProvider"));
		Assert.Contains("params int[] values", iface);
		Assert.Contains("ref int x", iface);
		Assert.Contains("in int x", iface);
	}

	[TestMethod]
	public void ReadOnlyStaticPropertyShouldEmitGetterOnly()
	{
		const string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public static class Config
	{
		public static int ReadOnlyValue { get; }
	}

	[GenerateAbstraction(typeof(Config), typeof(IConfigProvider), typeof(ConfigProvider))]
	internal sealed partial class ConfigProvider { }

	public partial interface IConfigProvider { }
}";
		string[] generated = RunGenerator(source);

		string iface = generated.First(s => s.Contains("partial interface IConfigProvider"));
		Assert.Contains("ReadOnlyValue { get; }", iface);
		Assert.DoesNotContain("ReadOnlyValue { get; set;", iface);
	}

	private static string[] RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "AbstractionDriverTestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new AbstractionGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		return [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];
	}
}
