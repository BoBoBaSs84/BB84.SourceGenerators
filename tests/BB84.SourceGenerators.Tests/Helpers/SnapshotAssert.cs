// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BB84.SourceGenerators.Tests.Helpers;

/// <summary>
/// Golden snapshot assertions for generator output. Each case stores every generated source as
/// <c>Snapshots/&lt;Generator&gt;/&lt;Case&gt;/&lt;hintName&gt;.snap</c> plus a <c>diagnostics.snap</c> dump.
/// Set the environment variable <c>BB84_UPDATE_SNAPSHOTS=1</c> to write the snapshots into the
/// source directory instead of asserting.
/// </summary>
internal static class SnapshotAssert
{
	private const string SnapshotsFolder = "Snapshots";
	private const string SnapshotExtension = ".snap";
	private const string DiagnosticsFileName = "diagnostics" + SnapshotExtension;
	private const string UpdateVariable = "BB84_UPDATE_SNAPSHOTS";

	/// <summary>
	/// Asserts that the run result matches the stored snapshots of the case.
	/// </summary>
	/// <param name="result">The generator driver run result.</param>
	/// <param name="generatorName">The generator name, used as the snapshot folder.</param>
	/// <param name="caseName">The case name, used as the snapshot sub folder.</param>
	/// <param name="includeAttributeSources">Whether the sources of the attribute source generator are compared.</param>
	/// <param name="callerFilePath">The caller file path, used to locate the source directory.</param>
	internal static void Match(GeneratorDriverRunResult result, string generatorName, string caseName, bool includeAttributeSources = false, [CallerFilePath] string callerFilePath = "")
	{
		foreach (GeneratorRunResult run in result.Results)
			Assert.IsNull(run.Exception, $"Generator '{run.Generator.GetGeneratorType().Name}' threw: {run.Exception}");

		SortedDictionary<string, string> actual = new(StringComparer.Ordinal);

		foreach (GeneratorRunResult run in result.Results)
		{
			if (!includeAttributeSources && run.Generator.GetGeneratorType() == typeof(AttributeSourceGenerator))
				continue;

			foreach (GeneratedSourceResult source in run.GeneratedSources)
				actual.Add(source.HintName + SnapshotExtension, Normalize(source.SourceText.ToString()));
		}

		actual.Add(DiagnosticsFileName, DumpDiagnostics(result.Diagnostics));

		if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
		{
			string sourceDirectory = Path.Combine(Path.GetDirectoryName(callerFilePath)!, SnapshotsFolder, generatorName, caseName);
			Write(sourceDirectory, actual);
			return;
		}

		string directory = Path.Combine(AppContext.BaseDirectory, SnapshotsFolder, generatorName, caseName);
		Assert.IsTrue(Directory.Exists(directory), $"Snapshot directory '{directory}' is missing. Run with {UpdateVariable}=1 to create it.");

		string[] expectedFiles = [.. Directory.GetFiles(directory, "*" + SnapshotExtension).Select(Path.GetFileName).OrderBy(static f => f, StringComparer.Ordinal)!];
		CollectionAssert.AreEqual(expectedFiles, actual.Keys.ToArray(), $"Generated files of '{generatorName}/{caseName}' differ from the snapshot.");

		foreach (KeyValuePair<string, string> entry in actual)
		{
			string expected = Normalize(File.ReadAllText(Path.Combine(directory, entry.Key)));
			Assert.AreEqual(expected, entry.Value, $"Snapshot '{generatorName}/{caseName}/{entry.Key}' differs.");
		}
	}

	private static string DumpDiagnostics(IEnumerable<Diagnostic> diagnostics)
	{
		StringBuilder sb = new();

		foreach (string line in diagnostics.Select(FormatDiagnostic).OrderBy(static l => l, StringComparer.Ordinal))
			sb.Append(line).Append('\n');

		return sb.ToString();
	}

	private static string FormatDiagnostic(Diagnostic diagnostic)
	{
		string location = "none";

		if (diagnostic.Location.IsInSource)
		{
			LinePositionSpan span = diagnostic.Location.GetLineSpan().Span;
			location = string.Format(CultureInfo.InvariantCulture, "{0}:{1}-{2}:{3}", span.Start.Line + 1, span.Start.Character + 1, span.End.Line + 1, span.End.Character + 1);
		}

		return $"{diagnostic.Id}|{diagnostic.Severity}|{location}|{diagnostic.GetMessage(CultureInfo.InvariantCulture)}";
	}

	private static void Write(string directory, SortedDictionary<string, string> files)
	{
		if (Directory.Exists(directory))
		{
			foreach (string stale in Directory.GetFiles(directory, "*" + SnapshotExtension))
				File.Delete(stale);
		}

		Directory.CreateDirectory(directory);

		foreach (KeyValuePair<string, string> entry in files)
			File.WriteAllText(Path.Combine(directory, entry.Key), entry.Value, new UTF8Encoding(false));
	}

	private static string Normalize(string text)
		=> text.Replace("\r\n", "\n");
}
