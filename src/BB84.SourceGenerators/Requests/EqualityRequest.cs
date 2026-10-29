// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate equality members for a class marked with the
/// <c>[GenerateEquality]</c> attribute.
/// </summary>
/// <param name="Type">The class to extend.</param>
/// <param name="Properties">The properties that take part in equality.</param>
internal sealed record EqualityRequest(
	TypeTargetInfo Type,
	EquatableArray<PropertyDescriptor> Properties
	);
