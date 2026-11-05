// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Describes a partial property that is implemented with change notification.
/// </summary>
/// <param name="Modifiers">The modifiers of the defining declaration, as written in source.</param>
/// <param name="Name">The name of the property.</param>
/// <param name="TypeName">The fully qualified property type name.</param>
/// <param name="FieldName">The name of the generated backing field.</param>
/// <param name="Initializer">The backing field initializer, including the terminating semicolon.</param>
/// <param name="GetterPrefix">The modifiers of the <c>get</c> accessor, followed by a space, or an empty string.</param>
/// <param name="SetterPrefix">The modifiers of the <c>set</c> accessor, followed by a space, or an empty string.</param>
/// <param name="IsExcluded">Indicates whether the property is excluded from change notification.</param>
/// <param name="AlsoNotifyFor">The names of the additionally notified properties.</param>
/// <param name="Commands">The commands whose <c>CanExecuteChanged</c> is raised.</param>
internal sealed record NotificationsPartialProperty(
	string Modifiers,
	string Name,
	string TypeName,
	string FieldName,
	string Initializer,
	string GetterPrefix,
	string SetterPrefix,
	bool IsExcluded,
	EquatableArray<string> AlsoNotifyFor,
	EquatableArray<string> Commands
	);
