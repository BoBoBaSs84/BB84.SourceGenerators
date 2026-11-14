// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Models;

namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Describes a request to generate command line argument parsing for a class or record class marked
/// with the <c>[GenerateCommandArgument]</c> attribute.
/// </summary>
/// <param name="Type">The class or record class to extend.</param>
/// <param name="IsRecord">Indicates whether the type is a record class.</param>
/// <param name="AssemblyName">The name of the assembly, shown in the first line of the usage text.</param>
/// <param name="SetsRequiredMembers">Indicates whether the generated constructor must be marked with <c>[SetsRequiredMembers]</c>.</param>
/// <param name="NeedsEncodingHelper">Indicates whether an option is a <c>System.Text.Encoding</c> and the encoding helper must be emitted.</param>
/// <param name="Options">The options, in declaration order.</param>
internal sealed record CommandArgumentRequest(
	TypeTargetInfo Type,
	bool IsRecord,
	string AssemblyName,
	bool SetsRequiredMembers,
	bool NeedsEncodingHelper,
	EquatableArray<CommandArgumentOption> Options
	);
