// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.ComponentModel;

namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated <c>field</c> should have a property with changing and changed
/// notifications generated for its class. The generated code will use the following interfaces:
/// <list type="bullet">
/// <item>The <see cref="INotifyPropertyChanging"/> interface for property changing notifications.</item>
/// <item>The <see cref="INotifyPropertyChanged"/> interface for property changed notifications.</item>
/// </list>
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class GenerateNotificationAttribute : Attribute
{ }
