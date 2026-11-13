// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Contains names of framework types that the generators look up or compare against.
/// </summary>
/// <remarks>
/// Bare names (e.g. <see cref="List"/>) are used for prefix matching and emission,
/// <c>*Definition</c> names for exact matches against an <c>OriginalDefinition</c> display string,
/// and <c>*ShortName</c> names where the type is referenced without its namespace.
/// </remarks>
internal static class TypeNames
{
	internal const string ArgumentException = NamespaceNames.System + ".ArgumentException";
	internal const string ArgumentNullException = NamespaceNames.System + ".ArgumentNullException";
	internal const string Attribute = NamespaceNames.System + ".Attribute";
	internal const string AttributeTargets = NamespaceNames.System + ".AttributeTargets";
	internal const string AttributeUsage = NamespaceNames.System + ".AttributeUsage";
	internal const string AttributeUsageAttribute = NamespaceNames.System + ".AttributeUsageAttribute";
	internal const string CancellationToken = NamespaceNames.SystemThreading + ".CancellationToken";
	internal const string DescriptionAttribute = NamespaceNames.SystemComponentModel + ".DescriptionAttribute";
	internal const string Dictionary = NamespaceNames.SystemCollectionsGeneric + ".Dictionary";
	internal const string DictionaryDefinition = Dictionary + "<TKey, TValue>";
	internal const string Enumerable = NamespaceNames.SystemLinq + ".Enumerable";
	internal const string Exception = NamespaceNames.System + ".Exception";
	internal const string ExcludeFromCodeCoverage = NamespaceNames.SystemDiagnosticsCodeAnalysis + "." + ExcludeFromCodeCoverageShortName;
	internal const string ExcludeFromCodeCoverageShortName = "ExcludeFromCodeCoverage";
	internal const string FlagsAttribute = NamespaceNames.System + "." + FlagsAttributeShortName;
	internal const string FlagsAttributeShortName = "FlagsAttribute";
	internal const string IAsyncDisposable = NamespaceNames.System + "." + IAsyncDisposableShortName;
	internal const string IAsyncDisposableShortName = "IAsyncDisposable";
	internal const string ICloneableShortName = "ICloneable";
	internal const string IDisposableShortName = "IDisposable";
	internal const string IEnumerable = NamespaceNames.SystemCollectionsGeneric + ".IEnumerable";
	internal const string IEquatableShortName = "IEquatable";
	internal const string IFormattable = NamespaceNames.System + "." + IFormattableShortName;
	internal const string IFormattableShortName = "IFormattable";
	internal const string ImmutableArray = NamespaceNames.SystemCollectionsImmutable + ".ImmutableArray";
	internal const string ImmutableArrayDefinition = ImmutableArray + "<T>";
	internal const string INotifyPropertyChanged = NamespaceNames.SystemComponentModel + ".INotifyPropertyChanged";
	internal const string INotifyPropertyChanging = NamespaceNames.SystemComponentModel + ".INotifyPropertyChanging";
	internal const string IReadOnlyCollection = NamespaceNames.SystemCollectionsGeneric + ".IReadOnlyCollection";
	internal const string IReadOnlyCollectionDefinition = IReadOnlyCollection + "<T>";
	internal const string IReadOnlyDictionary = NamespaceNames.SystemCollectionsGeneric + ".IReadOnlyDictionary";
	internal const string IReadOnlyList = NamespaceNames.SystemCollectionsGeneric + ".IReadOnlyList";
	internal const string IReadOnlyListDefinition = IReadOnlyList + "<T>";
	internal const string KeyValuePair = NamespaceNames.SystemCollectionsGeneric + ".KeyValuePair";
	internal const string List = NamespaceNames.SystemCollectionsGeneric + ".List";
	internal const string ListDefinition = List + "<T>";
	internal const string ReadOnlyCollection = NamespaceNames.SystemCollectionsObjectModel + ".ReadOnlyCollection";
	internal const string ReadOnlyCollectionDefinition = ReadOnlyCollection + "<T>";
	internal const string StringBuilder = NamespaceNames.SystemText + ".StringBuilder";
	internal const string StringComparison = NamespaceNames.System + ".StringComparison";
	internal const string Task = NamespaceNames.SystemThreadingTasks + ".Task";
	internal const string Type = NamespaceNames.System + ".Type";
}
