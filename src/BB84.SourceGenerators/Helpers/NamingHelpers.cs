// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis.CSharp;

namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Provides helper methods that derive identifiers and names for generated code.
/// </summary>
internal static class NamingHelpers
{
	/// <summary>
	/// Converts a field name to a property name by removing a leading underscore and
	/// capitalizing the first letter.
	/// </summary>
	/// <param name="fieldName">The field name to convert.</param>
	/// <returns>The converted property name, or an empty string if nothing remains.</returns>
	internal static string FieldNameToPropertyName(string fieldName)
	{
		string name = fieldName.StartsWith("_", StringComparison.OrdinalIgnoreCase) ? fieldName.Substring(1) : fieldName;
		return name.Length == 0 ? name : $"{char.ToUpperInvariant(name[0])}{name.Substring(1)}";
	}

	/// <summary>
	/// Converts a property name to a backing field name by prefixing an underscore
	/// and lowercasing the first letter.
	/// </summary>
	/// <param name="propertyName">The property name to convert.</param>
	/// <returns>The converted field name.</returns>
	internal static string PropertyNameToFieldName(string propertyName)
		=> $"_{ToCamelCase(propertyName)}";

	/// <summary>
	/// Converts a PascalCase name to camel case by lowercasing the first letter
	/// (e.g. <c>FilePath</c> to <c>filePath</c>).
	/// </summary>
	/// <param name="name">The name to convert.</param>
	/// <returns>The camel case name.</returns>
	internal static string ToCamelCase(string name)
		=> name.Length == 0 ? name : $"{char.ToLowerInvariant(name[0])}{name.Substring(1)}";

	/// <summary>
	/// Converts a PascalCase or camelCase name to upper snake case, keeping acronyms together
	/// (e.g. <c>ConnectTimeoutMs</c> to <c>CONNECT_TIMEOUT_MS</c>, <c>HTTPPort</c> to <c>HTTP_PORT</c>).
	/// </summary>
	/// <param name="name">The name to convert.</param>
	/// <returns>The upper snake case name.</returns>
	internal static string ToUpperSnakeCase(string name)
	{
		System.Text.StringBuilder builder = new(name.Length + 8);

		for (int i = 0; i < name.Length; i++)
		{
			char current = name[i];

			if (i > 0 && char.IsUpper(current) && name[i - 1] != '_')
			{
				char previous = name[i - 1];
				bool nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

				if (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower))
					_ = builder.Append('_');
			}

			_ = builder.Append(char.ToUpperInvariant(current));
		}

		return builder.ToString();
	}

	/// <summary>
	/// Derives a valid, collision-free local variable name from a member name. The name is camel
	/// cased, suffixed with a number while it is contained in <paramref name="reserved"/>, and
	/// prefixed with <c>@</c> if it is a C# keyword (e.g. <c>Class</c> to <c>@class</c>).
	/// </summary>
	/// <param name="memberName">The member name to derive the local name from.</param>
	/// <param name="reserved">
	/// The names already in use in the scope of the local. The chosen name (without the <c>@</c>
	/// prefix) is added, so subsequent calls with the same set never return it again.
	/// </param>
	/// <returns>The local variable name, ready to be emitted.</returns>
	internal static string ToLocalName(string memberName, ISet<string> reserved)
	{
		string baseName = ToCamelCase(memberName);
		string name = baseName;

		for (int suffix = 1; reserved.Contains(name); suffix++)
			name = $"{baseName}{suffix}";

		_ = reserved.Add(name);

		return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? $"@{name}" : name;
	}
}
