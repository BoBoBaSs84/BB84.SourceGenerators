// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a property with at least one validation rule.
/// </summary>
/// <param name="Name">The name of the property.</param>
/// <param name="TypeName">The type name of the property.</param>
/// <param name="IsCollection">Indicates whether the property type is a collection.</param>
/// <param name="IsArray">Indicates whether the property type is an array.</param>
/// <param name="Rules">The validation rules of the property.</param>
internal readonly record struct ValidatorProperty(
	string Name,
	string TypeName,
	bool IsCollection,
	bool IsArray,
	EquatableArray<ValidatorRule> Rules
	);
