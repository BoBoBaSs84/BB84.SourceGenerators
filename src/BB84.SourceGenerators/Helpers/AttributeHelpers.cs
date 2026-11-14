// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Provides helper methods to find attributes and read their arguments.
/// </summary>
/// <remarks>
/// All lookups work on the bound <see cref="AttributeData"/> and compare the fully qualified
/// attribute type name, so they are independent of how the attribute is written in source
/// (qualified, aliased, with or without the <c>Attribute</c> suffix).
/// </remarks>
internal static class AttributeHelpers
{
	/// <summary>
	/// Determines whether the attribute is of the type with the given fully qualified name.
	/// </summary>
	/// <param name="attribute">The attribute data to inspect.</param>
	/// <param name="attributeName">The fully qualified name of the attribute type.</param>
	/// <returns><see langword="true"/> if the attribute matches; otherwise, <see langword="false"/>.</returns>
	internal static bool IsAttribute(AttributeData attribute, string attributeName)
		=> attribute.AttributeClass?.ToDisplayString() == attributeName;

	/// <summary>
	/// Gets the first attribute on a symbol whose type has the given fully qualified name.
	/// </summary>
	/// <param name="symbol">The symbol to inspect.</param>
	/// <param name="attributeName">The fully qualified name of the attribute type.</param>
	/// <returns>The attribute data, or <see langword="null"/> if the symbol has no such attribute.</returns>
	internal static AttributeData? GetAttribute(ISymbol symbol, string attributeName)
	{
		foreach (AttributeData attribute in symbol.GetAttributes())
		{
			if (IsAttribute(attribute, attributeName))
				return attribute;
		}

		return null;
	}

	/// <summary>
	/// Checks whether a symbol has an attribute with the given fully qualified name.
	/// </summary>
	/// <param name="symbol">The symbol to inspect.</param>
	/// <param name="attributeName">The fully qualified name of the attribute type.</param>
	/// <returns><see langword="true"/> if the attribute is present; otherwise, <see langword="false"/>.</returns>
	internal static bool HasAttribute(ISymbol symbol, string attributeName)
		=> GetAttribute(symbol, attributeName) is not null;

	/// <summary>
	/// Gets the value of a positional constructor argument from attribute data.
	/// </summary>
	/// <typeparam name="T">The expected type of the argument value.</typeparam>
	/// <param name="attribute">The attribute data to inspect.</param>
	/// <param name="index">The zero-based index of the constructor argument.</param>
	/// <param name="defaultValue">The default value to return if the argument is not found.</param>
	/// <returns>The argument value, or <paramref name="defaultValue"/> if not found.</returns>
	internal static T GetConstructorArgument<T>(AttributeData attribute, int index, T defaultValue)
		=> attribute.ConstructorArguments.Length > index && attribute.ConstructorArguments[index].Value is T value
			? value
			: defaultValue;

	/// <summary>
	/// Gets the value of a positional constructor argument from the attribute with the given fully
	/// qualified name on a symbol.
	/// </summary>
	/// <typeparam name="T">The expected type of the argument value.</typeparam>
	/// <param name="symbol">The symbol to inspect.</param>
	/// <param name="attributeName">The fully qualified name of the attribute type.</param>
	/// <param name="index">The zero-based index of the constructor argument.</param>
	/// <param name="defaultValue">The default value to return if the attribute or argument is not found.</param>
	/// <returns>The argument value, or <paramref name="defaultValue"/> if not found.</returns>
	internal static T GetConstructorArgument<T>(ISymbol symbol, string attributeName, int index, T defaultValue)
		=> GetAttribute(symbol, attributeName) is AttributeData attribute
			? GetConstructorArgument(attribute, index, defaultValue)
			: defaultValue;

	/// <summary>
	/// Gets the elements of an array-valued (e.g. <see langword="params"/>) constructor argument from attribute data.
	/// </summary>
	/// <param name="attribute">The attribute data to inspect.</param>
	/// <param name="index">The zero-based index of the constructor argument.</param>
	/// <returns>The array elements, or <see langword="null"/> if the argument is not an array.</returns>
	internal static ImmutableArray<TypedConstant>? GetConstructorArrayArgument(AttributeData attribute, int index)
		=> attribute.ConstructorArguments.Length > index && attribute.ConstructorArguments[index].Kind is TypedConstantKind.Array
			? attribute.ConstructorArguments[index].Values
			: null;

