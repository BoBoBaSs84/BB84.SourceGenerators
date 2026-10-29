// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents the signature of a method marked with the <c>[ActionCommand]</c> attribute.
/// </summary>
/// <param name="MethodName">The name of the command method.</param>
/// <param name="IsStatic">Indicates whether the command method, and thus the command, is static.</param>
/// <param name="IsAsync">Indicates whether the command method returns a <c>Task</c>.</param>
/// <param name="ParameterType">The fully qualified command parameter type, or <see langword="null"/> if none.</param>
/// <param name="HasCancellationToken">Indicates whether the command method accepts a <c>CancellationToken</c>.</param>
internal sealed record ActionCommandSignature(
	string MethodName,
	bool IsStatic,
	bool IsAsync,
	string? ParameterType,
	bool HasCancellationToken
	);
