// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Helpers;

namespace BB84.SourceGenerators.Tests.Helpers;

[TestClass]
public sealed class NamingHelpersTests
{
	[TestMethod]
	[DataRow("_name", "Name")]
	[DataRow("name", "Name")]
	[DataRow("m_count", "M_count")]
	[DataRow("_", "")]
	public void FieldNameToPropertyNameShouldStripUnderscoreAndCapitalize(string fieldName, string expected)
		=> Assert.AreEqual(expected, NamingHelpers.FieldNameToPropertyName(fieldName));

	[TestMethod]
	public void PropertyNameToFieldNameShouldPrefixUnderscoreAndLowercase()
		=> Assert.AreEqual("_firstName", NamingHelpers.PropertyNameToFieldName("FirstName"));

	[TestMethod]
	[DataRow("FilePath", "filePath")]
	[DataRow("X", "x")]
	[DataRow("already", "already")]
	[DataRow("", "")]
	public void ToCamelCaseShouldLowercaseFirstLetter(string name, string expected)
		=> Assert.AreEqual(expected, NamingHelpers.ToCamelCase(name));

	[TestMethod]
	[DataRow("Token", "TOKEN")]
	[DataRow("ConnectTimeoutMs", "CONNECT_TIMEOUT_MS")]
	[DataRow("HTTPPort", "HTTP_PORT")]
	[DataRow("MaxRetries2Count", "MAX_RETRIES2_COUNT")]
	[DataRow("Already_Snake", "ALREADY_SNAKE")]
	[DataRow("lowerCamel", "LOWER_CAMEL")]
	public void ToUpperSnakeCaseShouldSplitWordsAndKeepAcronyms(string name, string expected)
		=> Assert.AreEqual(expected, NamingHelpers.ToUpperSnakeCase(name));

	[TestMethod]
	[DataRow("Items", "items")]
	[DataRow("MyItems", "myItems")]
	[DataRow("Class", "@class")]
	[DataRow("Event", "@event")]
	[DataRow("String", "@string")]
	[DataRow("Var", "var")]
	public void ToLocalNameShouldCamelCaseAndEscapeKeywords(string memberName, string expected)
		=> Assert.AreEqual(expected, NamingHelpers.ToLocalName(memberName, new HashSet<string>(StringComparer.Ordinal)));

	[TestMethod]
	public void ToLocalNameShouldAppendSuffixOnCollision()
	{
		HashSet<string> reserved = new(StringComparer.Ordinal) { "sb", "items1" };

		Assert.AreEqual("sb1", NamingHelpers.ToLocalName("Sb", reserved));
		Assert.AreEqual("items", NamingHelpers.ToLocalName("Items", reserved));
		Assert.AreEqual("items2", NamingHelpers.ToLocalName("Items", reserved));
		Assert.AreEqual("iTEMS", NamingHelpers.ToLocalName("ITEMS", reserved));
	}

	[TestMethod]
	public void ToLocalNameShouldReserveChosenName()
	{
		HashSet<string> reserved = new(StringComparer.Ordinal);

		Assert.AreEqual("@class", NamingHelpers.ToLocalName("Class", reserved));
		Assert.AreEqual("class1", NamingHelpers.ToLocalName("class", reserved));
		Assert.IsTrue(reserved.SetEquals(["class", "class1"]));
	}
}
