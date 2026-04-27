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
}
