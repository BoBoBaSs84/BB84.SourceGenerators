// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a request to generate a command property for a method marked with the
/// <c>[ActionCommand]</c> attribute.
/// </summary>
/// <param name="ContainingType">The type that contains the command method.</param>
/// <param name="Signature">The signature of the command method.</param>
/// <param name="CommandName">The name of the generated command property.</param>
/// <param name="FieldName">The name of the generated backing field.</param>
/// <param name="CanExecuteExpression">The guard delegate expression, or <see langword="null"/> if none.</param>
/// <param name="ExceptionHandlerExpression">The exception handler delegate expression, or <see langword="null"/> if none.</param>
/// <param name="AllowConcurrentExecutions">Indicates whether concurrent executions are allowed.</param>
/// <param name="CancelCommandName">The name of the generated cancel command property, or <see langword="null"/> if none.</param>
internal sealed record ActionCommandRequest(
	ActionCommandContainingType ContainingType,
	ActionCommandSignature Signature,
	string CommandName,
	string FieldName,
	string? CanExecuteExpression,
	string? ExceptionHandlerExpression,
	bool AllowConcurrentExecutions,
	string? CancelCommandName
	);
