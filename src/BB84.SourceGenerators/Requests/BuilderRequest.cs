// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate a fluent builder for a class marked with the
/// <c>[GenerateBuilder]</c> attribute.
/// </summary>
/// <param name="Type">The class to build.</param>
/// <param name="Properties">The settable properties of the class.</param>
internal sealed record BuilderRequest(
	TypeTargetInfo Type,
	EquatableArray<PropertyDescriptor> Properties
	);
