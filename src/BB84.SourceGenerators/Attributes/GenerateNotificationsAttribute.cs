// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.ComponentModel;

namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>class</c> should have properties with changing and changed
/// notifications generated for its fields. The generated code will use the following interfaces:
/// <list type="bullet">
/// <item>The <see cref="INotifyPropertyChanging"/> interface for property changing notifications.</item>
/// <item>The <see cref="INotifyPropertyChanged"/> interface for property changed notifications.</item>
/// </list>
/// </summary>
/// <param name="isChanged">
/// Indicates whether a boolean property named <c>IsChanged</c> should be generated.
/// </param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateNotificationsAttribute(bool isChanged = false) : Attribute
{
	/// <summary>
	/// Indicates whether a boolean property named <c>IsChanged</c> should be generated.
	/// </summary>
	public bool IsChanged => isChanged;
}
