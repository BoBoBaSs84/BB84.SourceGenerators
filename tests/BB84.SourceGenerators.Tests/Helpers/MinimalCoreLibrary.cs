// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace BB84.SourceGenerators.Tests.Helpers;

/// <summary>
/// A minimal core library compiled in memory, used where a test needs a reference set that does not
/// depend on the test host's runtime (for example, one without <c>System.IAsyncDisposable</c>).
/// </summary>
internal static class MinimalCoreLibrary
{
	private const string Source = """
		namespace System
		{
			public class Object { }
			public abstract class ValueType { }
			public abstract class Enum : ValueType { }
			public struct Void { }
			public struct Boolean { }
			public struct Char { }
			public struct Byte { }
			public struct Int16 { }
			public struct Int32 { }
			public struct Int64 { }
			public struct Single { }
			public struct Double { }
			public struct IntPtr { }
			public struct Nullable<T> where T : struct { }
			public sealed class String { }
			public abstract class Array { }
			public abstract class Delegate { }
			public abstract class MulticastDelegate : Delegate { }
			public abstract class Type { }
			public interface IDisposable { void Dispose(); }
			public enum StringComparison { CurrentCulture, CurrentCultureIgnoreCase, InvariantCulture, InvariantCultureIgnoreCase, Ordinal, OrdinalIgnoreCase }
			public abstract class Attribute { }
			public sealed class ParamArrayAttribute : Attribute { }
			public enum AttributeTargets { Assembly = 1, Module = 2, Class = 4, Struct = 8, Enum = 16, Constructor = 32, Method = 64, Property = 128, Field = 256, Event = 512, Interface = 1024, Parameter = 2048, Delegate = 4096, ReturnValue = 8192, GenericParameter = 16384, All = 32767 }
			public sealed class AttributeUsageAttribute : Attribute
			{
				public AttributeUsageAttribute(AttributeTargets validOn) { }
				public bool AllowMultiple { get; set; }
				public bool Inherited { get; set; }
			}
		}

		namespace System.Diagnostics.CodeAnalysis
		{
			public sealed class ExcludeFromCodeCoverageAttribute : System.Attribute { }
		}
		""";

	/// <summary>
	/// Gets the metadata reference to the minimal core library.
	/// </summary>
	internal static MetadataReference Reference { get; } = Create();

	private static MetadataReference Create()
	{
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "mscorlib",
			syntaxTrees: [CSharpSyntaxTree.ParseText(Source, GeneratorDriverHarness.ParseOptions)],
			references: [],
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		using MemoryStream stream = new();
		EmitResult result = compilation.Emit(stream);

		if (!result.Success)
			throw new InvalidOperationException($"The minimal core library failed to compile: {string.Join(Environment.NewLine, result.Diagnostics)}");

		return MetadataReference.CreateFromImage(stream.ToArray());
	}
}