	/// <summary>
	/// Gets the single constructor argument of the first attribute on <paramref name="symbol"/> whose
	/// class matches <paramref name="attributeName"/> and has exactly one argument of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The expected type of the argument value.</typeparam>
	/// <param name="symbol">The symbol to inspect.</param>
	/// <param name="attributeName">The fully qualified name of the attribute.</param>
	/// <param name="value">When this method returns <see langword="true"/>, contains the argument value.</param>
	/// <returns><see langword="true"/> if a matching attribute was found; otherwise, <see langword="false"/>.</returns>
	internal static bool TryGetSingleCtorArg<T>(ISymbol symbol, string attributeName, out T value)
	{
		foreach (AttributeData attributeData in symbol.GetAttributes())
		{
			if (IsAttribute(attributeData, attributeName)
				&& attributeData.ConstructorArguments.Length == 1
				&& attributeData.ConstructorArguments[0].Value is T typedValue)
			{
				value = typedValue;
				return true;
			}
		}

		value = default!;
		return false;
	}

	/// <summary>
	/// Gets the value of a named argument from attribute data.
	/// </summary>
	/// <typeparam name="T">The expected type of the argument value.</typeparam>
	/// <param name="attribute">The attribute data to inspect.</param>
	/// <param name="name">The name of the named argument.</param>
	/// <param name="defaultValue">The default value to return if the argument is not found.</param>
	/// <returns>The argument value, or <paramref name="defaultValue"/> if not found.</returns>
	internal static T GetNamedArgument<T>(AttributeData attribute, string name, T defaultValue)
	{
		foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
		{
			if (argument.Key == name && argument.Value.Value is T value)
				return value;
		}

		return defaultValue;
	}

	/// <summary>
	/// Gets the elements of an array-valued named argument from attribute data.
	/// </summary>
	/// <param name="attribute">The attribute data to inspect.</param>
	/// <param name="name">The name of the named argument.</param>
	/// <returns>The array elements, or <see langword="null"/> if the argument is not set.</returns>
	internal static ImmutableArray<TypedConstant>? GetNamedArrayArgument(AttributeData attribute, string name)
	{
		foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
		{
			if (argument.Key == name && argument.Value.Kind is TypedConstantKind.Array)
				return argument.Value.Values;
		}

		return null;
	}

	/// <summary>
	/// Gets the set of excluded property names from the first (<see langword="params"/> <see cref="string"/>
	/// array) constructor argument of an attribute, e.g. <c>[GenerateToString("Secret", "Internal")]</c>.
	/// </summary>
	/// <param name="attribute">The attribute data.</param>
	/// <returns>A set of excluded property names; empty if the attribute has no such argument.</returns>
	internal static HashSet<string> GetExcludedProperties(AttributeData attribute)
		=> ToNameSet(GetConstructorArrayArgument(attribute, 0));

	/// <summary>
	/// Gets the set of excluded property names from a named <see cref="string"/> array argument of an attribute,
	/// e.g. <c>ExcludeProperties = new[] { "Secret" }</c>.
	/// </summary>
	/// <param name="attribute">The attribute data.</param>
	/// <param name="argumentName">The name of the named argument.</param>
	/// <returns>A set of excluded property names; empty if the argument is not set.</returns>
	internal static HashSet<string> GetExcludedProperties(AttributeData attribute, string argumentName)
		=> ToNameSet(GetNamedArrayArgument(attribute, argumentName));

	private static HashSet<string> ToNameSet(ImmutableArray<TypedConstant>? values)
	{
		HashSet<string> names = new(StringComparer.Ordinal);

		if (values is ImmutableArray<TypedConstant> elements)
		{
			foreach (TypedConstant element in elements)
			{
				if (element.Value is string name)
					_ = names.Add(name);
			}
		}

		return names;
	}
}
