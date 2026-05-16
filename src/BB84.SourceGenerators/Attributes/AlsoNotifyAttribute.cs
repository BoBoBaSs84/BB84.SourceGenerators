// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Specifies additional property names that should be notified when the decorated field changes.
/// This attribute is used in conjunction with the <see cref="GenerateNotificationsAttribute"/> to
/// raise property changed and property changing notifications for related properties.
/// </summary>
/// <param name="propertyNames">The names of the additional properties to notify.</param>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class AlsoNotifyAttribute(params string[] propertyNames) : Attribute
{
	/// <summary>
	/// Gets the names of the additional properties to notify.
	/// </summary>
	public string[] PropertyNames => propertyNames;
}
