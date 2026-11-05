// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Describes a request to implement change notification for a class or record.
/// </summary>
/// <param name="Type">The class or record to extend.</param>
/// <param name="IsRecord">Indicates whether the type is a record class.</param>
/// <param name="IsSealed">Indicates whether the type is sealed.</param>
/// <param name="Options">The notification options.</param>
/// <param name="Fields">The notifying fields, one per declared variable.</param>
/// <param name="PartialProperties">The partial properties to implement.</param>
internal sealed record NotificationsRequest(
	TypeTargetInfo Type,
	bool IsRecord,
	bool IsSealed,
	NotificationsOptions Options,
	EquatableArray<NotificationsField> Fields,
	EquatableArray<NotificationsPartialProperty> PartialProperties
	);
