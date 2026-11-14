// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Describes a command line option filled by the generated <c>Parse</c> and <c>TryParse</c> methods.
/// </summary>
/// <param name="PropertyName">The name of the property.</param>
/// <param name="ParameterName">The name of the constructor parameter, escaped with <c>@</c> if it is a keyword.</param>
/// <param name="OptionName">The option name, including the leading <c>--</c> (e.g. <c>--filePath</c>).</param>
/// <param name="TypeName">The fully qualified property type, including the nullable annotation.</param>
/// <param name="Conversion">The conversion of the option value.</param>
/// <param name="IsFlag">Indicates whether the option is a flag that takes no value.</param>
/// <param name="IsMandatory">Indicates whether a missing option is an error.</param>
/// <param name="InitialValue">The expression the value starts with: the constant default, <c>null</c>, <c>false</c>, <c>default</c> or <c>default!</c>.</param>
/// <param name="DefaultText">The default value shown in the usage text, or <see langword="null"/> if none is shown.</param>
/// <param name="Summary">The XML summary of the property shown in the usage text, or <see langword="null"/> if none.</param>
internal sealed record CommandArgumentOption(
	string PropertyName,
	string ParameterName,
	string OptionName,
	string TypeName,
	ValueConversion Conversion,
	bool IsFlag,
	bool IsMandatory,
	string InitialValue,
	string? DefaultText,
	string? Summary
	);
