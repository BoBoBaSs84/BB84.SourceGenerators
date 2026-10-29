// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Input;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace BB84.SourceGenerators.Tests.Helpers;

/// <summary>
/// Shared in-process generator driver used by snapshot and incrementality tests. It always
/// runs the <see cref="AttributeSourceGenerator"/> first, so the trigger attributes resolve.
/// </summary>
internal static class GeneratorDriverHarness
{
	/// <summary>
	/// The parse options used for every syntax tree created by the harness.
	/// </summary>
	internal static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

	/// <summary>
	/// The superset of metadata references needed by all generator inputs.
	/// </summary>
	internal static readonly ImmutableArray<MetadataReference> References = CreateReferences();

	/// <summary>
	/// Runs the given generators on the source with nullable annotations disabled.
	/// </summary>
	/// <param name="source">The C# source to compile.</param>
	/// <param name="generators">The generators to run after the attribute source generator.</param>
	/// <returns>The run result of the generator driver.</returns>
	internal static GeneratorDriverRunResult Run(string source, params IIncrementalGenerator[] generators)
		=> Run(CreateCompilation(source), generators);

	/// <summary>
	/// Runs the given generators on the source with the given nullable context.
	/// </summary>
	/// <param name="source">The C# source to compile.</param>
	/// <param name="nullable">The nullable context of the compilation.</param>
	/// <param name="generators">The generators to run after the attribute source generator.</param>
	/// <returns>The run result of the generator driver.</returns>
	internal static GeneratorDriverRunResult Run(string source, NullableContextOptions nullable, params IIncrementalGenerator[] generators)
		=> Run(CreateCompilation(source, nullable), generators);

	/// <summary>
	/// Runs the given generators on an existing compilation.
	/// </summary>
	/// <param name="compilation">The compilation to run the generators on.</param>
	/// <param name="generators">The generators to run after the attribute source generator.</param>
	/// <returns>The run result of the generator driver.</returns>
	internal static GeneratorDriverRunResult Run(CSharpCompilation compilation, params IIncrementalGenerator[] generators)
		=> CreateDriver(generators).RunGenerators(compilation).GetRunResult();

	/// <summary>
	/// Creates a compilation for the source using the shared references and parse options.
	/// </summary>
	/// <param name="source">The C# source to compile.</param>
	/// <param name="nullable">The nullable context of the compilation.</param>
	/// <param name="references">Optional references that replace the shared references.</param>
	/// <returns>The created compilation.</returns>
	internal static CSharpCompilation CreateCompilation(string source, NullableContextOptions nullable = NullableContextOptions.Disable, IEnumerable<MetadataReference>? references = null)
		=> CSharpCompilation.Create(
			assemblyName: "GeneratorDriverHarnessAssembly",
			syntaxTrees: [CSharpSyntaxTree.ParseText(source, ParseOptions)],
			references: references ?? References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullable));

	/// <summary>
	/// Creates a generator driver for the attribute source generator followed by the given generators.
	/// </summary>
	/// <param name="generators">The generators to run after the attribute source generator.</param>
	/// <param name="trackIncrementalGeneratorSteps">Whether incremental generator steps are tracked.</param>
	/// <returns>The created generator driver.</returns>
	internal static GeneratorDriver CreateDriver(IIncrementalGenerator[] generators, bool trackIncrementalGeneratorSteps = false)
		=> CSharpGeneratorDriver.Create(
			generators: [new AttributeSourceGenerator().AsSourceGenerator(), .. generators.Select(GeneratorExtensions.AsSourceGenerator)],
			additionalTexts: null,
			parseOptions: ParseOptions,
			optionsProvider: null,
			driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps));

	private static ImmutableArray<MetadataReference> CreateReferences()
	{
		Assembly[] assemblies =
		[
			typeof(object).Assembly,
			typeof(Attribute).Assembly,
			typeof(Enumerable).Assembly,
			typeof(List<>).Assembly,
			typeof(ImmutableArray<>).Assembly,
			typeof(Task).Assembly,
			typeof(IAsyncDisposable).Assembly,
			typeof(INotifyPropertyChanged).Assembly,
			typeof(DescriptionAttribute).Assembly,
			typeof(ICommand).Assembly,
			typeof(RequiredAttribute).Assembly,
			typeof(Regex).Assembly,
			typeof(ExcludeFromCodeCoverageAttribute).Assembly,
			Assembly.Load("System.Runtime"),
			Assembly.Load("netstandard"),
		];

		return [.. assemblies
			.Select(static a => a.Location)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(static location => MetadataReference.CreateFromFile(location))];
	}
}
