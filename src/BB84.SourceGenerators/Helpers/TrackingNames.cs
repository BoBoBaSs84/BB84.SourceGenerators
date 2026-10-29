// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Represents the tracking names of the incremental pipeline steps that produce the generator
/// requests, so that tests can assert the steps are cached across unrelated edits.
/// </summary>
internal static class TrackingNames
{
	internal const string Abstraction = nameof(Abstraction);
	internal const string ActionCommand = nameof(ActionCommand);
	internal const string AssemblyInformation = nameof(AssemblyInformation);
	internal const string AutoMapper = nameof(AutoMapper);
	internal const string Builder = nameof(Builder);
	internal const string CloneableClass = nameof(CloneableClass);
	internal const string CloneableStruct = nameof(CloneableStruct);
	internal const string Decorator = nameof(Decorator);
	internal const string Disposable = nameof(Disposable);
	internal const string EnumeratorExtensions = nameof(EnumeratorExtensions);
	internal const string Equality = nameof(Equality);
	internal const string Factory = nameof(Factory);
	internal const string IniFile = nameof(IniFile);
	internal const string Notifications = nameof(Notifications);
	internal const string Singleton = nameof(Singleton);
	internal const string ToStringOverride = nameof(ToStringOverride);
	internal const string Validator = nameof(Validator);
}
