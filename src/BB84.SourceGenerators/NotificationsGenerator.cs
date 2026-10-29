// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Analyzers;
using BB84.SourceGenerators.Attributes;
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

	private static readonly SymbolDisplayFormat PropertyTypeFormat =
		SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

	private const string PropertyChangedInterfaceName = TypeNames.INotifyPropertyChanged;
	private const string PropertyChangingInterfaceName = TypeNames.INotifyPropertyChanging;
	private const string RaisePropertyChangedMethodName = "RaisePropertyChanged";
	private const string RaisePropertyChangingMethodName = "RaisePropertyChanging";
	private const string HasChangedPropertyName = "HasChanged";

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterClassOrRecordGenerator(context, AttributeNames.MetadataName, Execute);

	private void Execute(SourceProductionContext context, (TypeDeclarationSyntax TypeSyntax, SemanticModel SemanticModel)? input)
	{
		if (!GeneratorHelpers.TryCreateContext(input, out TypeGeneratorContext ctx))
			return;

		if (ctx.IsRecord && ctx.PositionalParameterList is not null)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				DiagnosticDescriptors.NotificationsPositionalRecordNotSupportedDiagnostic,
				ctx.ClassDeclaration.Identifier.GetLocation(),
				ctx.ClassName));
			return;
		}

		bool generatePropertyChanged = GetPropertyChanged(ctx.ClassSymbol);
		bool generatePropertyChanging = GetPropertyChanging(ctx.ClassSymbol);
		bool generateHasChanged = GetHasChanged(ctx.ClassSymbol);
		bool generatePartialMethods = GetPartialMethods(ctx.ClassSymbol);

		Compilation compilation = ctx.SemanticModel.Compilation;
		InheritedFeature inheritedChanged = GetInheritedFeature(ctx.ClassSymbol, compilation, PropertyChangedInterfaceName, RaisePropertyChangedMethodName, GetPropertyChanged);
		InheritedFeature inheritedChanging = GetInheritedFeature(ctx.ClassSymbol, compilation, PropertyChangingInterfaceName, RaisePropertyChangingMethodName, GetPropertyChanging);

		foreach (InheritedFeature feature in new[] { inheritedChanged, inheritedChanging })
		{
			if (feature.MissingRaiseMethodIn is null)
				continue;

			context.ReportDiagnostic(Diagnostic.Create(
				DiagnosticDescriptors.NotificationsBaseRaiseMethodMissingDiagnostic,
				ctx.ClassDeclaration.Identifier.GetLocation(),
				ctx.ClassName,
				feature.MissingRaiseMethodIn.Name,
				feature.InterfaceName,
				feature.MethodName));
			return;
		}

		bool inheritedHasChanged = GetInheritedHasChanged(ctx.ClassSymbol, compilation);

		NotificationOptions options = new(
			DeclareChanged: generatePropertyChanged && !inheritedChanged.IsProvided,
			DeclareChanging: generatePropertyChanging && !inheritedChanging.IsProvided,
			DeclareHasChanged: generateHasChanged && !inheritedHasChanged,
			RaiseChanged: generatePropertyChanged || inheritedChanged.IsProvided,
			RaiseChanging: generatePropertyChanging || inheritedChanging.IsProvided,
			SetHasChanged: generateHasChanged || inheritedHasChanged,
			ChangedSupportsParams: !inheritedChanged.IsProvided || inheritedChanged.SupportsParams,
			ChangingSupportsParams: !inheritedChanging.IsProvided || inheritedChanging.SupportsParams,
			PartialMethods: generatePartialMethods);

		if (!options.RaiseChanged && !options.RaiseChanging)
			return;

		List<FieldDeclarationSyntax> members = [.. ctx.ClassDeclaration.Members
			.OfType<FieldDeclarationSyntax>()
			.Where(field => !HasAttribute(field, nameof(ExcludeFromNotificationAttribute)))];

		List<PartialPropertyInfo> partialProperties = GetPartialProperties(context, ctx);

		HashSet<string> knownCommands = GetKnownCommandNames(ctx.ClassSymbol);
		Dictionary<MemberDeclarationSyntax, List<string>> notifyCommands = [];
		foreach (MemberDeclarationSyntax member in members.Cast<MemberDeclarationSyntax>().Concat(partialProperties.Select(static p => p.Syntax)))
			notifyCommands[member] = GetNotifyCanExecuteChangedCommands(context, member, knownCommands);

		bool isSealed = ctx.ClassDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword);

		SourceBuilder sb = new();

		sb.AppendAutoGeneratedWarning(GeneratorNames.NotificationsGeneratorFullName);
		sb.AppendNullableEnable();
		sb.AppendUsing(NamespaceNames.System);
		sb.AppendUsing(NamespaceNames.SystemComponentModel);
		sb.AppendLine();
		sb.OpenNamespace(ctx.NamespaceName);
		sb.OpenOuterClasses(ctx.OuterClasses);

		AppendClassStart(sb, ctx.ClassName, ctx.Accessibility, options, ctx.IsRecord);
		AppendProperties(sb, members, notifyCommands, options);
		AppendPartialProperties(sb, partialProperties, notifyCommands, members.Count > 0, options);
		AppendRaiseMethods(sb, options.DeclareChanged, options.DeclareChanging, isSealed);

		sb.CloseClassOrRecord(ctx.IsRecord);
		sb.CloseOuterClasses(ctx.OuterClasses);
		sb.CloseNamespace();

		string hintName = GeneratorHelpers.BuildHintName(ctx.OuterClasses, ctx.ClassName);

		context.AddSource(hintName, sb.ToString());
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

			return hasSingle || hasParams
				? new InheritedFeature(interfaceName, methodName, IsProvided: true, SupportsParams: hasParams, MissingRaiseMethodIn: null)
				: new InheritedFeature(interfaceName, methodName, IsProvided: false, SupportsParams: false, MissingRaiseMethodIn: baseType);
		}

		return new InheritedFeature(interfaceName, methodName, IsProvided: false, SupportsParams: false, MissingRaiseMethodIn: null);
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
			sb.AppendLine("/// <summary>");
			sb.AppendLine("/// Gets or sets a value indicating whether the object has been changed.");
			sb.AppendLine("/// </summary>");
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

	private static void AppendProperties(SourceBuilder sb, List<FieldDeclarationSyntax> members, Dictionary<MemberDeclarationSyntax, List<string>> notifyCommands, NotificationOptions options)
	{
		bool first = true;
		foreach (FieldDeclarationSyntax member in members)
		{
			List<string> alsoNotifyFor = GetAlsoNotifyForProperties(member);
			List<string> commands = notifyCommands[member];

			foreach (VariableDeclaratorSyntax variable in member.Declaration.Variables)
			{
				if (!first)
					sb.AppendLine();
				first = false;

				string fieldName = variable.Identifier.Text;
				string fieldType = $"{member.Declaration.Type}";
				string propertyName = FieldNameToPropertyName(fieldName);

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
	}

	private static void AppendPartialProperties(SourceBuilder sb, List<PartialPropertyInfo> properties, Dictionary<MemberDeclarationSyntax, List<string>> notifyCommands, bool hasPrecedingMembers, NotificationOptions options)
	{
		bool first = !hasPrecedingMembers;
		foreach (PartialPropertyInfo property in properties)
		{
			if (!first)
				sb.AppendLine();
			first = false;

			string initializer = property.Symbol.Type.IsReferenceType && property.Symbol.Type.NullableAnnotation != NullableAnnotation.Annotated
				? " = default!;"
				: ";";

			sb.AppendLine($"private {property.TypeName} {property.FieldName}{initializer}");
			sb.AppendLine();
			sb.AppendLine($"{property.Syntax.Modifiers} {property.TypeName} {property.Syntax.Identifier.Text}");
			sb.OpenBrace();
			sb.AppendLine($"{GetAccessorPrefix(property.Getter)}get => {property.FieldName};");

			if (property.IsExcluded)
			{
				sb.AppendLine($"{GetAccessorPrefix(property.Setter)}set => {property.FieldName} = value;");
			}
			else
			{
				List<string> alsoNotifyFor = GetAlsoNotifyForProperties(property.Syntax);
				sb.AppendLine($"{GetAccessorPrefix(property.Setter)}set");
				AppendNotifyingSetterBody(sb, property.FieldName, property.Syntax.Identifier.Text, property.TypeName, alsoNotifyFor, notifyCommands[property.Syntax], options);
			}

			sb.CloseBrace();

			if (options.PartialMethods && !property.IsExcluded)
				AppendPartialMethodDeclarations(sb, property.Syntax.Identifier.Text, property.TypeName);
		}
	}

	private static void AppendNotifyingSetterBody(SourceBuilder sb, string fieldName, string propertyName, string typeName, List<string> alsoNotifyFor, List<string> commands, NotificationOptions options)
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
	private static void AppendRaiseCall(SourceBuilder sb, string methodName, string propertyName, List<string> alsoNotifyFor, bool supportsParams)
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
		sb.AppendLine("/// <summary>");
		sb.AppendLine($"/// Executes the logic for when <see cref=\"{propertyName}\"/> is about to change.");
		sb.AppendLine("/// </summary>");
		sb.AppendLine("/// <param name=\"value\">The new property value being set.</param>");
		sb.AppendLine($"partial void On{propertyName}Changing({typeName} value);");
		sb.AppendLine();
		sb.AppendLine("/// <summary>");
		sb.AppendLine($"/// Executes the logic for when <see cref=\"{propertyName}\"/> is about to change.");
		sb.AppendLine("/// </summary>");
		sb.AppendLine("/// <param name=\"oldValue\">The previous property value that is being replaced.</param>");
		sb.AppendLine("/// <param name=\"newValue\">The new property value being set.</param>");
		sb.AppendLine($"partial void On{propertyName}Changing({typeName} oldValue, {typeName} newValue);");
		sb.AppendLine();
		sb.AppendLine("/// <summary>");
		sb.AppendLine($"/// Executes the logic for when <see cref=\"{propertyName}\"/> has just changed.");
		sb.AppendLine("/// </summary>");
		sb.AppendLine("/// <param name=\"value\">The new property value that was set.</param>");
		sb.AppendLine($"partial void On{propertyName}Changed({typeName} value);");
		sb.AppendLine();
		sb.AppendLine("/// <summary>");
		sb.AppendLine($"/// Executes the logic for when <see cref=\"{propertyName}\"/> has just changed.");
		sb.AppendLine("/// </summary>");
		sb.AppendLine("/// <param name=\"oldValue\">The previous property value that was replaced.</param>");
		sb.AppendLine("/// <param name=\"newValue\">The new property value that was set.</param>");
		sb.AppendLine($"partial void On{propertyName}Changed({typeName} oldValue, {typeName} newValue);");
	}

	private static string GetAccessorPrefix(AccessorDeclarationSyntax accessor)
		=> accessor.Modifiers.Count == 0 ? string.Empty : $"{accessor.Modifiers} ";

	/// <summary>
	/// Collects the partial property defining declarations of the type and reports diagnostics
	/// for those that cannot be implemented with change notification.
	/// </summary>
	/// <param name="context">The source production context used to report diagnostics.</param>
	/// <param name="ctx">The type generator context.</param>
	/// <returns>The partial properties that can be implemented.</returns>
	private static List<PartialPropertyInfo> GetPartialProperties(SourceProductionContext context, TypeGeneratorContext ctx)
	{
		List<PartialPropertyInfo> properties = [];

		foreach (PropertyDeclarationSyntax property in ctx.ClassDeclaration.Members.OfType<PropertyDeclarationSyntax>())
		{
			if (!property.Modifiers.Any(SyntaxKind.PartialKeyword) || !IsDefiningDeclaration(property))
				continue;

			if (ctx.SemanticModel.GetDeclaredSymbol(property) is not IPropertySymbol symbol)
				continue;

			string propertyName = property.Identifier.Text;
			Location location = property.Identifier.GetLocation();

			if (property.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NotificationsStaticPartialPropertyDiagnostic, location, propertyName));
				continue;
			}

			AccessorDeclarationSyntax? getter = property.AccessorList!.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
			AccessorDeclarationSyntax? setter = property.AccessorList.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));

			if (getter is null || setter is null)
			{
				context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NotificationsPartialPropertyRequiresSetterDiagnostic, location, propertyName));
				continue;
			}

			string fieldName = PropertyNameToFieldName(propertyName);

			if (!ctx.ClassSymbol.GetMembers(fieldName).IsEmpty)
			{
				context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NotificationsBackingFieldConflictDiagnostic, location, propertyName, fieldName));
				continue;
			}

			string typeName = symbol.Type.ToDisplayString(PropertyTypeFormat);
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
		{
			sb.AppendLine();
			sb.AppendLine("/// <summary>");
			sb.AppendLine("/// Raises the <see cref=\"PropertyChanging\"/> event to notify subscribers that a property");
			sb.AppendLine("/// is about to change.");
			sb.AppendLine("/// </summary>");
			sb.AppendLine("/// <param name=\"propertyName\">The name of the property that is changing.</param>");
			sb.AppendLine($"{methodModifier} void RaisePropertyChanging(string propertyName)");
			sb.Indent();
			sb.AppendLine("=> PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));");
			sb.Outdent();
			sb.AppendLine();
			sb.AppendLine("/// <summary>");
			sb.AppendLine("/// Raises the <see cref=\"PropertyChanging\"/> event to notify subscribers that a property");
			sb.AppendLine("/// is about to change.");
			sb.AppendLine("/// </summary>");
			sb.AppendLine("/// <param name=\"propertyNames\">The names of the properties that are changing.</param>");
			sb.AppendLine($"{methodModifier} void RaisePropertyChanging(params string[] propertyNames)");
			sb.AppendLine("{");
			sb.Indent();
			sb.AppendLine("foreach(string propertyName in propertyNames)");
			sb.Indent();
			sb.AppendLine("RaisePropertyChanging(propertyName);");
			sb.Outdent();
			sb.Outdent();
			sb.AppendLine("}");
		}

		if (propertyChanged)
		{
			sb.AppendLine();
			sb.AppendLine("/// <summary>");
			sb.AppendLine("/// Raises the <see cref=\"PropertyChanged\"/> event to notify subscribers that a property");
			sb.AppendLine("/// has changed.");
			sb.AppendLine("/// </summary>");
			sb.AppendLine("/// <param name=\"propertyName\">The name of the property that has changed.</param>");
			sb.AppendLine($"{methodModifier} void RaisePropertyChanged(string propertyName)");
			sb.Indent();
			sb.AppendLine("=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));");
			sb.Outdent();
			sb.AppendLine();
			sb.AppendLine("/// <summary>");
			sb.AppendLine("/// Raises the <see cref=\"PropertyChanged\"/> event to notify subscribers that a property");
			sb.AppendLine("/// has changed.");
			sb.AppendLine("/// </summary>");
			sb.AppendLine("/// <param name=\"propertyNames\">The names of the properties that have changed.</param>");
			sb.AppendLine($"{methodModifier} void RaisePropertyChanged(params string[] propertyNames)");
			sb.AppendLine("{");
			sb.Indent();
			sb.AppendLine("foreach(string propertyName in propertyNames)");
			sb.Indent();
			sb.AppendLine("RaisePropertyChanged(propertyName);");
			sb.Outdent();
			sb.Outdent();
			sb.AppendLine("}");
		}
	}

	/// <summary>
	/// Converts a field name to a property name by removing any leading underscores
	/// and capitalizing the first letter.
	/// </summary>
	/// <param name="fieldName">The field name to convert.</param>
	/// <returns>The converted property name.</returns>
	private static string FieldNameToPropertyName(string fieldName)
	{
		if (fieldName.StartsWith("_", StringComparison.OrdinalIgnoreCase))
			fieldName = fieldName[1..];
		return $"{char.ToUpperInvariant(fieldName[0])}{fieldName[1..]}";
	}

	/// <summary>
	/// Converts a property name to a backing field name by prefixing an underscore
	/// and lowercasing the first letter.
	/// </summary>
	/// <param name="propertyName">The property name to convert.</param>
	/// <returns>The converted field name.</returns>
	private static string PropertyNameToFieldName(string propertyName)
		=> $"_{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}";

	private static bool HasAttribute(MemberDeclarationSyntax member, string attributeName)
	{
		string shortName = GeneratorHelpers.StripAttributeSuffix(attributeName);

		foreach (AttributeListSyntax attributeList in member.AttributeLists)
		{
			foreach (AttributeSyntax attribute in attributeList.Attributes)
			{
				string name = attribute.Name.ToString();
				if (name == attributeName || name == shortName)
					return true;
			}
		}

		return false;
	}

	private static List<string> GetAlsoNotifyForProperties(MemberDeclarationSyntax member)
		=> [.. GetNameArguments(member, nameof(AlsoNotifyAttribute)).Select(static argument => argument.Name)];

	/// <summary>
	/// Gets the commands referenced by the <see cref="NotifyCanExecuteChangedAttribute"/> of the given member,
	/// reporting a diagnostic for each command that is neither an existing member nor a generated command.
	/// </summary>
	/// <param name="context">The source production context used to report diagnostics.</param>
	/// <param name="member">The field or partial property declaration.</param>
	/// <param name="knownCommands">The names of the members and generated commands of the type.</param>
	/// <returns>The names of the known commands to notify.</returns>
	private static List<string> GetNotifyCanExecuteChangedCommands(SourceProductionContext context, MemberDeclarationSyntax member, HashSet<string> knownCommands)
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

			context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NotifyCanExecuteChangedUnknownCommandDiagnostic, location, memberName, name));
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

		foreach (AttributeListSyntax attributeList in member.AttributeLists)
		{
			foreach (AttributeSyntax attribute in attributeList.Attributes)
			{
				string name = attribute.Name.ToString();
				if (name != attributeName && name != shortName)
					continue;

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
