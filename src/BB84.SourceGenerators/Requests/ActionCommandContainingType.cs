// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents the type that contains a method marked with the <c>[ActionCommand]</c> attribute.
/// </summary>
/// <param name="NamespaceName">The namespace of the containing type.</param>
/// <param name="TypeAccessibility">The accessibility keyword of the containing type.</param>
/// <param name="TypeName">The name of the containing type.</param>
/// <param name="IsRecord">Indicates whether the containing type is a record class.</param>
/// <param name="OuterClasses">The outer (nesting) classes, from outermost to innermost.</param>
internal sealed record ActionCommandContainingType(
	string NamespaceName,
	string TypeAccessibility,
	string TypeName,
	bool IsRecord,
	EquatableArray<(string Accessibility, string Name)> OuterClasses
	);
