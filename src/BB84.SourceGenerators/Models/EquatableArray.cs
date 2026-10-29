// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections;
using System.Collections.Immutable;

namespace BB84.SourceGenerators.Models;

/// <summary>
/// An immutable array with element-wise value equality, so that models holding collections
/// can be cached by the incremental generator pipeline.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <param name="array">The wrapped array.</param>
internal readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
	where T : IEquatable<T>
{
	private readonly ImmutableArray<T> _array = array;

	/// <summary>
	/// Gets an empty array.
	/// </summary>
	public static EquatableArray<T> Empty { get; } = new(ImmutableArray<T>.Empty);

	/// <summary>
	/// Gets the wrapped array, never in its default (uninitialized) state.
	/// </summary>
	public ImmutableArray<T> AsImmutableArray()
		=> _array.IsDefault ? ImmutableArray<T>.Empty : _array;

	/// <inheritdoc/>
	public int Count => AsImmutableArray().Length;

	/// <summary>
	/// Gets a value indicating whether the array has no elements.
	/// </summary>
	public bool IsEmpty => Count == 0;

	/// <inheritdoc/>
	public T this[int index] => AsImmutableArray()[index];

	/// <inheritdoc/>
	public bool Equals(EquatableArray<T> other)
	{
		ImmutableArray<T> left = AsImmutableArray();
		ImmutableArray<T> right = other.AsImmutableArray();

		if (left.Length != right.Length)
			return false;

		EqualityComparer<T> comparer = EqualityComparer<T>.Default;

		for (int i = 0; i < left.Length; i++)
		{
			if (!comparer.Equals(left[i], right[i]))
				return false;
		}

		return true;
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
		=> obj is EquatableArray<T> other && Equals(other);

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		EqualityComparer<T> comparer = EqualityComparer<T>.Default;
		int hash = 17;

		unchecked
		{
			foreach (T item in AsImmutableArray())
				hash = (hash * 31) + (item is null ? 0 : comparer.GetHashCode(item));
		}

		return hash;
	}

	/// <inheritdoc/>
	public IEnumerator<T> GetEnumerator()
		=> ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	/// <summary>
	/// Determines whether two arrays are equal element-wise.
	/// </summary>
	/// <param name="left">The first array.</param>
	/// <param name="right">The second array.</param>
	/// <returns><see langword="true"/> if the arrays are equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
		=> left.Equals(right);

	/// <summary>
	/// Determines whether two arrays differ element-wise.
	/// </summary>
	/// <param name="left">The first array.</param>
	/// <param name="right">The second array.</param>
	/// <returns><see langword="true"/> if the arrays differ; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
		=> !left.Equals(right);
}

/// <summary>
/// Provides conversions to <see cref="EquatableArray{T}"/>.
/// </summary>
internal static class EquatableArrayExtensions
{
	/// <summary>
	/// Wraps the elements in an <see cref="EquatableArray{T}"/>.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="items">The elements.</param>
	/// <returns>The equatable array.</returns>
	internal static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> items)
		where T : IEquatable<T>
		=> new(items.ToImmutableArray());

	/// <summary>
	/// Wraps the array in an <see cref="EquatableArray{T}"/>.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="array">The array.</param>
	/// <returns>The equatable array.</returns>
	internal static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> array)
		where T : IEquatable<T>
		=> new(array);
}
