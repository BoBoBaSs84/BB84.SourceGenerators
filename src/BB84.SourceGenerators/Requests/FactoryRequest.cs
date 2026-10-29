// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate a factory for a class marked with the
/// <c>[GenerateFactory]</c> attribute.
/// </summary>
/// <param name="Type">The factory class to extend.</param>
/// <param name="InterfaceFullName">The fully qualified name of the target interface.</param>
/// <param name="InterfaceShortName">The minimally qualified name of the target interface.</param>
/// <param name="Implementations">The fully qualified implementation type names and their keys, in discovery order.</param>
internal sealed record FactoryRequest(
	TypeTargetInfo Type,
	string InterfaceFullName,
	string InterfaceShortName,
	EquatableArray<(string TypeName, string Key)> Implementations
	);
