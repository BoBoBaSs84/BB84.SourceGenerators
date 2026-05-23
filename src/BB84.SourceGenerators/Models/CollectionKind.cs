// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Models;

/// <summary>
/// Classifies a property's collection type for deep clone code generation.
/// </summary>
internal enum CollectionKind
{
	/// <summary>The property is not a collection.</summary>
	None,
	/// <summary>The property is a <c>List&lt;T&gt;</c>.</summary>
	List,
	/// <summary>The property is a <c>Dictionary&lt;TKey, TValue&gt;</c>.</summary>
	Dictionary,
	/// <summary>The property is an array (<c>T[]</c>).</summary>
	Array,
	/// <summary>The property is an <c>ImmutableArray&lt;T&gt;</c>.</summary>
	ImmutableArray,
	/// <summary>The property is a read-only collection (<c>IReadOnlyList&lt;T&gt;</c>, <c>IReadOnlyCollection&lt;T&gt;</c>, <c>ReadOnlyCollection&lt;T&gt;</c>).</summary>
	ReadOnlyCollection
}
