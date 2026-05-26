// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Analyzers;

/// <summary>
/// Represents a collection of <see cref="DiagnosticDescriptor"/> instances used by the source generator analyzers
/// </summary>
internal static class DiagnosticDescriptors
{
	/// <summary>
	/// Represents a diagnostic error indicating that the [GenerateValidator] attribute has been applied to
	/// an abstract class, which is not supported.
	/// </summary>
	internal static readonly DiagnosticDescriptor AbstractClassDiagnostic = new(
		id: "BB84SG0001",
		title: "GenerateValidator cannot be applied to abstract classes",
		messageFormat: "The [GenerateValidator] attribute cannot be applied to abstract class '{0}'",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that the [GenerateSingleton] attribute has been applied to
	/// a class that cannot be parameterlessly instantiated due to required fields or properties.
	/// </summary>
	internal static readonly DiagnosticDescriptor SingletonRequiresParameterlessInitializationDiagnostic = new(
		id: "BB84SG0002",
		title: "GenerateSingleton requires parameterless initialization",
		messageFormat: "The [GenerateSingleton] attribute cannot be applied to class '{0}' because it contains required member '{1}' that prevents parameterless initialization",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that the [GenerateDecorator] attribute has been applied to
	/// a class that does not implement any interface.
	/// </summary>
	internal static readonly DiagnosticDescriptor DecoratorRequiresInterfaceDiagnostic = new(
		id: "BB84SG0003",
		title: "GenerateDecorator requires at least one interface",
		messageFormat: "The [GenerateDecorator] attribute cannot be applied to class '{0}' because it does not implement any interface",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that the [GenerateFactory] attribute has been applied
	/// with a type argument that is not an interface.
	/// </summary>
	internal static readonly DiagnosticDescriptor FactoryRequiresInterfaceTypeDiagnostic = new(
		id: "BB84SG0004",
		title: "GenerateFactory requires an interface type",
		messageFormat: "The [GenerateFactory] attribute on class '{0}' requires an interface type, but '{1}' is not an interface",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that duplicate factory keys were detected
	/// among implementations of the target interface.
	/// </summary>
	internal static readonly DiagnosticDescriptor FactoryDuplicateKeyDiagnostic = new(
		id: "BB84SG0005",
		title: "Duplicate factory key detected",
		messageFormat: "Duplicate factory key '{0}' detected among implementations of '{1}'",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic warning indicating that the [GenerateDisposable] attribute was applied
	/// with <c>async: true</c> but the target framework does not support <c>IAsyncDisposable</c>.
	/// </summary>
	internal static readonly DiagnosticDescriptor AsyncDisposableNotSupportedDiagnostic = new(
		id: "BB84SG0006",
		title: "IAsyncDisposable is not available in the target framework",
		messageFormat: "The [GenerateDisposable] attribute on class '{0}' requested async disposal, but 'IAsyncDisposable' is not available in the target framework. Async disposal code will not be generated.",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic warning indicating that a target property has no matching source property.
	/// </summary>
	internal static readonly DiagnosticDescriptor AutoMapperUnmappedPropertyDiagnostic = new(
		id: "BB84SG0007",
		title: "Unmapped target property in AutoMapper",
		messageFormat: "Target property '{0}' on type '{1}' has no matching source property and will not be mapped",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that a property type mismatch was detected between source and target.
	/// </summary>
	internal static readonly DiagnosticDescriptor AutoMapperTypeMismatchDiagnostic = new(
		id: "BB84SG0008",
		title: "Type mismatch in AutoMapper property mapping",
		messageFormat: "Property '{0}' on target type '{1}' cannot be assigned from source property '{2}' due to a type mismatch",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that the [GenerateAutoMapper] attribute was applied to a method
	/// whose containing type is not declared as partial.
	/// </summary>
	internal static readonly DiagnosticDescriptor AutoMapperRequiresPartialClassDiagnostic = new(
		id: "BB84SG0009",
		title: "GenerateAutoMapper requires a partial class",
		messageFormat: "The [GenerateAutoMapper] attribute on method '{0}' requires the containing class '{1}' to be declared as partial",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that the [GenerateAutoMapper] attribute was applied to a method
	/// that is not declared as partial.
	/// </summary>
	internal static readonly DiagnosticDescriptor AutoMapperRequiresPartialMethodDiagnostic = new(
		id: "BB84SG0010",
		title: "GenerateAutoMapper requires a partial method",
		messageFormat: "The [GenerateAutoMapper] attribute can only be applied to a partial method, but method '{0}' is not declared as partial",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that a property decorated with [GenerateIniFileSection]
	/// has a nullable type, which is not supported.
	/// </summary>
	internal static readonly DiagnosticDescriptor IniFileSectionNullablePropertyDiagnostic = new(
		id: "BB84SG0011",
		title: "IniFile section property must not be nullable",
		messageFormat: "The property '{0}' on class '{1}' decorated with [GenerateIniFileSection] must not be nullable",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that a property decorated with [GenerateIniFileSection]
	/// is not initialized.
	/// </summary>
	internal static readonly DiagnosticDescriptor IniFileSectionUninitializedPropertyDiagnostic = new(
		id: "BB84SG0012",
		title: "IniFile section property must be initialized",
		messageFormat: "The property '{0}' on class '{1}' decorated with [GenerateIniFileSection] must be initialized",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that a property decorated with [GenerateIniFileSection]
	/// does not have a public getter.
	/// </summary>
	internal static readonly DiagnosticDescriptor IniFileSectionNoPublicGetterDiagnostic = new(
		id: "BB84SG0013",
		title: "IniFile section property must have a public getter",
		messageFormat: "The property '{0}' on class '{1}' decorated with [GenerateIniFileSection] must have a public getter",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}
