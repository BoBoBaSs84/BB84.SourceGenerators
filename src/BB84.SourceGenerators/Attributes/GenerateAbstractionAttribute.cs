// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Represents an attribute that indicates a static class should have an abstraction generated for it.
/// </summary>
/// <param name="targetType">The type of the static class to generate an abstraction for.</param>
/// <param name="abstractionType">The type of the generated abstraction.</param>
/// <param name="implementationType">The type of the generated implementation.</param>
/// <param name="excludeMethods">The methods to exclude from the generated abstraction.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateAbstractionAttribute(Type targetType, Type abstractionType, Type implementationType, params string[] excludeMethods) : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GenerateAbstractionAttribute"/> class.
	/// </summary>
	/// <inheritdoc cref="GenerateAbstractionAttribute(Type, Type, Type, string[])"/>
	public GenerateAbstractionAttribute(Type targetType, Type abstractionType, Type implementationType) : this(targetType, abstractionType, implementationType, [])
	{	}

	/// <summary>
	/// Gets the type of the static class to generate an abstraction for.
	/// </summary>
	public Type TargetType => targetType;

	/// <summary>
	/// Gets the type of the generated abstraction.
	/// </summary>
	public Type AbstractionType => abstractionType;

	/// <summary>
	/// Gets the type of the generated implementation.
	/// </summary>
	public Type ImplementationType => implementationType;

	/// <summary>
	/// Gets the methods to exclude from the generated abstraction.
	/// </summary>
	public string[] ExcludeMethods => excludeMethods;
}
