// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Globalization;

using BB84.SourceGenerators.Tests.Helpers;

using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Tests;

/// <summary>
/// Golden snapshot tests that pin the exact generated output and diagnostics of every generator.
/// The cases live in <see cref="SnapshotCases"/>; the snapshots under <c>Snapshots/</c>.
/// Regenerate them with the environment variable <c>BB84_UPDATE_SNAPSHOTS=1</c>.
/// </summary>
/// <remarks>
/// The .NET Framework targets resolve a different set of runtime types (e.g. no <c>IAsyncDisposable</c>),
/// so the shared cases only run on .NET. Cases that need the .NET Framework reference set are
/// marked explicitly.
/// </remarks>
[TestClass]
public sealed class SnapshotTests
{
#if NET
	[TestMethod]
	[DataRow("Plain")]
	[DataRow("Excludes")]
	[DataRow("Modifiers")]
	[DataRow("NullableAndGeneric")]
	[DataRow("OtherNamespace")]
	public void Abstraction(string caseName)
		=> Verify(nameof(AbstractionGenerator), caseName, SnapshotCases.Abstraction, new AbstractionGenerator());

	[TestMethod]
	[DataRow("Sync")]
	[DataRow("Async")]
	[DataRow("StaticAndNested")]
	[DataRow("NotifyCanExecuteChanged")]
	[DataRow("UnsupportedSignatures")]
	[DataRow("InvalidOptions")]
	[DataRow("NameConflicts")]
	public void ActionCommand(string caseName)
		=> Verify(nameof(ActionCommandGenerator), caseName, SnapshotCases.ActionCommand, new NotificationsGenerator(), new ActionCommandGenerator());

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("Nested")]
	[DataRow("NoAssemblyAttributes")]
	public void AssemblyInformation(string caseName)
		=> Verify(nameof(AssemblyInformationGenerator), caseName, SnapshotCases.AssemblyInformation, new AssemblyInformationGenerator());

	[TestMethod]
	public void AttributeSource()
	{
		GeneratorDriverRunResult result = GeneratorDriverHarness.Run("namespace TestNamespace { }");

		Assert.HasCount(26, result.Results.Single().GeneratedSources);
		SnapshotAssert.Match(result, nameof(AttributeSourceGenerator), "All", includeAttributeSources: true);
	}

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("NestedInStaticOuter")]
	[DataRow("Nullable")]
	[DataRow("PropertyDiagnostics")]
	[DataRow("PartialDiagnostics")]
	public void AutoMapper(string caseName)
		=> Verify(nameof(AutoMapperGenerator), caseName, SnapshotCases.AutoMapper, new AutoMapperGenerator());

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("Empty")]
	[DataRow("NestedAndNullable")]
	public void Builder(string caseName)
		=> Verify(nameof(BuilderGenerator), caseName, SnapshotCases.Builder, new BuilderGenerator());

	[TestMethod]
	[DataRow("Class")]
	[DataRow("Collections")]
	[DataRow("Struct")]
	[DataRow("Record")]
	[DataRow("Nested")]
	[DataRow("NestedStruct")]
	public void Cloneable(string caseName)
		=> Verify(nameof(CloneableGenerator), caseName, SnapshotCases.Cloneable, new CloneableGenerator());

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("MultipleInterfacesAndGenerics")]
	[DataRow("Modifiers")]
	[DataRow("Nested")]
	[DataRow("NoInterface")]
	public void Decorator(string caseName)
		=> Verify(nameof(DecoratorGenerator), caseName, SnapshotCases.Decorator, new DecoratorGenerator());

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("SealedFinalizerAsync")]
	[DataRow("Ordered")]
	[DataRow("Nested")]
	[DataRow("Empty")]
	public void Disposable(string caseName)
		=> Verify(nameof(DisposableGenerator), caseName, SnapshotCases.Disposable, new DisposableGenerator());

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("Descriptions")]
	[DataRow("FlagsAndUnderlyingType")]
	[DataRow("Nested")]
	[DataRow("Empty")]
	public void EnumeratorExtensions(string caseName)
		=> Verify(nameof(EnumeratorExtensionsGenerator), caseName, SnapshotCases.EnumeratorExtensions, new EnumeratorExtensionsGenerator());

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("ExcludesAndInheritance")]
	[DataRow("Collections")]
	[DataRow("NestedAndNullable")]
	[DataRow("Record")]
	public void Equality(string caseName)
		=> Verify(nameof(EqualityGenerator), caseName, SnapshotCases.Equality, new EqualityGenerator());

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("CustomKeys")]
	[DataRow("Nested")]
	[DataRow("NoImplementations")]
	[DataRow("Diagnostics")]
	public void Factory(string caseName)
		=> Verify(nameof(FactoryGenerator), caseName, SnapshotCases.Factory, new FactoryGenerator());

	[TestMethod]
	[DataRow("AllTypes")]
	[DataRow("NestedSections")]
	[DataRow("EnumsAndFlags")]
	[DataRow("Collections")]
	[DataRow("CommentsAndNames")]
	[DataRow("Diagnostics")]
	public void IniFile(string caseName)
		=> Verify(nameof(IniFileGenerator), caseName, SnapshotCases.IniFile, new IniFileGenerator());

	[TestMethod]
	[DataRow("Fields")]
	[DataRow("AlsoNotifyExcludeHasChanged")]
	[DataRow("PartialProperties")]
	[DataRow("PartialHooks")]
	[DataRow("RecordAndNested")]
	[DataRow("GeneratedInheritance")]
	[DataRow("HandWrittenInheritance")]
	[DataRow("Diagnostics")]
	public void Notifications(string caseName)
		=> Verify(nameof(NotificationsGenerator), caseName, SnapshotCases.Notifications, new NotificationsGenerator());

	[TestMethod]
	[DataRow("Lazy")]
	[DataRow("NonLazy")]
	[DataRow("InterfaceAndNested")]
	[DataRow("Diagnostics")]
	public void Singleton(string caseName)
		=> Verify(nameof(SingletonGenerator), caseName, SnapshotCases.Singleton, new SingletonGenerator());

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("Options")]
	[DataRow("Collections")]
	[DataRow("ExcludesAndInheritance")]
	[DataRow("Records")]
	[DataRow("NestedAndNullable")]
	public void ToStringOverride(string caseName)
		=> Verify(nameof(ToStringGenerator), caseName, SnapshotCases.ToStringOverride, new ToStringGenerator());

	[TestMethod]
	[DataRow("AllRules")]
	[DataRow("ErrorMessages")]
	[DataRow("CollectionsAndCustom")]
	[DataRow("InheritedAndNested")]
	[DataRow("Diagnostics")]
	public void Validator(string caseName)
		=> Verify(nameof(ValidatorGenerator), caseName, SnapshotCases.Validator, new ValidatorGenerator());

	/// <summary>
	/// Runs the validator snapshot under a culture with a comma decimal separator, so that numeric
	/// attribute arguments (e.g. <c>[Range(0.5, 9.5)]</c>) must be emitted culture-invariantly.
	/// </summary>
	[TestMethod]
	public void ValidatorIsCultureInvariant()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo commaCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
		commaCulture.NumberFormat.NumberDecimalSeparator = ",";

		try
		{
			CultureInfo.CurrentCulture = commaCulture;
			Validator("AllRules");
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	private static void Verify(string generatorName, string caseName, IReadOnlyDictionary<string, SnapshotCase> cases, params IIncrementalGenerator[] generators)
	{
		SnapshotCase snapshotCase = cases[caseName];
		GeneratorDriverRunResult result = GeneratorDriverHarness.Run(snapshotCase.Source, snapshotCase.Nullable, generators);
		SnapshotAssert.Match(result, generatorName, caseName);
	}
#endif

	/// <summary>
	/// Runs the async disposal case against a minimal core library without <c>System.IAsyncDisposable</c>.
	/// The runtime's own core library cannot be used: Mono's <c>mscorlib</c> defines the interface,
	/// while the .NET Framework's does not, so the scenario would depend on the test host.
	/// </summary>
	[TestMethod]
	public void DisposableAsyncUnsupported()
	{
		GeneratorDriverRunResult result = GeneratorDriverHarness.Run(
			GeneratorDriverHarness.CreateCompilation(SnapshotCases.DisposableAsyncUnsupported, references: [MinimalCoreLibrary.Reference]),
			new DisposableGenerator());

		SnapshotAssert.Match(result, nameof(DisposableGenerator), "AsyncUnsupported");
	}
}
