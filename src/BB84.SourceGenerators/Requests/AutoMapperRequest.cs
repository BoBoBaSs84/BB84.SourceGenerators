// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Represents the information gathered from a partial method decorated with
/// <c>[GenerateAutoMapper]</c> needed to emit the mapping implementation.
/// </summary>
/// <param name="Namespace"> Gets or sets the namespace of the containing type. </param>
/// <param name="ContainingTypeAccessibility"> Gets or sets the accessibility keyword of the containing type. </param>
/// <param name="ContainingTypeName"> Gets or sets the name of the containing type. </param>
/// <param name="ContainingTypeIsStatic"> Gets or sets whether the containing type is static. </param>
/// <param name="OuterClasses"> Gets or sets the outer (nesting) class declarations. </param>
/// <param name="MethodAccessibility"> Gets or sets the accessibility keyword of the method. </param>
/// <param name="MethodIsStatic"> Gets or sets whether the method is static. </param>
/// <param name="MethodName"> Gets or sets the name of the method. </param>
/// <param name="ParameterName"> Gets or sets the parameter name. </param>
/// <param name="SourceTypeFullName"> Gets or sets the fully qualified source type name. </param>
/// <param name="TargetTypeFullName"> Gets or sets the fully qualified target (return) type name. </param>
/// <param name="TargetTypeDisplayName"> Gets or sets the display name of the target type for the object initializer. </param>
/// <param name="SourceIsNullable"> Gets or sets whether the source parameter is nullable. </param>
/// <param name="PropertyMappings"> Gets or sets the property mappings (target property name -> source property name). </param>
/// <param name="UnmappedProperties"> Gets or sets the names of target properties that could not be mapped to a source property. </param>
/// <param name="TypeMismatchProperties"> Gets or sets the type mismatch information for properties that were found but have incompatible types.
/// Each tuple contains (targetPropertyName, sourcePropertyName). </param>
/// <param name="TargetTypeName"> Gets or sets the display name of the target type (for diagnostic messages). </param>
/// <param name="FilePath"> Gets or sets the file path of the method declaration (for diagnostic location). </param>
/// <param name="MethodSpanStart"> Gets or sets the span start of the method identifier (for diagnostic location). </param>
/// <param name="MethodSpanEnd"> Gets or sets the span end of the method identifier (for diagnostic location). </param>
internal sealed record AutoMapperRequest(
	string Namespace,
	string ContainingTypeAccessibility,
	string ContainingTypeName,
	bool ContainingTypeIsStatic,
	List<(string Accessibility, string Name, bool IsStatic)> OuterClasses,
	string MethodAccessibility,
	bool MethodIsStatic,
	string MethodName,
	string ParameterName,
	string SourceTypeFullName,
	string TargetTypeFullName,
	string TargetTypeDisplayName,
	bool SourceIsNullable,
	ImmutableArray<AutoMapperPropertyMapping> PropertyMappings,
	ImmutableArray<string> UnmappedProperties,
	ImmutableArray<(string TargetProperty, string SourceProperty)> TypeMismatchProperties,
	string TargetTypeName,
	string FilePath,
	int MethodSpanStart,
	int MethodSpanEnd
	);
