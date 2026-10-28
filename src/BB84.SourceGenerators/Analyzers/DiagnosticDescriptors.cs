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

	/// <summary>
	/// Represents a diagnostic error indicating that the [GenerateNotifications] attribute has been applied to
	/// a positional record, which has no body-declared fields to convert into notification properties.
	/// </summary>
	internal static readonly DiagnosticDescriptor NotificationsPositionalRecordNotSupportedDiagnostic = new(
		id: "BB84SG0014",
		title: "GenerateNotifications cannot be applied to positional records",
		messageFormat: "The [GenerateNotifications] attribute cannot be applied to positional record '{0}' because it has no body-declared fields to convert into notification properties",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that a partial property in a [GenerateNotifications] type
	/// does not declare both a <c>get</c> and a <c>set</c> accessor.
	/// </summary>
	internal static readonly DiagnosticDescriptor NotificationsPartialPropertyRequiresSetterDiagnostic = new(
		id: "BB84SG0015",
		title: "GenerateNotifications partial property requires get and set accessors",
		messageFormat: "The partial property '{0}' must declare both a 'get' and a 'set' accessor to raise change notifications",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that the generated backing field for a partial property
	/// in a [GenerateNotifications] type collides with an existing member.
	/// </summary>
	internal static readonly DiagnosticDescriptor NotificationsBackingFieldConflictDiagnostic = new(
		id: "BB84SG0016",
		title: "GenerateNotifications backing field name conflict",
		messageFormat: "The backing field '{1}' for partial property '{0}' collides with an existing member",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that a static partial property has been declared in a
	/// [GenerateNotifications] type, which is not supported.
	/// </summary>
	internal static readonly DiagnosticDescriptor NotificationsStaticPartialPropertyDiagnostic = new(
		id: "BB84SG0017",
		title: "GenerateNotifications does not support static partial properties",
		messageFormat: "The static partial property '{0}' is not supported by the [GenerateNotifications] attribute",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that the [ActionCommand] attribute has been applied to a method
	/// whose containing type is not a partial class or record class.
	/// </summary>
	internal static readonly DiagnosticDescriptor ActionCommandContainingTypeNotPartialDiagnostic = new(
		id: "BB84SG0018",
		title: "Action command requires a partial class or record class",
		messageFormat: "The method '{0}' has an [ActionCommand] attribute, but its containing type '{1}' is not a partial class or record class",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that a method with the [ActionCommand] attribute has a
	/// signature that cannot be mapped to a command.
	/// </summary>
	internal static readonly DiagnosticDescriptor ActionCommandUnsupportedSignatureDiagnostic = new(
		id: "BB84SG0019",
		title: "Action command method has an unsupported signature",
		messageFormat: "The method '{0}' has an unsupported signature for [ActionCommand]; expected 'void M()', 'void M(T)', 'Task M([CancellationToken])' or 'Task M(T[, CancellationToken])'",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that the CanExecute member of an [ActionCommand] attribute
	/// was not found or is not compatible with the command.
	/// </summary>
	internal static readonly DiagnosticDescriptor ActionCommandInvalidCanExecuteDiagnostic = new(
		id: "BB84SG0020",
		title: "Action command CanExecute member is invalid",
		messageFormat: "The CanExecute member '{1}' of command method '{0}' was not found or is not a bool property, field or compatible method",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that the ExceptionHandler of an [ActionCommand] attribute
	/// was not found or does not have a compatible signature.
	/// </summary>
	internal static readonly DiagnosticDescriptor ActionCommandInvalidExceptionHandlerDiagnostic = new(
		id: "BB84SG0021",
		title: "Action command exception handler is invalid",
		messageFormat: "The ExceptionHandler '{1}' of command method '{0}' was not found or does not have the signature 'void M(Exception)'",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic warning indicating that an option that only applies to asynchronous commands
	/// has been set on a synchronous [ActionCommand] method.
	/// </summary>
	internal static readonly DiagnosticDescriptor ActionCommandAsyncOptionOnSyncCommandDiagnostic = new(
		id: "BB84SG0022",
		title: "Action command option only applies to asynchronous commands",
		messageFormat: "The option '{1}' of command method '{0}' only applies to asynchronous commands and is ignored",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that the command property or backing field generated for an
	/// [ActionCommand] method collides with an existing member.
	/// </summary>
	internal static readonly DiagnosticDescriptor ActionCommandNameConflictDiagnostic = new(
		id: "BB84SG0023",
		title: "Action command name conflict",
		messageFormat: "The command '{1}' generated for method '{0}' collides with an existing member",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic warning indicating that a [NotifyCanExecuteChanged] attribute references a
	/// command that is neither an existing member nor a generated command.
	/// </summary>
	internal static readonly DiagnosticDescriptor NotifyCanExecuteChangedUnknownCommandDiagnostic = new(
		id: "BB84SG0024",
		title: "NotifyCanExecuteChanged references an unknown command",
		messageFormat: "The member '{0}' references command '{1}' in [NotifyCanExecuteChanged], but no such member or generated command exists",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	/// <summary>
	/// Represents a diagnostic error indicating that a base type of a [GenerateNotifications] type implements a
	/// notification interface but does not expose an accessible raise method the generated setters can call.
	/// </summary>
	internal static readonly DiagnosticDescriptor NotificationsBaseRaiseMethodMissingDiagnostic = new(
		id: "BB84SG0025",
		title: "GenerateNotifications base type has no accessible raise method",
		messageFormat: "The base type '{1}' of '{0}' implements '{2}' but has no accessible '{3}(string)' method",
		category: "BB84.SourceGenerators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}