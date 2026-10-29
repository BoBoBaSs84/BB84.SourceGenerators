// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BB84.SourceGenerators.Models;

/// <summary>
/// A cacheable description of a source location that does not root a syntax tree.
/// </summary>
/// <param name="FilePath">The path of the syntax tree containing the location.</param>
/// <param name="TextSpan">The text span of the location.</param>
/// <param name="LineSpan">The line span of the location.</param>
internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
	/// <summary>
	/// Creates a location description from the given location.
	/// </summary>
	/// <param name="location">The location.</param>
	/// <returns>The location description, or <see langword="null"/> if the location is not in source.</returns>
	internal static LocationInfo? From(Location location)
		=> location.SourceTree is SyntaxTree tree
			? new LocationInfo(tree.FilePath, location.SourceSpan, location.GetLineSpan().Span)
			: null;

	/// <summary>
	/// Creates a location description from the given syntax token.
	/// </summary>
	/// <param name="token">The syntax token.</param>
	/// <returns>The location description, or <see langword="null"/> if the token is not in source.</returns>
	internal static LocationInfo? From(SyntaxToken token)
		=> From(token.GetLocation());

	/// <summary>
	/// Creates a location description from the given syntax node.
	/// </summary>
	/// <param name="node">The syntax node.</param>
	/// <returns>The location description, or <see langword="null"/> if the node is not in source.</returns>
	internal static LocationInfo? From(SyntaxNode node)
		=> From(node.GetLocation());

	/// <summary>
	/// Resolves the location against the given compilation. When the compilation contains exactly one
	/// syntax tree with the stored path, a source location in that tree is returned; otherwise, an
	/// external file location with the stored spans.
	/// </summary>
	/// <param name="compilation">The compilation that contains the syntax tree.</param>
	/// <returns>The resolved location.</returns>
	internal Location ToLocation(Compilation compilation)
	{
		SyntaxTree? match = null;

		foreach (SyntaxTree tree in compilation.SyntaxTrees)
		{
			if (tree.FilePath != FilePath)
				continue;

			if (match is not null)
				return Location.Create(FilePath, TextSpan, LineSpan);

			match = tree;
		}

		return match is not null
			? Location.Create(match, TextSpan)
			: Location.Create(FilePath, TextSpan, LineSpan);
	}
}
