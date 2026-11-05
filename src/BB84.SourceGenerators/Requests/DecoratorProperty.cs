// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Requests;

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
