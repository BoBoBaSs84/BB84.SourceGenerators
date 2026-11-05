// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Requests;

/// <summary>
/// Describes which notification members are declared by the generated type and which are raised by its setters.
/// </summary>
/// <param name="DeclareChanged">Indicates whether the <c>PropertyChanged</c> event and raise methods are declared.</param>
/// <param name="DeclareChanging">Indicates whether the <c>PropertyChanging</c> event and raise methods are declared.</param>
/// <param name="DeclareHasChanged">Indicates whether the <c>HasChanged</c> property is declared.</param>
/// <param name="RaiseChanged">Indicates whether setters raise the <c>PropertyChanged</c> event.</param>
/// <param name="RaiseChanging">Indicates whether setters raise the <c>PropertyChanging</c> event.</param>
/// <param name="SetHasChanged">Indicates whether setters set the <c>HasChanged</c> property.</param>
/// <param name="ChangedSupportsParams">Indicates whether <c>RaisePropertyChanged</c> accepts multiple property names.</param>
/// <param name="ChangingSupportsParams">Indicates whether <c>RaisePropertyChanging</c> accepts multiple property names.</param>
/// <param name="PartialMethods">Indicates whether partial method hooks are declared and called.</param>
internal sealed record NotificationsOptions(
	bool DeclareChanged,
	bool DeclareChanging,
	bool DeclareHasChanged,
	bool RaiseChanged,
	bool RaiseChanging,
	bool SetHasChanged,
	bool ChangedSupportsParams,
	bool ChangingSupportsParams,
	bool PartialMethods
	);
