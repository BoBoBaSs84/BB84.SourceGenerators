// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Describes a request to generate environment variable access for a class or record class marked
/// with the <c>[GenerateEnvironmentVariables]</c> attribute.
/// </summary>
/// <param name="Type">The class or record class to extend.</param>
/// <param name="IsRecord">Indicates whether the type is a record class.</param>
/// <param name="Properties">The properties filled by the generated <c>Read</c> and <c>TryRead</c> methods; empty when no such methods are generated.</param>
/// <param name="PartialProperties">The partial properties implemented to access the environment variable directly.</param>
internal sealed record EnvironmentVariablesRequest(
	TypeTargetInfo Type,
	bool IsRecord,
	EquatableArray<EnvironmentVariablesProperty> Properties,
	EquatableArray<EnvironmentVariablesPartialProperty> PartialProperties
	);
