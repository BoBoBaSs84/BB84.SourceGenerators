// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Tests.Helpers;

using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Tests;

/// <summary>
/// Incrementality tests that assert the request step and the source outputs of every generator
/// are cached after an unrelated edit. The inputs are the diagnostic-free <see cref="SnapshotCases"/>:
/// results that carry diagnostics are re-reported on every run to resolve their source locations.
/// </summary>
[TestClass]
public sealed class IncrementalTests
{
#if NET
	[TestMethod]
	[DataRow("Plain")]
	[DataRow("Excludes")]
	[DataRow("Modifiers")]
	[DataRow("NullableAndGeneric")]
	[DataRow("OtherNamespace")]
	public void Abstraction(string caseName)
		=> Verify(SnapshotCases.Abstraction[caseName], new AbstractionGenerator(), TrackingNames.Abstraction);

	[TestMethod]
	[DataRow("Sync")]
	[DataRow("Async")]
	[DataRow("StaticAndNested")]
	public void ActionCommand(string caseName)
		=> Verify(SnapshotCases.ActionCommand[caseName], new ActionCommandGenerator(), TrackingNames.ActionCommand);

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("Nested")]
	[DataRow("NoAssemblyAttributes")]
	public void AssemblyInformation(string caseName)
		=> Verify(SnapshotCases.AssemblyInformation[caseName], new AssemblyInformationGenerator(), TrackingNames.AssemblyInformation);

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("NestedInStaticOuter")]
	[DataRow("Nullable")]
	public void AutoMapper(string caseName)
		=> Verify(SnapshotCases.AutoMapper[caseName], new AutoMapperGenerator(), TrackingNames.AutoMapper);

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("NestedAndNullable")]
	public void Builder(string caseName)
		=> Verify(SnapshotCases.Builder[caseName], new BuilderGenerator(), TrackingNames.Builder);

	[TestMethod]
	[DataRow("Class")]
	[DataRow("Collections")]
	[DataRow("Nested")]
	public void CloneableClass(string caseName)
		=> Verify(SnapshotCases.Cloneable[caseName], new CloneableGenerator(), TrackingNames.CloneableClass);

	[TestMethod]
	[DataRow("Struct")]
	[DataRow("NestedStruct")]
	public void CloneableStruct(string caseName)
		=> Verify(SnapshotCases.Cloneable[caseName], new CloneableGenerator(), TrackingNames.CloneableStruct);

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("MultipleInterfacesAndGenerics")]
	[DataRow("Modifiers")]
	[DataRow("Nested")]
	public void Decorator(string caseName)
		=> Verify(SnapshotCases.Decorator[caseName], new DecoratorGenerator(), TrackingNames.Decorator);

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("CustomKeys")]
	[DataRow("Nested")]
	public void Factory(string caseName)
		=> Verify(SnapshotCases.Factory[caseName], new FactoryGenerator(), TrackingNames.Factory);

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("SealedFinalizerAsync")]
	[DataRow("Ordered")]
	[DataRow("Nested")]
	public void Disposable(string caseName)
		=> Verify(SnapshotCases.Disposable[caseName], new DisposableGenerator(), TrackingNames.Disposable);

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("ExcludesAndInheritance")]
	[DataRow("Collections")]
	[DataRow("NestedAndNullable")]
	public void Equality(string caseName)
		=> Verify(SnapshotCases.Equality[caseName], new EqualityGenerator(), TrackingNames.Equality);

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("Options")]
	[DataRow("Collections")]
	[DataRow("ExcludesAndInheritance")]
	[DataRow("Records")]
	[DataRow("NestedAndNullable")]
	public void ToStringOverride(string caseName)
		=> Verify(SnapshotCases.ToStringOverride[caseName], new ToStringGenerator(), TrackingNames.ToStringOverride);

	[TestMethod]
	[DataRow("Plain")]
	[DataRow("Descriptions")]
	[DataRow("FlagsAndUnderlyingType")]
	[DataRow("Nested")]
	public void EnumeratorExtensions(string caseName)
		=> Verify(SnapshotCases.EnumeratorExtensions[caseName], new EnumeratorExtensionsGenerator(), TrackingNames.EnumeratorExtensions);

	[TestMethod]
	[DataRow("AllTypes")]
	[DataRow("NestedSections")]
	[DataRow("EnumsAndFlags")]
	[DataRow("Collections")]
	[DataRow("CommentsAndNames")]
	public void IniFile(string caseName)
		=> Verify(SnapshotCases.IniFile[caseName], new IniFileGenerator(), TrackingNames.IniFile);

	[TestMethod]
	[DataRow("Fields")]
	[DataRow("AlsoNotifyExcludeHasChanged")]
	[DataRow("PartialProperties")]
	[DataRow("PartialHooks")]
	[DataRow("RecordAndNested")]
	[DataRow("GeneratedInheritance")]
	public void Notifications(string caseName)
		=> Verify(SnapshotCases.Notifications[caseName], new NotificationsGenerator(), TrackingNames.Notifications);

	[TestMethod]
	[DataRow("Lazy")]
	[DataRow("NonLazy")]
	[DataRow("InterfaceAndNested")]
	public void Singleton(string caseName)
		=> Verify(SnapshotCases.Singleton[caseName], new SingletonGenerator(), TrackingNames.Singleton);

	[TestMethod]
	[DataRow("AllRules")]
	[DataRow("ErrorMessages")]
	[DataRow("CollectionsAndCustom")]
	[DataRow("InheritedAndNested")]
	public void Validator(string caseName)
		=> Verify(SnapshotCases.Validator[caseName], new ValidatorGenerator(), TrackingNames.Validator);

	private static void Verify(SnapshotCase snapshotCase, IIncrementalGenerator generator, params string[] trackedStepNames)
		=> IncrementalAssert.OutputsCached(snapshotCase.Source, snapshotCase.Nullable, generator, trackedStepNames);
#endif
}
