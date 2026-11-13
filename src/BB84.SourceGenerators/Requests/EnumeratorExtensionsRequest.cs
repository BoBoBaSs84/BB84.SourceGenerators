// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate extension methods for an enumeration marked with the
/// <c>[GenerateEnumeratorExtensions]</c> attribute.
/// </summary>
/// <param name="EnumName">The name of the enumeration.</param>
/// <param name="NamespaceName">The namespace of the enumeration.</param>
/// <param name="Members">
/// The enumeration members in declaration order, with their <c>[Description]</c> text, if any, and whether
/// the member is an alias, i.e. its value was already declared by an earlier member.
/// </param>
/// <param name="IsFlags">Indicates whether the enumeration is decorated with <c>[Flags]</c>.</param>
/// <param name="FlagMembers">
/// The names of the single-bit members in declaration order, one per distinct bit; empty unless <paramref name="IsFlags"/> is set.
/// </param>
internal sealed record EnumeratorExtensionsRequest(
	string EnumName,
	string NamespaceName,
	EquatableArray<(string Name, string? Description, bool IsAlias)> Members,
	bool IsFlags,
	EquatableArray<string> FlagMembers
	);
