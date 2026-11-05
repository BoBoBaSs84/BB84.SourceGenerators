// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate the decorator pattern for a class marked with the
/// <c>[GenerateDecorator]</c> attribute.
/// </summary>
/// <param name="Type">The decorator class to extend.</param>
/// <param name="InnerTypeName">The type of the decorated inner instance (the first direct interface).</param>
/// <param name="Interfaces">All implemented interfaces, including inherited ones, in depth-first order.</param>
internal sealed record DecoratorRequest(
	TypeTargetInfo Type,
	string InnerTypeName,
	EquatableArray<DecoratorInterface> Interfaces
	);
