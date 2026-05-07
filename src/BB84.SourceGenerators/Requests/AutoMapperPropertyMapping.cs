// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents a single property-to-property mapping between a source and target type.
/// </summary>
/// <param name="TargetProperty"> Gets or sets the name of the property on the target type. </param>
/// <param name="SourceProperty"> Gets or sets the name of the property on the source type. </param>
/// <param name="RequiresNullForgiving"> Gets or sets a value indicating whether a null-forgiving operator is needed. </param>
internal sealed record AutoMapperPropertyMapping(
	string TargetProperty,
	string SourceProperty,
	bool RequiresNullForgiving
	);
