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
/// <param name="NamespaceName">The namespace of the containing type.</param>
/// <param name="TypeAccessibility">The accessibility keyword of the containing type.</param>
/// <param name="TypeName">The name of the containing type.</param>
/// <param name="IsRecord">Indicates whether the containing type is a record class.</param>
/// <param name="OuterClasses">The outer (nesting) classes, from outermost to innermost.</param>
/// <param name="MethodName">The name of the command method.</param>
/// <param name="IsStatic">Indicates whether the command method, and thus the command, is static.</param>
/// <param name="IsAsync">Indicates whether the command method returns a <c>Task</c>.</param>
/// <param name="ParameterType">The fully qualified command parameter type, or <see langword="null"/> if none.</param>
/// <param name="HasCancellationToken">Indicates whether the command method accepts a <c>CancellationToken</c>.</param>
/// <param name="CommandName">The name of the generated command property.</param>
/// <param name="FieldName">The name of the generated backing field.</param>
/// <param name="CanExecuteExpression">The guard delegate expression, or <see langword="null"/> if none.</param>
/// <param name="ExceptionHandlerExpression">The exception handler delegate expression, or <see langword="null"/> if none.</param>
/// <param name="AllowConcurrentExecutions">Indicates whether concurrent executions are allowed.</param>
/// <param name="CancelCommandName">The name of the generated cancel command property, or <see langword="null"/> if none.</param>
internal sealed record ActionCommandRequest(
	string NamespaceName,
	string TypeAccessibility,
	string TypeName,
	bool IsRecord,
	List<(string Accessibility, string Name)> OuterClasses,
	string MethodName,
	bool IsStatic,
	bool IsAsync,
	string? ParameterType,
	bool HasCancellationToken,
	string CommandName,
	string FieldName,
	string? CanExecuteExpression,
	string? ExceptionHandlerExpression,
	bool AllowConcurrentExecutions,
	string? CancelCommandName
	);