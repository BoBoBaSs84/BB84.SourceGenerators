// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate validation for a class marked with the
/// <c>[GenerateValidator]</c> attribute.
/// </summary>
/// <param name="Type">The class to extend.</param>
/// <param name="Properties">The properties to validate.</param>
/// <param name="ImplementsIValidatableObject">Indicates whether the class already implements <c>IValidatableObject</c>.</param>
internal sealed record ValidatorRequest(
	TypeTargetInfo Type,
	EquatableArray<ValidatorProperty> Properties,
	bool ImplementsIValidatableObject
	);
