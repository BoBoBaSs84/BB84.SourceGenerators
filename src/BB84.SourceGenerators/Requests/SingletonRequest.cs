// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate the singleton pattern for a class marked with the
/// <c>[GenerateSingleton]</c> attribute.
/// </summary>
/// <param name="Type">The class to extend.</param>
/// <param name="UseLazy">Indicates whether the instance is created lazily.</param>
/// <param name="InstanceTypeName">The type of the <c>Instance</c> property.</param>
internal sealed record SingletonRequest(
	TypeTargetInfo Type,
	bool UseLazy,
	string InstanceTypeName
	);
