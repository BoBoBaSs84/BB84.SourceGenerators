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

/// <summary>
/// Represents a delegated interface method, with its signature parts already formatted.
/// </summary>
/// <param name="ReturnType">The return type.</param>
/// <param name="Name">The method name.</param>
/// <param name="TypeParameters">The type parameter list, including angle brackets, or an empty string.</param>
/// <param name="Constraints">The type parameter constraint clauses, with a leading space, or an empty string.</param>
/// <param name="Parameters">The parameter list.</param>
/// <param name="Arguments">The argument list of the delegating call.</param>
/// <param name="TypeArguments">The type argument list of the delegating call, or an empty string.</param>
/// <param name="IsVoid">Indicates whether the method returns <see langword="void"/>.</param>
internal sealed record DecoratorMethod(
	string ReturnType,
	string Name,
	string TypeParameters,
	string Constraints,
	string Parameters,
	string Arguments,
	string TypeArguments,
	bool IsVoid
	);

/// <summary>
/// Represents a delegated interface property.
/// </summary>
/// <param name="TypeName">The property type.</param>
/// <param name="Name">The property name.</param>
/// <param name="HasGetter">Indicates whether the property has a public getter.</param>
/// <param name="HasSetter">Indicates whether the property has a public setter.</param>
internal sealed record DecoratorProperty(
	string TypeName,
	string Name,
	bool HasGetter,
	bool HasSetter
	);
