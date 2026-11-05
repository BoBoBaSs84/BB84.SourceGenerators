// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents an interface whose members are delegated to the inner instance.
/// </summary>
/// <param name="InnerAccess">The expression used to access the inner instance as this interface.</param>
/// <param name="Methods">The ordinary methods of the interface.</param>
/// <param name="Properties">The properties of the interface.</param>
/// <param name="Events">The event types and names of the interface.</param>
internal sealed record DecoratorInterface(
	string InnerAccess,
	EquatableArray<DecoratorMethod> Methods,
	EquatableArray<DecoratorProperty> Properties,
	EquatableArray<(string TypeName, string Name)> Events
	);
