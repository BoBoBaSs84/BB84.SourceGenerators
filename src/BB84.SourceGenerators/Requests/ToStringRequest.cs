// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate a <c>ToString()</c> override for a class or record marked with the
/// <c>[GenerateToString]</c> attribute.
/// </summary>
/// <param name="Type">The class or record to extend.</param>
/// <param name="IsRecord">Indicates whether the type is a record class.</param>
/// <param name="Properties">The properties included in the output.</param>
/// <param name="CollectionFormat">The format used for collection properties.</param>
/// <param name="Separator">The separator between properties.</param>
/// <param name="Formattable">Indicates whether <c>IFormattable</c> is implemented.</param>
/// <param name="NullPlaceholder">The text emitted for <see langword="null"/> values, or <see langword="null"/> if none.</param>
/// <param name="PrintMembersIsOverride">Indicates whether the record's <c>PrintMembers</c> overrides a base record.</param>
internal sealed record ToStringRequest(
	TypeTargetInfo Type,
	bool IsRecord,
	EquatableArray<PropertyDescriptor> Properties,
	CollectionFormat CollectionFormat,
	string Separator,
	bool Formattable,
	string? NullPlaceholder,
	bool PrintMembersIsOverride
	);
