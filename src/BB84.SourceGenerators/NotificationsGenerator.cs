// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Analyzers;
using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Extensions;
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators;

/// <summary>
/// Responsible for generating notification properties for classes marked
/// with the <see cref="GenerateNotificationsAttribute"/> attribute.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class NotificationsGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateNotificationsAttribute>();

	private static readonly string[] FileUsings =
	[
		NamespaceNames.System,
		NamespaceNames.SystemComponentModel,
	];

	private const string PropertyChangedInterfaceName = TypeNames.INotifyPropertyChanged;
	private const string PropertyChangingInterfaceName = TypeNames.INotifyPropertyChanging;
	private const string RaisePropertyChangedMethodName = "RaisePropertyChanged";
	private const string RaisePropertyChangingMethodName = "RaisePropertyChanging";
	private const string HasChangedPropertyName = "HasChanged";

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<NotificationsRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassOrRecordClassNode, Transform, TrackingNames.Notifications, Execute);

	private static GeneratorResult<NotificationsRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not TypeDeclarationSyntax typeDeclaration || context.TargetSymbol is not INamedTypeSymbol typeSymbol)
			return null;

		TypeTargetInfo type = TypeTargetInfo.Create(typeDeclaration, typeSymbol);
		bool isRecord = typeDeclaration is RecordDeclarationSyntax;

		if (isRecord && ((RecordDeclarationSyntax)typeDeclaration).ParameterList is not null)
			return GeneratorResult<NotificationsRequest>.FromDiagnostics(type.CreateDiagnostic(DiagnosticDescriptors.NotificationsPositionalRecordNotSupportedDiagnostic, type.Name));

		List<DiagnosticInfo> diagnostics = [];

		if (BuildOptions(diagnostics, type, typeSymbol, context.SemanticModel.Compilation) is not NotificationOptions options)
			return GeneratorResult<NotificationsRequest>.FromDiagnostics([.. diagnostics]);

		if (!options.RaiseChanged && !options.RaiseChanging)
			return null;

		(EquatableArray<NotifyingField> fields, EquatableArray<NotifyingPartialProperty> partialProperties) = CollectMembers(diagnostics, typeDeclaration, typeSymbol, context.SemanticModel);

		bool isSealed = typeDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword);

		NotificationsRequest request = new(type, isRecord, isSealed, options, fields, partialProperties);

		return new GeneratorResult<NotificationsRequest>(request, diagnostics.ToEquatableArray());
	}

	/// <summary>
	/// Builds the notification options of the type from its attribute and its base type chain,
	/// adding a diagnostic when a base type lacks an accessible raise method.
	/// </summary>
	/// <param name="diagnostics">The list the diagnostics are added to.</param>
	/// <param name="type">The type description.</param>
	/// <param name="typeSymbol">The type symbol.</param>
	/// <param name="compilation">The compilation used to resolve base type members.</param>
	/// <returns>The notification options, or <see langword="null"/> when a diagnostic was added.</returns>
	private static NotificationOptions? BuildOptions(List<DiagnosticInfo> diagnostics, TypeTargetInfo type, INamedTypeSymbol typeSymbol, Compilation compilation)
	{
		bool generatePropertyChanged = GetPropertyChanged(typeSymbol);
		bool generatePropertyChanging = GetPropertyChanging(typeSymbol);
		bool generateHasChanged = GetHasChanged(typeSymbol);
		bool generatePartialMethods = GetPartialMethods(typeSymbol);

		InheritedFeature inheritedChanged = GetInheritedFeature(typeSymbol, compilation, PropertyChangedInterfaceName, RaisePropertyChangedMethodName, GetPropertyChanged);
		InheritedFeature inheritedChanging = GetInheritedFeature(typeSymbol, compilation, PropertyChangingInterfaceName, RaisePropertyChangingMethodName, GetPropertyChanging);

		foreach (InheritedFeature feature in new[] { inheritedChanged, inheritedChanging })
		{
			if (feature.MissingRaiseMethodIn is null)
				continue;

			diagnostics.Add(type.CreateDiagnostic(DiagnosticDescriptors.NotificationsBaseRaiseMethodMissingDiagnostic, type.Name, feature.MissingRaiseMethodIn.Name, feature.InterfaceName, feature.MethodName));
			return null;
		}

		bool inheritedHasChanged = GetInheritedHasChanged(typeSymbol, compilation);

		return new NotificationOptions(
			DeclareChanged: generatePropertyChanged && !inheritedChanged.IsProvided,
			DeclareChanging: generatePropertyChanging && !inheritedChanging.IsProvided,
			DeclareHasChanged: generateHasChanged && !inheritedHasChanged,
			RaiseChanged: generatePropertyChanged || inheritedChanged.IsProvided,
			RaiseChanging: generatePropertyChanging || inheritedChanging.IsProvided,
			SetHasChanged: generateHasChanged || inheritedHasChanged,
			ChangedSupportsParams: !inheritedChanged.IsProvided || inheritedChanged.SupportsParams,
			ChangingSupportsParams: !inheritedChanging.IsProvided || inheritedChanging.SupportsParams,
			PartialMethods: generatePartialMethods);
	}

	/// <summary>
	/// Collects the notifying fields and partial properties of the type together with the commands
	/// each of them notifies, adding diagnostics for invalid partial properties and unknown commands.
	/// </summary>
	/// <param name="diagnostics">The list the diagnostics are added to.</param>
	/// <param name="typeDeclaration">The type declaration syntax.</param>
	/// <param name="typeSymbol">The type symbol.</param>
	/// <param name="semanticModel">The semantic model of the type declaration.</param>
	/// <returns>The notifying fields, one per declared variable, and the partial properties to implement.</returns>
	private static (EquatableArray<NotifyingField> Fields, EquatableArray<NotifyingPartialProperty> PartialProperties) CollectMembers(List<DiagnosticInfo> diagnostics, TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol typeSymbol, SemanticModel semanticModel)
	{
		List<FieldDeclarationSyntax> fieldDeclarations = [.. typeDeclaration.Members
			.OfType<FieldDeclarationSyntax>()
			.Where(field => !HasAttribute(field, nameof(ExcludeFromNotificationAttribute)))];

		List<PartialPropertyInfo> partialProperties = GetPartialProperties(diagnostics, typeDeclaration, typeSymbol, semanticModel);

		HashSet<string> knownCommands = GetKnownCommandNames(typeSymbol);
		Dictionary<MemberDeclarationSyntax, List<string>> notifyCommands = [];
		foreach (MemberDeclarationSyntax member in fieldDeclarations.Cast<MemberDeclarationSyntax>().Concat(partialProperties.Select(static p => p.Syntax)))
			notifyCommands[member] = GetNotifyCanExecuteChangedCommands(diagnostics, member, knownCommands);

		List<NotifyingField> fields = [];
		foreach (FieldDeclarationSyntax fieldDeclaration in fieldDeclarations)
		{
			EquatableArray<string> alsoNotifyFor = GetAlsoNotifyForProperties(fieldDeclaration).ToEquatableArray();
			EquatableArray<string> commands = notifyCommands[fieldDeclaration].ToEquatableArray();
			string fieldType = $"{fieldDeclaration.Declaration.Type}";

			foreach (VariableDeclaratorSyntax variable in fieldDeclaration.Declaration.Variables)
			{
				string fieldName = variable.Identifier.Text;
				fields.Add(new NotifyingField(fieldName, fieldType, GeneratorHelpers.FieldNameToPropertyName(fieldName), alsoNotifyFor, commands));
			}
		}

		EquatableArray<NotifyingPartialProperty> properties = partialProperties
			.Select(property => new NotifyingPartialProperty(
				$"{property.Syntax.Modifiers}",
				property.Syntax.Identifier.Text,
				property.TypeName,
				property.FieldName,
				property.Symbol.Type.IsReferenceType && property.Symbol.Type.NullableAnnotation != NullableAnnotation.Annotated ? " = default!;" : ";",
				GetAccessorPrefix(property.Getter),
				GetAccessorPrefix(property.Setter),
				property.IsExcluded,
				property.IsExcluded ? EquatableArray<string>.Empty : GetAlsoNotifyForProperties(property.Syntax).ToEquatableArray(),
				notifyCommands[property.Syntax].ToEquatableArray()))
			.ToEquatableArray();

		return (fields.ToEquatableArray(), properties);
	}

	private static void Execute(SourceProductionContext context, NotificationsRequest request)
	{
		TypeTargetInfo type = request.Type;
		NotificationOptions options = request.Options;

		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.NotificationsGeneratorFullName, type.NamespaceName, type.OuterClasses, FileUsings);

		AppendClassStart(sb, type.Name, type.Accessibility, options, request.IsRecord);
		AppendProperties(sb, request.Fields, options);
		AppendPartialProperties(sb, request.PartialProperties, request.Fields.Count > 0, options);
		AppendRaiseMethods(sb, options.DeclareChanged, options.DeclareChanging, request.IsSealed);

		sb.CloseClassOrRecord(request.IsRecord);
		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, type.Name, null, sb);
	}

	private static bool GetPropertyChanged(INamedTypeSymbol namedTypeSymbol)
		=> GeneratorHelpers.GetConstructorArgumentValue(namedTypeSymbol, AttributeNames.TypeName, 0, true);

	private static bool GetPropertyChanging(INamedTypeSymbol namedTypeSymbol)
		=> GeneratorHelpers.GetConstructorArgumentValue(namedTypeSymbol, AttributeNames.TypeName, 1, true);

	private static bool GetHasChanged(INamedTypeSymbol namedTypeSymbol)
		=> GeneratorHelpers.GetConstructorArgumentValue(namedTypeSymbol, AttributeNames.TypeName, 2, false);

	private static bool GetPartialMethods(INamedTypeSymbol namedTypeSymbol)
		=> GeneratorHelpers.GetConstructorArgumentValue(namedTypeSymbol, AttributeNames.TypeName, 3, false);

	/// <summary>
	/// Determines whether the base type chain of the given type already provides the change notification
	/// described by <paramref name="interfaceName"/>, either through a base type decorated with the
	/// <see cref="GenerateNotificationsAttribute"/> or through a base type implementing the interface itself.
	/// </summary>
	/// <param name="typeSymbol">The type whose base types are inspected.</param>
	/// <param name="compilation">The compilation used to resolve the interface and to check accessibility.</param>
	/// <param name="interfaceName">The metadata name of the notification interface.</param>
	/// <param name="methodName">The name of the raise method the derived type calls.</param>
	/// <param name="isGenerated">Determines whether a decorated base type generates the notification.</param>
	/// <returns>The inherited feature description.</returns>
	private static InheritedFeature GetInheritedFeature(INamedTypeSymbol typeSymbol, Compilation compilation, string interfaceName, string methodName, Func<INamedTypeSymbol, bool> isGenerated)
	{
		INamedTypeSymbol? interfaceSymbol = compilation.GetTypeByMetadataName(interfaceName);

		for (INamedTypeSymbol? baseType = typeSymbol.BaseType; baseType is not null && baseType.SpecialType != SpecialType.System_Object; baseType = baseType.BaseType)
		{
			if (HasGenerateNotificationsAttribute(baseType))
			{
				if (isGenerated(baseType))
					return new InheritedFeature(interfaceName, methodName, IsProvided: true, SupportsParams: true, MissingRaiseMethodIn: null);
				continue;
			}

			if (interfaceSymbol is null || !baseType.Interfaces.Contains(interfaceSymbol, SymbolEqualityComparer.Default))
				continue;

			(bool hasSingle, bool hasParams) = ScanRaiseOverloads(baseType, typeSymbol, compilation, methodName);

			return hasSingle || hasParams
				? new InheritedFeature(interfaceName, methodName, IsProvided: true, SupportsParams: hasParams, MissingRaiseMethodIn: null)
				: new InheritedFeature(interfaceName, methodName, IsProvided: false, SupportsParams: false, MissingRaiseMethodIn: baseType);
		}

		return new InheritedFeature(interfaceName, methodName, IsProvided: false, SupportsParams: false, MissingRaiseMethodIn: null);
	}

	/// <summary>
	/// Scans the given base type and its own base types for raise method overloads that are
	/// accessible from the derived type.
	/// </summary>
	/// <param name="baseType">The base type to start the scan from.</param>
	/// <param name="typeSymbol">The derived type that calls the raise method.</param>
	/// <param name="compilation">The compilation used to check accessibility.</param>
	/// <param name="methodName">The name of the raise method.</param>
	/// <returns>Whether a <c>string</c> overload and a <c>string[]</c> overload were found.</returns>
	private static (bool HasSingle, bool HasParams) ScanRaiseOverloads(INamedTypeSymbol baseType, INamedTypeSymbol typeSymbol, Compilation compilation, string methodName)
	{
		bool hasSingle = false;
		bool hasParams = false;

		for (INamedTypeSymbol? current = baseType; current is not null; current = current.BaseType)
		{
			foreach (IMethodSymbol method in current.GetMembers(methodName).OfType<IMethodSymbol>())
			{
				if (method.IsStatic || !method.ReturnsVoid || method.Parameters.Length != 1
					|| !compilation.IsSymbolAccessibleWithin(method, typeSymbol, typeSymbol))
					continue;

				ITypeSymbol parameterType = method.Parameters[0].Type;
				if (parameterType.SpecialType == SpecialType.System_String)
					hasSingle = true;
				else if (parameterType is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_String })
					hasParams = true;
			}
		}

		return (hasSingle, hasParams);
	}

	/// <summary>
	/// Determines whether the base type chain of the given type already provides a settable
	/// <c>HasChanged</c> property.
	/// </summary>
	/// <param name="typeSymbol">The type whose base types are inspected.</param>
	/// <param name="compilation">The compilation used to check accessibility.</param>
	/// <returns><see langword="true"/> if a base type provides <c>HasChanged</c>; otherwise, <see langword="false"/>.</returns>
	private static bool GetInheritedHasChanged(INamedTypeSymbol typeSymbol, Compilation compilation)
	{
		for (INamedTypeSymbol? baseType = typeSymbol.BaseType; baseType is not null && baseType.SpecialType != SpecialType.System_Object; baseType = baseType.BaseType)
		{
			if (HasGenerateNotificationsAttribute(baseType) && GetHasChanged(baseType))
				return true;

			bool hasProperty = baseType.GetMembers(HasChangedPropertyName)
				.OfType<IPropertySymbol>()
				.Any(p => p.Type.SpecialType == SpecialType.System_Boolean
					&& p.SetMethod is not null
					&& compilation.IsSymbolAccessibleWithin(p.SetMethod, typeSymbol, typeSymbol));

			if (hasProperty)
				return true;
		}

		return false;
	}

	private static bool HasGenerateNotificationsAttribute(INamedTypeSymbol typeSymbol)
		=> typeSymbol.GetAttributes().Any(static a => a.AttributeClass?.Name == AttributeNames.TypeName);

	private static void AppendClassStart(SourceBuilder sb, string className, string accessibility, NotificationOptions options, bool isRecord)
	{
		List<string> interfaces = [];

		if (options.DeclareChanged)
			interfaces.Add("INotifyPropertyChanged");
		if (options.DeclareChanging)
			interfaces.Add("INotifyPropertyChanging");

		string? baseTypes = interfaces.Count > 0 ? string.Join(", ", interfaces) : null;

		sb.OpenClassOrRecord(isRecord, accessibility, className, baseTypes);

		if (options.DeclareChanged)
		{
			sb.AppendLine("/// <inheritdoc/>");
			sb.AppendLine("public event PropertyChangedEventHandler? PropertyChanged;");
			sb.AppendLine();
		}

		if (options.DeclareChanging)
		{
			sb.AppendLine("/// <inheritdoc/>");
			sb.AppendLine("public event PropertyChangingEventHandler? PropertyChanging;");
			sb.AppendLine();
		}

		if (options.DeclareHasChanged)
		{
			sb.AppendLine("private bool _hasChanged;");
			sb.AppendLine();
			sb.AppendSummary("Gets or sets a value indicating whether the object has been changed.");
			sb.AppendLine("public bool HasChanged");
			sb.OpenBrace();
			sb.AppendLine("get => _hasChanged;");
			sb.AppendLine("set");
			sb.OpenBrace();
			sb.AppendLine("if (_hasChanged != value)");
			sb.OpenBrace();
			if (options.RaiseChanging)
				sb.AppendLine("RaisePropertyChanging(nameof(HasChanged));");
			sb.AppendLine("_hasChanged = value;");
			if (options.RaiseChanged)
				sb.AppendLine("RaisePropertyChanged(nameof(HasChanged));");
			sb.CloseBrace();
			sb.CloseBrace();
			sb.CloseBrace();
			sb.AppendLine();
		}
	}

	private static void AppendProperties(SourceBuilder sb, EquatableArray<NotifyingField> fields, NotificationOptions options)
	{
		bool first = true;
		foreach ((string fieldName, string fieldType, string propertyName, EquatableArray<string> alsoNotifyFor, EquatableArray<string> commands) in fields)
		{
			if (!first)
				sb.AppendLine();
			first = false;

			sb.AppendLine($"public {fieldType} {propertyName}");
			sb.OpenBrace();
			sb.AppendLine($"get => {fieldName};");
			sb.AppendLine("set");
			AppendNotifyingSetterBody(sb, fieldName, propertyName, fieldType, alsoNotifyFor, commands, options);
			sb.CloseBrace();

			if (options.PartialMethods)
				AppendPartialMethodDeclarations(sb, propertyName, fieldType);
		}
	}

	private static void AppendPartialProperties(SourceBuilder sb, EquatableArray<NotifyingPartialProperty> properties, bool hasPrecedingMembers, NotificationOptions options)
	{
		bool first = !hasPrecedingMembers;
		foreach (NotifyingPartialProperty property in properties)
		{
			if (!first)
				sb.AppendLine();
			first = false;

			sb.AppendLine($"private {property.TypeName} {property.FieldName}{property.Initializer}");
			sb.AppendLine();
			sb.AppendLine($"{property.Modifiers} {property.TypeName} {property.Name}");
			sb.OpenBrace();
			sb.AppendLine($"{property.GetterPrefix}get => {property.FieldName};");

			if (property.IsExcluded)
			{
				sb.AppendLine($"{property.SetterPrefix}set => {property.FieldName} = value;");
			}
			else
			{
				sb.AppendLine($"{property.SetterPrefix}set");
				AppendNotifyingSetterBody(sb, property.FieldName, property.Name, property.TypeName, property.AlsoNotifyFor, property.Commands, options);
			}

			sb.CloseBrace();

			if (options.PartialMethods && !property.IsExcluded)
				AppendPartialMethodDeclarations(sb, property.Name, property.TypeName);
		}
	}

	private static void AppendNotifyingSetterBody(SourceBuilder sb, string fieldName, string propertyName, string typeName, EquatableArray<string> alsoNotifyFor, EquatableArray<string> commands, NotificationOptions options)
	{
		sb.OpenBrace();
		sb.AppendLine($"if ({fieldName} != value)");
		sb.OpenBrace();
		if (options.PartialMethods)
		{
			sb.AppendLine($"{typeName} __oldValue = {fieldName};");
			sb.AppendLine($"On{propertyName}Changing(value);");
			sb.AppendLine($"On{propertyName}Changing(__oldValue, value);");
		}
		if (options.RaiseChanging)
			AppendRaiseCall(sb, RaisePropertyChangingMethodName, propertyName, alsoNotifyFor, options.ChangingSupportsParams);
		sb.AppendLine($"{fieldName} = value;");
		if (options.PartialMethods)
		{
			sb.AppendLine($"On{propertyName}Changed(value);");
			sb.AppendLine($"On{propertyName}Changed(__oldValue, value);");
		}
		if (options.RaiseChanged)
			AppendRaiseCall(sb, RaisePropertyChangedMethodName, propertyName, alsoNotifyFor, options.ChangedSupportsParams);
		foreach (string command in commands)
			sb.AppendLine($"{command}.RaiseCanExecuteChanged();");
		if (options.SetHasChanged)
			sb.AppendLine("HasChanged = true;");
		sb.CloseBrace();
		sb.CloseBrace();
	}

	/// <summary>
	/// Appends the call of a raise method for the given property and its additionally notified properties,
	/// using a single <c>params</c> call when supported and one call per property otherwise.
	/// </summary>
	/// <param name="sb">The source builder.</param>
	/// <param name="methodName">The name of the raise method.</param>
	/// <param name="propertyName">The name of the changed property.</param>
	/// <param name="alsoNotifyFor">The names of the additionally notified properties.</param>
	/// <param name="supportsParams">Indicates whether a <c>params string[]</c> overload of the raise method is available.</param>
	private static void AppendRaiseCall(SourceBuilder sb, string methodName, string propertyName, EquatableArray<string> alsoNotifyFor, bool supportsParams)
	{
		if (alsoNotifyFor.Count == 0)
		{
			sb.AppendLine($"{methodName}(nameof({propertyName}));");
		}
		else if (supportsParams)
		{
			sb.AppendLine($"{methodName}(nameof({propertyName}), {string.Join(", ", alsoNotifyFor.Select(p => $"nameof({p})"))});");
		}
		else
		{
			foreach (string name in new[] { propertyName }.Concat(alsoNotifyFor))
				sb.AppendLine($"{methodName}(nameof({name}));");
		}
	}

	/// <summary>
	/// Appends the defining declarations of the <c>On[PropertyName]Changing</c> and
	/// <c>On[PropertyName]Changed</c> partial methods called from the property setter.
	/// </summary>
	/// <param name="sb">The source builder.</param>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="typeName">The type of the property.</param>
	private static void AppendPartialMethodDeclarations(SourceBuilder sb, string propertyName, string typeName)
	{
		sb.AppendLine();
		sb.AppendSummary($"Executes the logic for when <see cref=\"{propertyName}\"/> is about to change.");
		sb.AppendLine("/// <param name=\"value\">The new property value being set.</param>");
		sb.AppendLine($"partial void On{propertyName}Changing({typeName} value);");
		sb.AppendLine();
		sb.AppendSummary($"Executes the logic for when <see cref=\"{propertyName}\"/> is about to change.");
		sb.AppendLine("/// <param name=\"oldValue\">The previous property value that is being replaced.</param>");
		sb.AppendLine("/// <param name=\"newValue\">The new property value being set.</param>");
		sb.AppendLine($"partial void On{propertyName}Changing({typeName} oldValue, {typeName} newValue);");
		sb.AppendLine();
		sb.AppendSummary($"Executes the logic for when <see cref=\"{propertyName}\"/> has just changed.");
		sb.AppendLine("/// <param name=\"value\">The new property value that was set.</param>");
		sb.AppendLine($"partial void On{propertyName}Changed({typeName} value);");
		sb.AppendLine();
		sb.AppendSummary($"Executes the logic for when <see cref=\"{propertyName}\"/> has just changed.");
		sb.AppendLine("/// <param name=\"oldValue\">The previous property value that was replaced.</param>");
		sb.AppendLine("/// <param name=\"newValue\">The new property value that was set.</param>");
		sb.AppendLine($"partial void On{propertyName}Changed({typeName} oldValue, {typeName} newValue);");
	}

	private static string GetAccessorPrefix(AccessorDeclarationSyntax accessor)
		=> accessor.Modifiers.Count == 0 ? string.Empty : $"{accessor.Modifiers} ";

	/// <summary>
	/// Collects the partial property defining declarations of the type and adds diagnostics
	/// for those that cannot be implemented with change notification.
	/// </summary>
	/// <param name="diagnostics">The list the diagnostics are added to.</param>
	/// <param name="typeDeclaration">The type declaration syntax.</param>
	/// <param name="typeSymbol">The type symbol.</param>
	/// <param name="semanticModel">The semantic model of the type declaration.</param>
	/// <returns>The partial properties that can be implemented.</returns>
	private static List<PartialPropertyInfo> GetPartialProperties(List<DiagnosticInfo> diagnostics, TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol typeSymbol, SemanticModel semanticModel)
	{
		List<PartialPropertyInfo> properties = [];

		foreach (PropertyDeclarationSyntax property in typeDeclaration.Members.OfType<PropertyDeclarationSyntax>())
		{
			if (!property.Modifiers.Any(SyntaxKind.PartialKeyword) || !IsDefiningDeclaration(property))
				continue;

			if (semanticModel.GetDeclaredSymbol(property) is not IPropertySymbol symbol)
				continue;

			string propertyName = property.Identifier.Text;
			LocationInfo? location = LocationInfo.From(property.Identifier);

			if (property.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.NotificationsStaticPartialPropertyDiagnostic, location, propertyName));
				continue;
			}

			AccessorDeclarationSyntax? getter = property.AccessorList!.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
			AccessorDeclarationSyntax? setter = property.AccessorList.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));

			if (getter is null || setter is null)
			{
				diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.NotificationsPartialPropertyRequiresSetterDiagnostic, location, propertyName));
				continue;
			}

			string fieldName = GeneratorHelpers.PropertyNameToFieldName(propertyName);

			if (!typeSymbol.GetMembers(fieldName).IsEmpty)
			{
				diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.NotificationsBackingFieldConflictDiagnostic, location, propertyName, fieldName));
				continue;
			}

			string typeName = symbol.Type.ToPropertyTypeDisplayString();
			bool isExcluded = HasAttribute(property, nameof(ExcludeFromNotificationAttribute));

			properties.Add(new PartialPropertyInfo(property, symbol, getter, setter, typeName, fieldName, isExcluded));
		}

		return properties;
	}

	/// <summary>
	/// Determines whether the given partial property is a defining declaration, i.e. it has an
	/// accessor list and none of its accessors has an implementation.
	/// </summary>
	/// <param name="property">The property declaration syntax.</param>
	/// <returns><see langword="true"/> if the property is a defining declaration; otherwise, <see langword="false"/>.</returns>
	private static bool IsDefiningDeclaration(PropertyDeclarationSyntax property)
		=> property.AccessorList is not null
			&& property.ExpressionBody is null
			&& property.AccessorList.Accessors.All(static a => a.Body is null && a.ExpressionBody is null);

	private static void AppendRaiseMethods(SourceBuilder sb, bool propertyChanged, bool propertyChanging, bool isSealed)
	{
		string methodModifier = isSealed ? "private" : "protected virtual";

		if (propertyChanging)
			AppendRaiseMethodPair(sb, methodModifier, RaisePropertyChangingMethodName, "PropertyChanging", "is about to change.", "is changing", "are changing");

		if (propertyChanged)
			AppendRaiseMethodPair(sb, methodModifier, RaisePropertyChangedMethodName, "PropertyChanged", "has changed.", "has changed", "have changed");
	}

	/// <summary>
	/// Appends the single-name and the <c>params</c> overloads of a raise method for the given event.
	/// </summary>
	/// <param name="sb">The source builder.</param>
	/// <param name="methodModifier">The access and inheritance modifiers of the raise methods.</param>
	/// <param name="methodName">The name of the raise method.</param>
	/// <param name="eventName">The name of the raised event; its event args type is <c>[eventName]EventArgs</c>.</param>
	/// <param name="summaryTail">The wording that ends the summary, describing the change.</param>
	/// <param name="singleVerb">The verb phrase used in the single-name parameter documentation.</param>
	/// <param name="pluralVerb">The verb phrase used in the <c>params</c> parameter documentation.</param>
	private static void AppendRaiseMethodPair(SourceBuilder sb, string methodModifier, string methodName, string eventName, string summaryTail, string singleVerb, string pluralVerb)
	{
		sb.AppendLine();
		AppendRaiseMethodSummary(sb, eventName, summaryTail);
		sb.AppendLine($"/// <param name=\"propertyName\">The name of the property that {singleVerb}.</param>");
		sb.AppendLine($"{methodModifier} void {methodName}(string propertyName)");
		sb.Indent();
		sb.AppendLine($"=> {eventName}?.Invoke(this, new {eventName}EventArgs(propertyName));");
		sb.Outdent();
		sb.AppendLine();
		AppendRaiseMethodSummary(sb, eventName, summaryTail);
		sb.AppendLine($"/// <param name=\"propertyNames\">The names of the properties that {pluralVerb}.</param>");
		sb.AppendLine($"{methodModifier} void {methodName}(params string[] propertyNames)");
		sb.AppendLine("{");
		sb.Indent();
		sb.AppendLine("foreach(string propertyName in propertyNames)");
		sb.Indent();
		sb.AppendLine($"{methodName}(propertyName);");
		sb.Outdent();
		sb.Outdent();
		sb.AppendLine("}");
	}

	private static void AppendRaiseMethodSummary(SourceBuilder sb, string eventName, string summaryTail)
	{
		sb.AppendLine("/// <summary>");
		sb.AppendLine($"/// Raises the <see cref=\"{eventName}\"/> event to notify subscribers that a property");
		sb.AppendLine($"/// {summaryTail}");
		sb.AppendLine("/// </summary>");
	}

	private static bool HasAttribute(MemberDeclarationSyntax member, string attributeName)
		=> GeneratorHelpers.FindAttributes(member.AttributeLists, attributeName, GeneratorHelpers.StripAttributeSuffix(attributeName)).Any();

	private static List<string> GetAlsoNotifyForProperties(MemberDeclarationSyntax member)
		=> [.. GetNameArguments(member, nameof(AlsoNotifyAttribute)).Select(static argument => argument.Name)];

	/// <summary>
	/// Gets the commands referenced by the <see cref="NotifyCanExecuteChangedAttribute"/> of the given member,
	/// adding a diagnostic for each command that is neither an existing member nor a generated command.
	/// </summary>
	/// <param name="diagnostics">The list the diagnostics are added to.</param>
	/// <param name="member">The field or partial property declaration.</param>
	/// <param name="knownCommands">The names of the members and generated commands of the type.</param>
	/// <returns>The names of the known commands to notify.</returns>
	private static List<string> GetNotifyCanExecuteChangedCommands(List<DiagnosticInfo> diagnostics, MemberDeclarationSyntax member, HashSet<string> knownCommands)
	{
		List<string> commands = [];

		foreach ((string name, Location location) in GetNameArguments(member, nameof(NotifyCanExecuteChangedAttribute)))
		{
			if (knownCommands.Contains(name))
			{
				commands.Add(name);
				continue;
			}

			string memberName = member is FieldDeclarationSyntax field
				? string.Join(", ", field.Declaration.Variables.Select(static v => v.Identifier.Text))
				: ((PropertyDeclarationSyntax)member).Identifier.Text;

			diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.NotifyCanExecuteChangedUnknownCommandDiagnostic, LocationInfo.From(location), memberName, name));
		}

		return commands;
	}

	/// <summary>
	/// Gets the names of all members of the type and of the commands generated for its
	/// <see cref="ActionCommandAttribute"/> methods.
	/// </summary>
	/// <param name="typeSymbol">The type symbol.</param>
	/// <returns>The set of known command names.</returns>
	private static HashSet<string> GetKnownCommandNames(INamedTypeSymbol typeSymbol)
	{
		HashSet<string> names = new(StringComparer.Ordinal);

		foreach (ISymbol member in typeSymbol.GetMembers())
		{
			names.Add(member.Name);

			if (member is IMethodSymbol method && ActionCommandGenerator.GetCommandName(method) is string commandName)
				names.Add(commandName);
		}

		return names;
	}

	/// <summary>
	/// Gets the name arguments (<c>nameof(...)</c> expressions or string literals) of the given attribute on a member.
	/// </summary>
	/// <param name="member">The member declaration syntax.</param>
	/// <param name="attributeName">The full name of the attribute.</param>
	/// <returns>The names and their source locations.</returns>
	private static List<(string Name, Location Location)> GetNameArguments(MemberDeclarationSyntax member, string attributeName)
	{
		List<(string Name, Location Location)> names = [];
		string shortName = GeneratorHelpers.StripAttributeSuffix(attributeName);

		foreach (AttributeSyntax attribute in GeneratorHelpers.FindAttributes(member.AttributeLists, attributeName, shortName))
		{
			if (attribute.ArgumentList is null)
				continue;

			foreach (AttributeArgumentSyntax argument in attribute.ArgumentList.Arguments)
			{
				string argText = argument.Expression.ToString();

				if (argText.StartsWith("nameof(", StringComparison.Ordinal))
					argText = argText[7..^1];
				else if (argText.StartsWith("\"", StringComparison.Ordinal))
					argText = argText.Trim('"');

				if (!string.IsNullOrWhiteSpace(argText))
					names.Add((argText, argument.GetLocation()));
			}
		}

		return names;
	}

	/// <summary>
	/// Describes a partial property defining declaration that will be implemented by the generator.
	/// </summary>
	/// <param name="Syntax">The defining declaration syntax.</param>
	/// <param name="Symbol">The property symbol.</param>
	/// <param name="Getter">The <c>get</c> accessor of the defining declaration.</param>
	/// <param name="Setter">The <c>set</c> accessor of the defining declaration.</param>
	/// <param name="TypeName">The fully qualified property type name.</param>
	/// <param name="FieldName">The name of the generated backing field.</param>
	/// <param name="IsExcluded">Indicates whether the property is excluded from change notification.</param>
	private sealed record PartialPropertyInfo(
		PropertyDeclarationSyntax Syntax,
		IPropertySymbol Symbol,
		AccessorDeclarationSyntax Getter,
		AccessorDeclarationSyntax Setter,
		string TypeName,
		string FieldName,
		bool IsExcluded);

	/// <summary>
	/// Describes a request to implement change notification for a class or record.
	/// </summary>
	/// <param name="Type">The class or record to extend.</param>
	/// <param name="IsRecord">Indicates whether the type is a record class.</param>
	/// <param name="IsSealed">Indicates whether the type is sealed.</param>
	/// <param name="Options">The notification options.</param>
	/// <param name="Fields">The notifying fields, one per declared variable.</param>
	/// <param name="PartialProperties">The partial properties to implement.</param>
	private sealed record NotificationsRequest(
		TypeTargetInfo Type,
		bool IsRecord,
		bool IsSealed,
		NotificationOptions Options,
		EquatableArray<NotifyingField> Fields,
		EquatableArray<NotifyingPartialProperty> PartialProperties);

	/// <summary>
	/// Describes a field that is turned into a notifying property.
	/// </summary>
	/// <param name="FieldName">The name of the field.</param>
	/// <param name="FieldType">The type of the field, as written in source.</param>
	/// <param name="PropertyName">The name of the generated property.</param>
	/// <param name="AlsoNotifyFor">The names of the additionally notified properties.</param>
	/// <param name="Commands">The commands whose <c>CanExecuteChanged</c> is raised.</param>
	private sealed record NotifyingField(
		string FieldName,
		string FieldType,
		string PropertyName,
		EquatableArray<string> AlsoNotifyFor,
		EquatableArray<string> Commands);

	/// <summary>
	/// Describes a partial property that is implemented with change notification.
	/// </summary>
	/// <param name="Modifiers">The modifiers of the defining declaration, as written in source.</param>
	/// <param name="Name">The name of the property.</param>
	/// <param name="TypeName">The fully qualified property type name.</param>
	/// <param name="FieldName">The name of the generated backing field.</param>
	/// <param name="Initializer">The backing field initializer, including the terminating semicolon.</param>
	/// <param name="GetterPrefix">The modifiers of the <c>get</c> accessor, followed by a space, or an empty string.</param>
	/// <param name="SetterPrefix">The modifiers of the <c>set</c> accessor, followed by a space, or an empty string.</param>
	/// <param name="IsExcluded">Indicates whether the property is excluded from change notification.</param>
	/// <param name="AlsoNotifyFor">The names of the additionally notified properties.</param>
	/// <param name="Commands">The commands whose <c>CanExecuteChanged</c> is raised.</param>
	private sealed record NotifyingPartialProperty(
		string Modifiers,
		string Name,
		string TypeName,
		string FieldName,
		string Initializer,
		string GetterPrefix,
		string SetterPrefix,
		bool IsExcluded,
		EquatableArray<string> AlsoNotifyFor,
		EquatableArray<string> Commands);

	/// <summary>
	/// Describes whether a change notification is already provided by the base type chain.
	/// </summary>
	/// <param name="InterfaceName">The metadata name of the notification interface.</param>
	/// <param name="MethodName">The name of the raise method.</param>
	/// <param name="IsProvided">Indicates whether a base type provides the notification and a usable raise method.</param>
	/// <param name="SupportsParams">Indicates whether the inherited raise method has a <c>params string[]</c> overload.</param>
	/// <param name="MissingRaiseMethodIn">
	/// The base type that implements the interface without an accessible raise method; otherwise, <see langword="null"/>.
	/// </param>
	private sealed record InheritedFeature(
		string InterfaceName,
		string MethodName,
		bool IsProvided,
		bool SupportsParams,
		INamedTypeSymbol? MissingRaiseMethodIn);

	/// <summary>
	/// Describes which notification members are declared by the generated type and which are raised by its setters.
	/// </summary>
	/// <param name="DeclareChanged">Indicates whether the <c>PropertyChanged</c> event and raise methods are declared.</param>
	/// <param name="DeclareChanging">Indicates whether the <c>PropertyChanging</c> event and raise methods are declared.</param>
	/// <param name="DeclareHasChanged">Indicates whether the <c>HasChanged</c> property is declared.</param>
	/// <param name="RaiseChanged">Indicates whether setters raise the <c>PropertyChanged</c> event.</param>
	/// <param name="RaiseChanging">Indicates whether setters raise the <c>PropertyChanging</c> event.</param>
	/// <param name="SetHasChanged">Indicates whether setters set the <c>HasChanged</c> property.</param>
	/// <param name="ChangedSupportsParams">Indicates whether <c>RaisePropertyChanged</c> accepts multiple property names.</param>
	/// <param name="ChangingSupportsParams">Indicates whether <c>RaisePropertyChanging</c> accepts multiple property names.</param>
	/// <param name="PartialMethods">Indicates whether partial method hooks are declared and called.</param>
	private sealed record NotificationOptions(
		bool DeclareChanged,
		bool DeclareChanging,
		bool DeclareHasChanged,
		bool RaiseChanged,
		bool RaiseChanging,
		bool SetHasChanged,
		bool ChangedSupportsParams,
		bool ChangingSupportsParams,
		bool PartialMethods);
}
