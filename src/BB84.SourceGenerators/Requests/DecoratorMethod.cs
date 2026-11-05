// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Requests;

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
