// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;

using BB84.SourceGenerators.Analyzers;
using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Extensions;
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Models;
using BB84.SourceGenerators.Requests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators;

/// <summary>
/// Responsible for generating the decorator pattern for classes marked
/// with the <see cref="GenerateDecoratorAttribute"/> attribute. The generated code
/// wraps an inner instance, delegates all interface members, and exposes them as
/// virtual methods for selective overriding.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DecoratorGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateDecoratorAttribute>();

	private static readonly string[] FileUsings =
	[
		NamespaceNames.System,
	];

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<DecoratorRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassNode, Transform, TrackingNames.Decorator, Execute);

	private static GeneratorResult<DecoratorRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not ClassDeclarationSyntax classDeclaration || context.TargetSymbol is not INamedTypeSymbol classSymbol)
			return null;

		TypeTargetInfo type = TypeTargetInfo.Create(classDeclaration, classSymbol);

		ImmutableArray<INamedTypeSymbol> interfaces = classSymbol.Interfaces;
		if (interfaces.Length == 0)
			return GeneratorResult<DecoratorRequest>.FromDiagnostics(type.CreateDiagnostic(DiagnosticDescriptors.DecoratorRequiresInterfaceDiagnostic, type.Name));

		// Use the first direct interface as the inner field type
		INamedTypeSymbol primaryInterface = interfaces[0];

		// Collect all interfaces that the primary interface already covers (itself + its full hierarchy)
		HashSet<string> primaryCovers = new(StringComparer.Ordinal)
		{
			primaryInterface.ToDisplayString()
		};
		foreach (INamedTypeSymbol inherited in primaryInterface.AllInterfaces)
			primaryCovers.Add(inherited.ToDisplayString());

		// Collect all interface members across all implemented interfaces (including inherited)
		EquatableArray<DecoratorInterface> interfaceInfos = GetAllInterfaces(interfaces)
			.Select(interfaceSymbol => CreateInterface(interfaceSymbol, primaryCovers))
			.ToEquatableArray();

		return GeneratorResult<DecoratorRequest>.FromRequest(new DecoratorRequest(type, primaryInterface.ToMinimalDisplayString(), interfaceInfos));
	}

	private static DecoratorInterface CreateInterface(INamedTypeSymbol interfaceSymbol, HashSet<string> primaryCovers)
	{
		// Determine the inner accessor expression - cast if the interface is not covered by the primary interface type
		string innerAccess = primaryCovers.Contains(interfaceSymbol.ToDisplayString())
			? "_inner"
			: $"(({interfaceSymbol.ToMinimalDisplayString()})_inner)";

		List<DecoratorMethod> methods = [];
		List<DecoratorProperty> properties = [];
		List<(string TypeName, string Name)> events = [];

		foreach (ISymbol member in interfaceSymbol.GetMembers())
		{
			switch (member)
			{
				case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
					methods.Add(new DecoratorMethod(
						method.ReturnType.ToMinimalDisplayString(),
						method.Name,
						GetTypeParameterList(method),
						GetTypeParameterConstraints(method),
						GetParameterList(method),
						GetArgumentList(method),
						GetTypeArgumentList(method),
						method.ReturnsVoid));
					break;
				case IPropertySymbol property:
					properties.Add(new DecoratorProperty(
						property.Type.ToMinimalDisplayString(),
						property.Name,
						property.GetMethod is not null && property.GetMethod.DeclaredAccessibility == Accessibility.Public,
						property.SetMethod is not null && property.SetMethod.DeclaredAccessibility == Accessibility.Public));
					break;
				case IEventSymbol evt:
					events.Add((evt.Type.ToMinimalDisplayString(), evt.Name));
					break;
			}
		}

		return new DecoratorInterface(innerAccess, methods.ToEquatableArray(), properties.ToEquatableArray(), events.ToEquatableArray());
	}

	private static void Execute(SourceProductionContext context, DecoratorRequest request)
	{
		TypeTargetInfo type = request.Type;
		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.DecoratorGeneratorFullName, type.NamespaceName, type.OuterClasses, FileUsings);

		AppendPartialClass(sb, type.Name, type.Accessibility, request.InnerTypeName, request.Interfaces);

		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, type.Name, "Decorator", sb);
	}

	private static void AppendPartialClass(SourceBuilder sb, string className, string accessibility, string innerTypeName, EquatableArray<DecoratorInterface> interfaceInfos)
	{
		sb.OpenClass(accessibility, className);

		AppendConstructor(sb, className, innerTypeName);
		AppendHooks(sb, interfaceInfos);
		AppendMemberDelegations(sb, interfaceInfos);

		sb.CloseClass();
	}

	private static void AppendConstructor(SourceBuilder sb, string className, string innerTypeName)
	{
		// Inner field
		sb.AppendLine($"private readonly {innerTypeName} _inner;");
		sb.AppendLine();

		// Constructor
		sb.AppendLine("/// <summary>");
		sb.AppendLine($"/// Initializes a new instance of the <see cref=\"{className}\"/> class,");
		sb.AppendLine("/// wrapping the specified <paramref name=\"inner\"/> instance.");
		sb.AppendLine("/// </summary>");
		sb.AppendLine($"/// <param name=\"inner\">The inner <see cref=\"{innerTypeName}\"/> instance to decorate.</param>");
		sb.AppendLine($"public {className}({innerTypeName} inner)");
		sb.OpenBrace();
		sb.AppendLine("_inner = inner ?? throw new ArgumentNullException(nameof(inner));");
		sb.CloseBrace();
	}

	private static void AppendHooks(SourceBuilder sb, EquatableArray<DecoratorInterface> interfaceInfos)
	{
		// Generate partial method hooks
		HashSet<string> emittedHooks = new(StringComparer.Ordinal);
		foreach (DecoratorInterface info in interfaceInfos)
		{
			foreach (DecoratorMethod method in info.Methods)
			{
				if (emittedHooks.Add(method.Name))
				{
					sb.AppendLine();
					sb.AppendLine($"partial void On{method.Name}Executing();");
					sb.AppendLine($"partial void On{method.Name}Executed();");
				}
			}
		}
	}

	private static void AppendMemberDelegations(SourceBuilder sb, EquatableArray<DecoratorInterface> interfaceInfos)
	{
		// Generate members for all interfaces
		foreach (DecoratorInterface info in interfaceInfos)
		{
			string innerAccess = info.InnerAccess;

			foreach (DecoratorMethod method in info.Methods)
			{
				sb.AppendLine();
				AppendMethodDelegation(sb, method, innerAccess);
			}

			foreach (DecoratorProperty property in info.Properties)
			{
				sb.AppendLine();
				AppendPropertyDelegation(sb, property, innerAccess);
			}

			foreach ((string eventType, string eventName) in info.Events)
			{
				sb.AppendLine();
				AppendEventDelegation(sb, eventType, eventName, innerAccess);
			}
		}
	}

	private static void AppendMethodDelegation(SourceBuilder sb, DecoratorMethod method, string innerAccess)
	{
		(string returnType, string methodName, string typeParameters, string typeConstraints, string parameters, string arguments, string typeArguments, bool isVoid) = method;

		sb.AppendLine("/// <inheritdoc/>");
		sb.AppendLine($"public virtual {returnType} {methodName}{typeParameters}({parameters}){typeConstraints}");
		sb.OpenBrace();
		sb.AppendLine($"On{methodName}Executing();");
		if (isVoid)
		{
			sb.AppendLine($"{innerAccess}.{methodName}{typeArguments}({arguments});");
			sb.AppendLine($"On{methodName}Executed();");
		}
		else
		{
			sb.AppendLine($"{returnType} result = {innerAccess}.{methodName}{typeArguments}({arguments});");
			sb.AppendLine($"On{methodName}Executed();");
			sb.AppendLine("return result;");
		}
		sb.CloseBrace();
	}

	private static void AppendPropertyDelegation(SourceBuilder sb, DecoratorProperty property, string innerAccess)
	{
		(string propertyType, string propertyName, bool hasGetter, bool hasSetter) = property;

		sb.AppendLine("/// <inheritdoc/>");

		if (hasGetter && hasSetter)
		{
			sb.AppendLine($"public virtual {propertyType} {propertyName}");
			sb.OpenBrace();
			sb.AppendLine($"get => {innerAccess}.{propertyName};");
			sb.AppendLine($"set => {innerAccess}.{propertyName} = value;");
			sb.CloseBrace();
		}
		else if (hasGetter)
		{
			sb.AppendLine($"public virtual {propertyType} {propertyName}");
			sb.Indent();
			sb.AppendLine($"=> {innerAccess}.{propertyName};");
			sb.Outdent();
		}
		else if (hasSetter)
		{
			sb.AppendLine($"public virtual {propertyType} {propertyName}");
			sb.OpenBrace();
			sb.AppendLine($"set => {innerAccess}.{propertyName} = value;");
			sb.CloseBrace();
		}
	}

	private static void AppendEventDelegation(SourceBuilder sb, string eventType, string eventName, string innerAccess)
	{
		sb.AppendLine("/// <inheritdoc/>");
		sb.AppendLine($"public virtual event {eventType} {eventName}");
		sb.OpenBrace();
		sb.AppendLine($"add => {innerAccess}.{eventName} += value;");
		sb.AppendLine($"remove => {innerAccess}.{eventName} -= value;");
		sb.CloseBrace();
	}

	private static string GetParameterList(IMethodSymbol method)
	{
		List<string> parts = [];
		foreach (IParameterSymbol param in method.Parameters)
		{
			string modifier = GeneratorHelpers.GetRefKindModifier(param, includeIn: true, includeParams: false, includeRefReadOnly: true);

			string typeName = param.Type.ToMinimalDisplayString();
			parts.Add($"{modifier}{typeName} {param.Name}");
		}

		return string.Join(", ", parts);
	}

	private static string GetArgumentList(IMethodSymbol method)
	{
		List<string> parts = [];
		foreach (IParameterSymbol param in method.Parameters)
		{
			string modifier = GeneratorHelpers.GetRefKindModifier(param, includeIn: true, includeParams: false, includeRefReadOnly: true);

			parts.Add($"{modifier}{param.Name}");
		}

		return string.Join(", ", parts);
	}

	private static string GetTypeParameterList(IMethodSymbol method)
	{
		if (method.TypeParameters.Length == 0)
			return string.Empty;

		List<string> parts = [];
		foreach (ITypeParameterSymbol tp in method.TypeParameters)
			parts.Add(tp.Name);

		return $"<{string.Join(", ", parts)}>";
	}

	// Type arguments at the call site are the method's own type parameter names.
	private static string GetTypeArgumentList(IMethodSymbol method)
		=> GetTypeParameterList(method);

	private static string GetTypeParameterConstraints(IMethodSymbol method)
	{
		if (method.TypeParameters.Length == 0)
			return string.Empty;

		List<string> clauses = [];
		foreach (ITypeParameterSymbol tp in method.TypeParameters)
		{
			List<string> constraints = [];

			if (tp.HasReferenceTypeConstraint)
				constraints.Add("class");
			else if (tp.HasValueTypeConstraint)
				constraints.Add("struct");
			else if (tp.HasNotNullConstraint)
				constraints.Add("notnull");

			foreach (ITypeSymbol constraintType in tp.ConstraintTypes)
				constraints.Add(constraintType.ToMinimalDisplayString());

			if (tp.HasConstructorConstraint)
				constraints.Add("new()");

			if (constraints.Count > 0)
				clauses.Add($"where {tp.Name} : {string.Join(", ", constraints)}");
		}

		return clauses.Count == 0 ? string.Empty : " " + string.Join(" ", clauses);
	}

	/// <summary>
	/// Gets all interfaces from the direct interfaces list, including any inherited interfaces,
	/// in a deduplicated, depth-first order.
	/// </summary>
	private static List<INamedTypeSymbol> GetAllInterfaces(ImmutableArray<INamedTypeSymbol> directInterfaces)
	{
		HashSet<string> seen = new(StringComparer.Ordinal);
		List<INamedTypeSymbol> result = [];

		foreach (INamedTypeSymbol iface in directInterfaces)
		{
			CollectInterfaces(iface, seen, result);
		}

		return result;
	}

	private static void CollectInterfaces(INamedTypeSymbol iface, HashSet<string> seen, List<INamedTypeSymbol> result)
	{
		string key = iface.ToDisplayString();
		if (!seen.Add(key))
			return;

		// Collect base interfaces first
		foreach (INamedTypeSymbol baseInterface in iface.Interfaces)
		{
			CollectInterfaces(baseInterface, seen, result);
		}

		result.Add(iface);
	}
}