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
}
