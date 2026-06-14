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
		sb.OpenClass("public", "Foo");
		sb.CloseClass();

		string result = sb.ToString();

		Assert.Contains("public partial class Foo", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenClassWithBaseTypesEmitsPartialClassDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		sb.OpenClass("public", "Foo", "IDisposable");
		sb.CloseClass();

		string result = sb.ToString();

		Assert.Contains("public partial class Foo : IDisposable", result);
	}

	[TestMethod]
	public void OpenSealedClassWithoutBaseTypesEmitsSealedClassDeclaration()
	{
		SourceBuilder sb = new();
		sb.OpenSealedClass("public", "Foo");
		sb.CloseClass();

		string result = sb.ToString();

		Assert.Contains("public sealed class Foo", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenSealedClassWithBaseTypesEmitsSealedClassDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		sb.OpenSealedClass("public", "Foo", "IDisposable");
		sb.CloseClass();

		string result = sb.ToString();

		Assert.Contains("public sealed class Foo : IDisposable", result);
	}

	[TestMethod]
	public void OpenStructWithoutBaseTypesEmitsPartialStructDeclaration()
	{
		SourceBuilder sb = new();
		sb.OpenStruct("public", "Foo");
		sb.CloseStruct();

		string result = sb.ToString();

		Assert.Contains("public partial struct Foo", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenStructWithBaseTypesEmitsPartialStructDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		sb.OpenStruct("public", "Foo", "IEquatable<Foo>");
		sb.CloseStruct();

		string result = sb.ToString();

		Assert.Contains("public partial struct Foo : IEquatable<Foo>", result);
	}

	[TestMethod]
	public void OpenRecordWithoutBaseTypesEmitsPartialRecordDeclaration()
	{
		SourceBuilder sb = new();
		sb.OpenRecord("public", "Bar");
		sb.CloseRecord();

		string result = sb.ToString();

		Assert.Contains("public partial record Bar", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenRecordWithBaseTypesEmitsPartialRecordDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		sb.OpenRecord("public", "Bar", "IDisposable");
		sb.CloseRecord();

		string result = sb.ToString();

		Assert.Contains("public partial record Bar : IDisposable", result);
	}

	[TestMethod]
	public void OpenSealedRecordWithoutBaseTypesEmitsSealedPartialRecordDeclaration()
	{
		SourceBuilder sb = new();
		sb.OpenSealedRecord("public", "Bar");
		sb.CloseRecord();

		string result = sb.ToString();

		Assert.Contains("public sealed partial record Bar", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenSealedRecordWithBaseTypesEmitsSealedPartialRecordDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		sb.OpenSealedRecord("public", "Bar", "BaseRecord");
		sb.CloseRecord();

		string result = sb.ToString();

		Assert.Contains("public sealed partial record Bar : BaseRecord", result);
	}

	[TestMethod]
	public void OpenRecordStructWithoutBaseTypesEmitsPartialRecordStructDeclaration()
	{
		SourceBuilder sb = new();
		sb.OpenRecordStruct("public", "Baz");
		sb.CloseRecordStruct();

		string result = sb.ToString();

		Assert.Contains("public partial record struct Baz", result);
		Assert.DoesNotContain(':', result);
	}

	[TestMethod]
	public void OpenRecordStructWithBaseTypesEmitsPartialRecordStructDeclarationWithBaseTypes()
	{
		SourceBuilder sb = new();
		sb.OpenRecordStruct("public", "Baz", "IEquatable<Baz>");
		sb.CloseRecordStruct();

		string result = sb.ToString();

		Assert.Contains("public partial record struct Baz : IEquatable<Baz>", result);
	}

	[TestMethod]
	public void OpenStructIncreasesIndentLevel()
	{
		SourceBuilder sb = new();

		Assert.AreEqual(0, sb.IndentLevel);
		sb.OpenStruct("public", "Foo");
		Assert.AreEqual(1, sb.IndentLevel);
		sb.CloseStruct();
		Assert.AreEqual(0, sb.IndentLevel);
	}

	[TestMethod]
	public void OpenRecordIncreasesIndentLevel()
	{
		SourceBuilder sb = new();

		Assert.AreEqual(0, sb.IndentLevel);
		sb.OpenRecord("public", "Bar");
		Assert.AreEqual(1, sb.IndentLevel);
		sb.CloseRecord();
		Assert.AreEqual(0, sb.IndentLevel);
	}

	[TestMethod]
	public void OpenRecordStructIncreasesIndentLevel()
	{
		SourceBuilder sb = new();

		Assert.AreEqual(0, sb.IndentLevel);
		sb.OpenRecordStruct("public", "Baz");
		Assert.AreEqual(1, sb.IndentLevel);
		sb.CloseRecordStruct();
		Assert.AreEqual(0, sb.IndentLevel);
	}
}
