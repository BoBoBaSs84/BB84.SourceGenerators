// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have properties with changing and changed
/// notifications generated for its fields. The generated code can use the following interfaces:
/// <list type="bullet">
/// <item>The <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface for property changed notifications.</item>
/// <item>The <see cref="System.ComponentModel.INotifyPropertyChanging"/> interface for property changing notifications.</item>
/// </list>
/// </summary>
/// <param name="propertyChanged">
/// Indicates whether the <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface should be implemented.
/// </param>
/// <param name="propertyChanging">
/// Indicates whether the <see cref="System.ComponentModel.INotifyPropertyChanging"/> interface should be implemented.
/// </param>
/// <param name="hasChanged">
/// Indicates whether a boolean property named <c>HasChanged</c> should be generated.
/// </param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateNotificationsAttribute(bool propertyChanged = true, bool propertyChanging = true, bool hasChanged = false) : Attribute
{
	/// <summary>
	/// Indicates whether the <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface should be implemented.
	/// </summary>
	public bool PropertyChanged => propertyChanged;

	/// <summary>
	/// Indicates whether the <see cref="System.ComponentModel.INotifyPropertyChanging"/> interface should be implemented.
	/// </summary>
	public bool PropertyChanging => propertyChanging;

	/// <summary>
	/// Indicates whether a boolean property named <c>HasChanged</c> should be generated.
	/// </summary>
	public bool HasChanged => hasChanged;
}
