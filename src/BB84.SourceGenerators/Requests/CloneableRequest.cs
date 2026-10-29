// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate clone methods for a class or struct marked with the
/// <c>[GenerateCloneable]</c> attribute.
/// </summary>
/// <param name="Type">The class or struct to extend.</param>
/// <param name="Properties">The settable properties that are copied.</param>
internal sealed record CloneableRequest(
	TypeTargetInfo Type,
	EquatableArray<PropertyDescriptor> Properties
	);
