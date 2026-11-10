// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace System.Runtime.CompilerServices
{
	/// <summary>
	/// Reserved to be used by the compiler for tracking metadata.
	/// Enables <c>record</c> types and <c>init</c> accessors on <c>netstandard2.0</c>.
	/// </summary>
	internal static class IsExternalInit
	{ }
}

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>
	/// Specifies that when a method returns <see cref="ReturnValue"/>, the parameter
	/// will not be <see langword="null"/> even if the corresponding type allows it.
	/// </summary>
	/// <param name="returnValue">
	/// The return value condition. If the method returns this value, the associated parameter will not be <see langword="null"/>.
	/// </param>
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute
	{
		/// <summary>
		/// Gets the return value condition.
		/// </summary>
		public bool ReturnValue { get; } = returnValue;
	}
}
