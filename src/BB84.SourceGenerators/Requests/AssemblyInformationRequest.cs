// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate assembly information constants for a class marked with the
/// <c>[GenerateAssemblyInformation]</c> attribute.
/// </summary>
/// <param name="Type">The class to extend.</param>
/// <param name="ConstantValues">The values of the well-known assembly attributes, in declaration order of the constants.</param>
/// <param name="OtherAttributes">The values of all other assembly attributes, grouped by attribute name in discovery order.</param>
internal sealed record AssemblyInformationRequest(
	TypeTargetInfo Type,
	EquatableArray<string> ConstantValues,
	EquatableArray<(string AttributeName, EquatableArray<string> Values)> OtherAttributes
	);
