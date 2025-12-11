// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that a wrapper class should be generated for the specified class.
/// </summary>
/// <remarks>
/// This attribute is applied to a class to specify that a wrapper should be generated for another
/// class. The wrapper class will provide functionality to interact with the specified class.
/// </remarks>
/// <param name="className">The name of the class for which the wrapper should be generated.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateWrapperAttribute(string className) : Attribute
{
	/// <summary>
	/// Gets the name of the class to generate the wrapper for.
	/// </summary>
	public string ClassName { get; } = className;
}
