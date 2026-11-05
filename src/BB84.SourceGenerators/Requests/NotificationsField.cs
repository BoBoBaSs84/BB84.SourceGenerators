// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Describes a field that is turned into a notifying property.
/// </summary>
/// <param name="FieldName">The name of the field.</param>
/// <param name="FieldType">The type of the field, as written in source.</param>
/// <param name="PropertyName">The name of the generated property.</param>
/// <param name="AlsoNotifyFor">The names of the additionally notified properties.</param>
/// <param name="Commands">The commands whose <c>CanExecuteChanged</c> is raised.</param>
internal sealed record NotificationsField(
	string FieldName,
	string FieldType,
	string PropertyName,
	EquatableArray<string> AlsoNotifyFor,
	EquatableArray<string> Commands
	);
