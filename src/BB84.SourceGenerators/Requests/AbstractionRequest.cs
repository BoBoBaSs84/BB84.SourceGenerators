// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate an abstraction for a static class.
/// </summary>
/// <param name="AbstractionNamespace">The namespace of the generated abstraction.</param>
/// <param name="AbstractionAccessibility">The accessibility keyword of the generated abstraction.</param>
/// <param name="AbstractionName">The name of the generated abstraction.</param>
/// <param name="ImplementationNamespace">The namespace of the generated implementation.</param>
/// <param name="ImplementationAccessibility">The accessibility keyword of the generated implementation.</param>
/// <param name="ImplementationName">The name of the generated implementation.</param>
/// <param name="Properties">The abstracted properties, with their documentation comment and both signatures.</param>
/// <param name="Methods">The abstracted methods, with their documentation comment, signature and delegating call.</param>
internal sealed record AbstractionRequest(
	string AbstractionNamespace,
	string AbstractionAccessibility,
	string AbstractionName,
	string ImplementationNamespace,
	string ImplementationAccessibility,
	string ImplementationName,
	EquatableArray<(string Comment, string AbstractionSignature, string ImplementationSignature)> Properties,
	EquatableArray<(string Comment, string Signature, string Using)> Methods
	);
