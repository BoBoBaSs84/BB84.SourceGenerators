// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BB84.SourceGenerators.Tests.Helpers;

/// <summary>
/// Incrementality assertions for generators. The generator runs twice, the second time after an
/// unrelated edit to the compilation, and every tracked output must come from the cache.
/// </summary>
internal static class IncrementalAssert
{
	private const string UnrelatedSource = "namespace UnrelatedNamespace { internal static class UnrelatedType { } }";

	/// <summary>
	/// Asserts that all outputs of the given tracked steps and all source outputs of the generator
	/// are <see cref="IncrementalStepRunReason.Cached"/> or <see cref="IncrementalStepRunReason.Unchanged"/>
	/// after an unrelated edit.
	/// </summary>
	/// <param name="source">The C# source to compile.</param>
	/// <param name="generator">The generator under test.</param>
	/// <param name="trackedStepNames">The tracking names of the steps that must be cached.</param>
	internal static void OutputsCached(string source, IIncrementalGenerator generator, params string[] trackedStepNames)
		=> OutputsCached(source, NullableContextOptions.Disable, generator, trackedStepNames);

	/// <inheritdoc cref="OutputsCached(string, IIncrementalGenerator, string[])"/>
	/// <param name="source">The C# source to compile.</param>
	/// <param name="nullable">The nullable context of the compilation.</param>
	/// <param name="generator">The generator under test.</param>
	/// <param name="trackedStepNames">The tracking names of the steps that must be cached.</param>
	internal static void OutputsCached(string source, NullableContextOptions nullable, IIncrementalGenerator generator, params string[] trackedStepNames)
	{
		CSharpCompilation compilation = GeneratorDriverHarness.CreateCompilation(source, nullable);
		GeneratorDriver driver = GeneratorDriverHarness.CreateDriver([generator], trackIncrementalGeneratorSteps: true);

		driver = driver.RunGenerators(compilation);
		driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(UnrelatedSource, GeneratorDriverHarness.ParseOptions)));

		GeneratorRunResult run = driver.GetRunResult().Results.Single(r => r.Generator.GetGeneratorType() == generator.GetType());
		Assert.IsNull(run.Exception, $"Generator threw: {run.Exception}");

		foreach (string stepName in trackedStepNames)
		{
			Assert.IsTrue(run.TrackedSteps.TryGetValue(stepName, out ImmutableArray<IncrementalGeneratorRunStep> steps), $"Step '{stepName}' was not tracked.");
			AssertCached(stepName, steps);
		}

		foreach (KeyValuePair<string, ImmutableArray<IncrementalGeneratorRunStep>> output in run.TrackedOutputSteps)
			AssertCached(output.Key, output.Value);
	}

	private static void AssertCached(string stepName, ImmutableArray<IncrementalGeneratorRunStep> steps)
	{
		foreach ((object Value, IncrementalStepRunReason Reason) output in steps.SelectMany(static s => s.Outputs))
		{
			Assert.IsTrue(
				output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
				$"Step '{stepName}' produced '{output.Reason}' for '{output.Value}'.");
		}
	}
}
