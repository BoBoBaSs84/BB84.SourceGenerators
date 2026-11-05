// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Contains the accessibility and modifier keywords that the generators emit.
/// </summary>
internal static class Keywords
{
	internal const string Internal = "internal";
	internal const string Private = "private";
	internal const string Protected = "protected";
	internal const string ProtectedOverride = Protected + " override";
	internal const string ProtectedVirtual = Protected + " virtual";
	internal const string Public = "public";
	internal const string Static = "static";
}
