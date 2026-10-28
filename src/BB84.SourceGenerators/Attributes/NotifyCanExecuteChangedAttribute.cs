// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Specifies the commands whose <c>RaiseCanExecuteChanged</c> method should be called when the
/// decorated field or partial property changes. This attribute is used in conjunction with the
/// <see cref="GenerateNotificationsAttribute"/> and <see cref="ActionCommandAttribute"/>.
/// </summary>
/// <param name="commandNames">The names of the commands to notify.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class NotifyCanExecuteChangedAttribute(params string[] commandNames) : Attribute
{
	/// <summary>
	/// Gets the names of the commands to notify.
	/// </summary>
	public string[] CommandNames => commandNames;
}