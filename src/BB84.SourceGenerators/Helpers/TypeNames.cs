// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Contains names of framework types that the generators look up or compare against.
/// </summary>
internal static class TypeNames
{
	internal const string CancellationToken = "System.Threading.CancellationToken";
	internal const string DescriptionAttribute = "System.ComponentModel.DescriptionAttribute";
	internal const string Exception = "System.Exception";
	internal const string FlagsAttributeShortName = "FlagsAttribute";
	internal const string IAsyncDisposable = "System.IAsyncDisposable";
	internal const string IFormattable = "System.IFormattable";
	internal const string INotifyPropertyChanged = "System.ComponentModel.INotifyPropertyChanged";
	internal const string INotifyPropertyChanging = "System.ComponentModel.INotifyPropertyChanging";
	internal const string List = "System.Collections.Generic.List";
	internal const string Task = "System.Threading.Tasks.Task";
}
