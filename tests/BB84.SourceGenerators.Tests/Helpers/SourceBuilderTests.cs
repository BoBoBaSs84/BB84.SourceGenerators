// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Helpers;

namespace BB84.SourceGenerators.Tests.Helpers;

[TestClass]
public sealed class SourceBuilderTests
{
	[TestMethod]
	public void OpenClassWithoutBaseTypesEmitsPartialClassDeclaration()
	{
		SourceBuilder sb = new();
		_ = sb.OpenClass("public", "Foo");
		_ = sb.CloseClass();

		string result = sb.ToString();

		Assert.Contains("public partial class Foo", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenClassWithBaseTypesEmitsPartialClassDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		_ = sb.OpenClass("public", "Foo", "IDisposable");
		_ = sb.CloseClass();

		string result = sb.ToString();

		Assert.Contains("public partial class Foo : IDisposable", result);
	}

	[TestMethod]
	public void OpenSealedClassWithoutBaseTypesEmitsSealedClassDeclaration()
	{
		SourceBuilder sb = new();
		_ = sb.OpenSealedClass("public", "Foo");
		_ = sb.CloseClass();

		string result = sb.ToString();

		Assert.Contains("public sealed class Foo", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenSealedClassWithBaseTypesEmitsSealedClassDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		_ = sb.OpenSealedClass("public", "Foo", "IDisposable");
		_ = sb.CloseClass();

		string result = sb.ToString();

		Assert.Contains("public sealed class Foo : IDisposable", result);
	}

	[TestMethod]
	public void OpenStructWithoutBaseTypesEmitsPartialStructDeclaration()
	{
		SourceBuilder sb = new();
		_ = sb.OpenStruct("public", "Foo");
		_ = sb.CloseStruct();

		string result = sb.ToString();

		Assert.Contains("public partial struct Foo", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenStructWithBaseTypesEmitsPartialStructDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		_ = sb.OpenStruct("public", "Foo", "IEquatable<Foo>");
		_ = sb.CloseStruct();

		string result = sb.ToString();

		Assert.Contains("public partial struct Foo : IEquatable<Foo>", result);
	}

	[TestMethod]
	public void OpenRecordWithoutBaseTypesEmitsPartialRecordDeclaration()
	{
		SourceBuilder sb = new();
		_ = sb.OpenRecord("public", "Bar");
		_ = sb.CloseRecord();

		string result = sb.ToString();

		Assert.Contains("public partial record Bar", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenRecordWithBaseTypesEmitsPartialRecordDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		_ = sb.OpenRecord("public", "Bar", "IDisposable");
		_ = sb.CloseRecord();

		string result = sb.ToString();

		Assert.Contains("public partial record Bar : IDisposable", result);
	}

	[TestMethod]
	public void OpenSealedRecordWithoutBaseTypesEmitsSealedPartialRecordDeclaration()
	{
		SourceBuilder sb = new();
		_ = sb.OpenSealedRecord("public", "Bar");
		_ = sb.CloseRecord();

		string result = sb.ToString();

		Assert.Contains("public sealed partial record Bar", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenSealedRecordWithBaseTypesEmitsSealedPartialRecordDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		_ = sb.OpenSealedRecord("public", "Bar", "BaseRecord");
		_ = sb.CloseRecord();

		string result = sb.ToString();

		Assert.Contains("public sealed partial record Bar : BaseRecord", result);
	}

	[TestMethod]
	public void OpenRecordStructWithoutBaseTypesEmitsPartialRecordStructDeclaration()
	{
		SourceBuilder sb = new();
		_ = sb.OpenRecordStruct("public", "Baz");
		_ = sb.CloseRecordStruct();

		string result = sb.ToString();

		Assert.Contains("public partial record struct Baz", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenRecordStructWithBaseTypesEmitsPartialRecordStructDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		_ = sb.OpenRecordStruct("public", "Baz", "IEquatable<Baz>");
		_ = sb.CloseRecordStruct();

		string result = sb.ToString();

		Assert.Contains("public partial record struct Baz : IEquatable<Baz>", result);
	}

	[TestMethod]
	public void OpenStructIncreasesIndentLevel()
	{
		SourceBuilder sb = new();

		Assert.AreEqual(0, sb.IndentLevel);
		_ = sb.OpenStruct("public", "Foo");
		Assert.AreEqual(1, sb.IndentLevel);
		_ = sb.CloseStruct();
		Assert.AreEqual(0, sb.IndentLevel);
	}

	[TestMethod]
	public void OpenRecordIncreasesIndentLevel()
	{
		SourceBuilder sb = new();

		Assert.AreEqual(0, sb.IndentLevel);
		_ = sb.OpenRecord("public", "Bar");
		Assert.AreEqual(1, sb.IndentLevel);
		_ = sb.CloseRecord();
		Assert.AreEqual(0, sb.IndentLevel);
	}

	[TestMethod]
	public void AppendSummaryEmitsSingleLineSummaryBlock()
	{
		SourceBuilder sb = new();
		_ = sb.AppendSummary("Does things.");

		string[] lines = sb.ToString().Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);

		string[] expected = ["/// <summary>", "/// Does things.", "/// </summary>"];
		Assert.AreSequenceEqual(expected, lines);
	}

	// NOTE: BeginTypeFile/EndTypeFile and OpenOuterClasses/CloseOuterClasses accept System.ValueTuple lists.
	// Invoking them across the netstandard2.0 -> net4x assembly boundary throws MissingMethodException
	// under Mono (the Linux CI host for the .NET Framework targets), whose ValueTuple identity does not
	// unify with the generator assembly's. The logic is runtime-independent and fully covered by the
	// modern-.NET run, so these cases are excluded on .NET Framework.
#if !NETFRAMEWORK
	[TestMethod]
	public void BeginAndEndTypeFileEmitHeaderNamespaceAndOuterClasses()
	{
		SourceBuilder sb = new();
		_ = sb.BeginTypeFile("Tool", "My.Space", [("public", "Outer")], ["System"]);
		_ = sb.AppendLine("// body");
		_ = sb.EndTypeFile([("public", "Outer")]);

		string result = sb.ToString();

		Assert.Contains("//     Tool: Tool", result);
		Assert.Contains($"#nullable enable{Environment.NewLine}using System;{Environment.NewLine}{Environment.NewLine}namespace My.Space", result);
		Assert.Contains($"  public partial class Outer{Environment.NewLine}  {{{Environment.NewLine}    // body", result);
		Assert.AreEqual(0, sb.IndentLevel);
	}

	[TestMethod]
	public void OpenOuterClassesWithStaticFlagEmitsStaticModifier()
	{
		SourceBuilder sb = new();
		_ = sb.OpenOuterClasses([("public", "Outer", true), ("internal", "Inner", false)]);
		_ = sb.CloseOuterClasses([("public", "Outer", true), ("internal", "Inner", false)]);

		string result = sb.ToString();

		Assert.Contains("public static partial class Outer", result);
		Assert.Contains("internal partial class Inner", result);
		Assert.AreEqual(0, sb.IndentLevel);
	}
#endif

	[TestMethod]
	public void OpenRecordStructIncreasesIndentLevel()
	{
		SourceBuilder sb = new();

		Assert.AreEqual(0, sb.IndentLevel);
		_ = sb.OpenRecordStruct("public", "Baz");
		Assert.AreEqual(1, sb.IndentLevel);
		_ = sb.CloseRecordStruct();
		Assert.AreEqual(0, sb.IndentLevel);
	}
}
