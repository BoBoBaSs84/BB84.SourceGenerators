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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators;

/// <summary>
/// Responsible for generating a <c>Validate()</c> method for classes marked with the
/// <see cref="GenerateValidatorAttribute"/> attribute. The generated method scans
/// properties for data annotation attributes and emits direct validation checks.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ValidatorGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateValidatorAttribute>();

	private static readonly string[] FileUsings =
	[
		NamespaceNames.SystemCollectionsGeneric,
		NamespaceNames.SystemComponentModelDataAnnotations,
		NamespaceNames.SystemTextRegularExpressions,
	];

	private const string ErrorMessageArgumentName = "ErrorMessage";

	private static readonly Dictionary<ValidationKind, SimpleAttributeRule> SimpleAttributeRules = new()
	{
		[ValidationKind.EmailAddress] = new("EmailAddressAttribute", "e-mail address"),
		[ValidationKind.Url] = new("UrlAttribute", "fully-qualified http, https, or ftp URL"),
		[ValidationKind.Phone] = new("PhoneAttribute", "phone number"),
		[ValidationKind.CreditCard] = new("CreditCardAttribute", "credit card number"),
	};

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<ValidatorRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassNode, Transform, TrackingNames.Validator, Execute);

	private static GeneratorResult<ValidatorRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not ClassDeclarationSyntax classDeclaration || context.TargetSymbol is not INamedTypeSymbol classSymbol)
			return null;

		TypeTargetInfo type = TypeTargetInfo.Create(classDeclaration, classSymbol);

		if (classSymbol.IsAbstract)
			return GeneratorResult<ValidatorRequest>.FromDiagnostics(type.CreateDiagnostic(DiagnosticDescriptors.AbstractClassDiagnostic, type.Name));

		EquatableArray<PropertyValidationInfo> validatedProperties = GetValidatedProperties(classSymbol);
		bool implementsIValidatableObject = GeneratorHelpers.ImplementsInterface(classSymbol, DataAnnotationNames.IValidatableObject);

		return GeneratorResult<ValidatorRequest>.FromRequest(new ValidatorRequest(type, validatedProperties, implementsIValidatableObject));
	}

	private static void Execute(SourceProductionContext context, ValidatorRequest request)
	{
		TypeTargetInfo type = request.Type;
		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.ValidatorGeneratorFullName, type.NamespaceName, type.OuterClasses, FileUsings);

		AppendPartialClass(sb, type.Name, type.Accessibility, request.Properties, request.ImplementsIValidatableObject);

		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, type.Name, "Validator", sb);
	}

	private static void AppendPartialClass(SourceBuilder sb, string className, string accessibility, EquatableArray<PropertyValidationInfo> validatedProperties, bool implementsIValidatableObject)
	{
		sb.OpenClass(accessibility, className);

		// Collect all unique regex patterns and emit static readonly Regex fields
		Dictionary<string, RegexFieldInfo> regexFields = CollectRegexPatterns(validatedProperties);

		AppendRegexFields(sb, regexFields);
		AppendValidateAll(sb, validatedProperties, implementsIValidatableObject, regexFields);
		AppendValidateProperty(sb);

		sb.CloseClass();
	}

	private static void AppendRegexFields(SourceBuilder sb, Dictionary<string, RegexFieldInfo> regexFields)
	{
		foreach (KeyValuePair<string, RegexFieldInfo> entry in regexFields)
		{
			sb.AppendLine($"private static readonly Regex {entry.Value.FieldName} = new Regex(@\"{GeneratorHelpers.EscapeVerbatimString(entry.Key)}\", RegexOptions.Compiled);");
		}

		if (regexFields.Count > 0)
			sb.AppendLine();
	}

	private static void AppendValidateAll(SourceBuilder sb, EquatableArray<PropertyValidationInfo> validatedProperties, bool implementsIValidatableObject, Dictionary<string, RegexFieldInfo> regexFields)
	{
		// Generate Validate() returning Dictionary<string, List<string>>
		sb.AppendSummary("Validates the current instance and returns a dictionary of validation errors grouped by property name.");
		sb.AppendLine("/// <returns>A dictionary where each key is a property name and the value is a list of validation error messages. An empty dictionary indicates a valid instance.</returns>");
		sb.AppendLine("public Dictionary<string, List<string>> Validate()");
		sb.OpenBrace();
		sb.AppendLine("Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();");

		foreach (PropertyValidationInfo property in validatedProperties)
		{
			foreach (ValidationRule rule in property.Rules)
			{
				AppendValidationRule(sb, property, rule, regexFields);
			}
		}

		if (implementsIValidatableObject)
		{
			sb.AppendLine();
			sb.AppendLine("foreach (ValidationResult validationResult in ((IValidatableObject)this).Validate(new ValidationContext(this)))");
			sb.OpenBrace();
			sb.AppendLine("if (validationResult != ValidationResult.Success)");
			sb.OpenBrace();
			sb.AppendLine("string errorMessage = validationResult.ErrorMessage ?? \"Validation failed.\";\n");
			sb.AppendLine("if (validationResult.MemberNames != null)");
			sb.OpenBrace();
			sb.AppendLine("foreach (string memberName in validationResult.MemberNames)");
			sb.OpenBrace();
			sb.AppendLine("if (!errors.ContainsKey(memberName))");
			sb.Indent();
			sb.AppendLine("errors[memberName] = new List<string>();");
			sb.Outdent();
			sb.AppendLine("errors[memberName].Add(errorMessage);");
			sb.CloseBrace();
			sb.CloseBrace();
			sb.CloseBrace();
			sb.CloseBrace();
		}

		sb.AppendLine("return errors;");
		sb.CloseBrace();
		sb.AppendLine();
	}

	private static void AppendValidateProperty(SourceBuilder sb)
	{
		// Generate Validate(string propertyName) returning List<string>
		sb.AppendSummary("Validates a specific property of the current instance and returns a list of validation errors.");
		sb.AppendLine("/// <param name=\"propertyName\">The name of the property to validate.</param>");
		sb.AppendLine("/// <returns>A list of validation error messages for the specified property. An empty list indicates the property is valid.</returns>");
		sb.AppendLine("public List<string> Validate(string propertyName)");
		sb.OpenBrace();
		sb.AppendLine("Dictionary<string, List<string>> allErrors = Validate();");
		sb.AppendLine("if (allErrors.TryGetValue(propertyName, out List<string>? propertyErrors))");
		sb.Indent();
		sb.AppendLine("return propertyErrors;");
		sb.Outdent();
		sb.AppendLine("return new List<string>();");
		sb.CloseBrace();
	}

	private static void AppendValidationRule(SourceBuilder sb, PropertyValidationInfo property, ValidationRule rule, Dictionary<string, RegexFieldInfo> regexFields)
	{
		string propertyName = property.Name;

		sb.AppendLine();

		switch (rule.Kind)
		{
			case ValidationKind.Required:
				AppendRequiredValidation(sb, propertyName, property.TypeName, rule);
				break;
			case ValidationKind.Range:
				AppendRangeValidation(sb, propertyName, property.IsCollection, property.IsArray, rule);
				break;
			case ValidationKind.StringLength:
				AppendStringLengthValidation(sb, propertyName, rule);
				break;
			case ValidationKind.MinLength:
				AppendMinLengthValidation(sb, propertyName, rule);
				break;
			case ValidationKind.MaxLength:
				AppendMaxLengthValidation(sb, propertyName, rule);
				break;
			case ValidationKind.RegularExpression:
				AppendRegularExpressionValidation(sb, propertyName, rule, regexFields);
				break;
			case ValidationKind.EmailAddress:
			case ValidationKind.Url:
			case ValidationKind.Phone:
			case ValidationKind.CreditCard:
				AppendSimpleAttributeValidation(sb, propertyName, rule, SimpleAttributeRules[rule.Kind]);
				break;
			case ValidationKind.Compare:
				AppendCompareValidation(sb, propertyName, rule);
				break;
			case ValidationKind.AllowedValues:
				AppendAllowedValuesValidation(sb, propertyName, rule);
				break;
			case ValidationKind.DeniedValues:
				AppendDeniedValuesValidation(sb, propertyName, rule);
				break;
			case ValidationKind.CustomValidation:
				AppendCustomValidation(sb, propertyName, rule);
				break;
		}
	}

	private static void AppendRequiredValidation(SourceBuilder sb, string propertyName, string typeName, ValidationRule rule)
	{
		string message = rule.ErrorMessage ?? $"The {propertyName} field is required.";

		if (typeName is "string" or "string?")
			sb.AppendLine($"if (string.IsNullOrEmpty({propertyName}))");
		else
			sb.AppendLine($"if ({propertyName} == null)");

		AppendAddError(sb, propertyName, message);
	}

	private static void AppendRangeValidation(SourceBuilder sb, string propertyName, bool isCollection, bool isArray, ValidationRule rule)
	{
		if (isCollection)
		{
			string countAccessor = isArray ? "Length" : "Count";
			string message = rule.ErrorMessage ?? $"The field {propertyName} must have between {rule.MinValue} and {rule.MaxValue} elements.";

			sb.AppendLine($"if ({propertyName} != null && ({propertyName}.{countAccessor} < {rule.MinValue} || {propertyName}.{countAccessor} > {rule.MaxValue}))");
			AppendAddError(sb, propertyName, message);
		}
		else
		{
			string message = rule.ErrorMessage ?? $"The field {propertyName} must be between {rule.MinValue} and {rule.MaxValue}.";

			sb.AppendLine($"if ({propertyName} < {rule.MinValue} || {propertyName} > {rule.MaxValue})");
			AppendAddError(sb, propertyName, message);
		}
	}

	private static void AppendStringLengthValidation(SourceBuilder sb, string propertyName, ValidationRule rule)
	{
		if (rule.MinValue is not null)
		{
			string minMessage = rule.ErrorMessage ?? $"The field {propertyName} must be a string with a minimum length of {rule.MinValue} and a maximum length of {rule.MaxValue}.";

			sb.AppendLine($"if ({propertyName} != null && ({propertyName}.Length < {rule.MinValue} || {propertyName}.Length > {rule.MaxValue}))");
			AppendAddError(sb, propertyName, minMessage);
		}
		else
		{
			string message = rule.ErrorMessage ?? $"The field {propertyName} must be a string with a maximum length of {rule.MaxValue}.";

			sb.AppendLine($"if ({propertyName} != null && {propertyName}.Length > {rule.MaxValue})");
			AppendAddError(sb, propertyName, message);
		}
	}

	private static void AppendMinLengthValidation(SourceBuilder sb, string propertyName, ValidationRule rule)
	{
		string message = rule.ErrorMessage ?? $"The field {propertyName} must be a string or collection with a minimum length of {rule.MinValue}.";

		sb.AppendLine($"if ({propertyName} != null && {propertyName}.Length < {rule.MinValue})");
		AppendAddError(sb, propertyName, message);
	}

	private static void AppendMaxLengthValidation(SourceBuilder sb, string propertyName, ValidationRule rule)
	{
		string message = rule.ErrorMessage ?? $"The field {propertyName} must be a string or collection with a maximum length of {rule.MaxValue}.";

		sb.AppendLine($"if ({propertyName} != null && {propertyName}.Length > {rule.MaxValue})");
		AppendAddError(sb, propertyName, message);
	}

	private static void AppendRegularExpressionValidation(SourceBuilder sb, string propertyName, ValidationRule rule, Dictionary<string, RegexFieldInfo> regexFields)
	{
		string message = rule.ErrorMessage ?? $"The field {propertyName} must match the regular expression '{GeneratorHelpers.EscapeString(rule.Pattern!)}'.";
		string fieldName = regexFields[rule.Pattern!].FieldName;

		sb.AppendLine($"if ({propertyName} != null && !{fieldName}.IsMatch({propertyName}))");
		AppendAddError(sb, propertyName, message);
	}

	private static void AppendSimpleAttributeValidation(SourceBuilder sb, string propertyName, ValidationRule rule, SimpleAttributeRule simpleRule)
	{
		string message = rule.ErrorMessage ?? $"The {propertyName} field is not a valid {simpleRule.ValueDescription}.";

		sb.AppendLine($"if ({propertyName} != null && !new {simpleRule.AttributeTypeName}().IsValid({propertyName}))");
		AppendAddError(sb, propertyName, message);
	}

	private static void AppendCompareValidation(SourceBuilder sb, string propertyName, ValidationRule rule)
	{
		string otherProperty = rule.CompareProperty!;
		string message = rule.ErrorMessage ?? $"'{propertyName}' and '{otherProperty}' do not match.";

		sb.AppendLine($"if (!Equals({propertyName}, {otherProperty}))");
		AppendAddError(sb, propertyName, message);
	}

	private static void AppendAllowedValuesValidation(SourceBuilder sb, string propertyName, ValidationRule rule)
	{
		string message = rule.ErrorMessage ?? $"The field {propertyName} does not contain an allowed value.";
		string conditions = string.Join(" && ", rule.Values!.Value.Select(v => $"!Equals({propertyName}, {v})"));

		sb.AppendLine($"if ({conditions})");
		AppendAddError(sb, propertyName, message);
	}

	private static void AppendDeniedValuesValidation(SourceBuilder sb, string propertyName, ValidationRule rule)
	{
		string message = rule.ErrorMessage ?? $"The field {propertyName} contains a denied value.";
		string conditions = string.Join(" || ", rule.Values!.Value.Select(v => $"Equals({propertyName}, {v})"));

		sb.AppendLine($"if ({conditions})");
		AppendAddError(sb, propertyName, message);
	}

	private static void AppendCustomValidation(SourceBuilder sb, string propertyName, ValidationRule rule)
	{
		string attributeTypeName = rule.CustomAttributeTypeName!;
		string message = rule.ErrorMessage ?? $"The field {propertyName} is invalid.";

		sb.AppendLine($"if (!new {attributeTypeName}().IsValid({propertyName}))");
		AppendAddError(sb, propertyName, message);
	}

	private static void AppendAddError(SourceBuilder sb, string propertyName, string message)
	{
		sb.OpenBrace();
		sb.AppendLine($"if (!errors.ContainsKey(nameof({propertyName})))");
		sb.Indent();
		sb.AppendLine($"errors[nameof({propertyName})] = new List<string>();");
		sb.Outdent();
		sb.AppendLine($"errors[nameof({propertyName})].Add(\"{GeneratorHelpers.EscapeString(message)}\");");
		sb.CloseBrace();
	}

	private static EquatableArray<PropertyValidationInfo> GetValidatedProperties(INamedTypeSymbol classSymbol)
	{
		ImmutableArray<PropertyValidationInfo>.Builder builder = ImmutableArray.CreateBuilder<PropertyValidationInfo>();
		HashSet<string> seen = [];

		foreach (IPropertySymbol propertySymbol in GeneratorHelpers.GetPublicPropertySymbols(classSymbol, includeInherited: true, requireGetter: true))
		{
			if (!seen.Add(propertySymbol.Name))
				continue;

			ImmutableArray<ValidationRule>.Builder rules = ImmutableArray.CreateBuilder<ValidationRule>();

			foreach (AttributeData attribute in propertySymbol.GetAttributes())
			{
				string? attributeName = attribute.AttributeClass?.ToDisplayString();

				if (attributeName is null)
					continue;

				ValidationRule? rule = attributeName switch
				{
					DataAnnotationNames.Required => ParseRequiredAttribute(attribute),
					DataAnnotationNames.Range => ParseRangeAttribute(attribute),
					DataAnnotationNames.StringLength => ParseStringLengthAttribute(attribute),
					DataAnnotationNames.MinLength => ParseMinLengthAttribute(attribute),
					DataAnnotationNames.MaxLength => ParseMaxLengthAttribute(attribute),
					DataAnnotationNames.RegularExpression => ParseRegularExpressionAttribute(attribute),
					DataAnnotationNames.EmailAddress => ParseSimpleAttribute(attribute, ValidationKind.EmailAddress),
					DataAnnotationNames.Url => ParseSimpleAttribute(attribute, ValidationKind.Url),
					DataAnnotationNames.Phone => ParseSimpleAttribute(attribute, ValidationKind.Phone),
					DataAnnotationNames.CreditCard => ParseSimpleAttribute(attribute, ValidationKind.CreditCard),
					DataAnnotationNames.Compare => ParseCompareAttribute(attribute),
					DataAnnotationNames.AllowedValues => ParseValuesAttribute(attribute, ValidationKind.AllowedValues),
					DataAnnotationNames.DeniedValues => ParseValuesAttribute(attribute, ValidationKind.DeniedValues),
					_ => TryParseCustomValidationAttribute(attribute)
				};

				if (rule is not null)
					rules.Add(rule);
			}

			if (rules.Count > 0)
			{
				string typeName = propertySymbol.Type.ToMinimalDisplayString();
				bool isCollection = IsCollectionType(propertySymbol.Type);
				bool isArray = propertySymbol.Type.TypeKind == TypeKind.Array;
				builder.Add(new PropertyValidationInfo(propertySymbol.Name, typeName, isCollection, isArray, rules.ToImmutable().ToEquatableArray()));
			}
		}

		return builder.ToImmutable().ToEquatableArray();
	}

	private static ValidationRule ParseRequiredAttribute(AttributeData attribute)
	{
		string? errorMessage = GeneratorHelpers.GetNamedArgument<string?>(attribute, ErrorMessageArgumentName, null);

		return new ValidationRule(ValidationKind.Required) { ErrorMessage = errorMessage };
	}

	private static ValidationRule? ParseRangeAttribute(AttributeData attribute)
	{
		ImmutableArray<TypedConstant> args = attribute.ConstructorArguments;

		if (args.Length < 2)
			return null;

		string? minValue = GeneratorHelpers.FormatInvariant(args[0].Value);
		string? maxValue = GeneratorHelpers.FormatInvariant(args[1].Value);

		if (minValue is null || maxValue is null)
			return null;

		string? errorMessage = GeneratorHelpers.GetNamedArgument<string?>(attribute, ErrorMessageArgumentName, null);

		return new ValidationRule(ValidationKind.Range) { MinValue = minValue, MaxValue = maxValue, ErrorMessage = errorMessage };
	}

	private static ValidationRule? ParseStringLengthAttribute(AttributeData attribute)
	{
		if (!TryGetFirstArgString(attribute, out string maxValue))
			return null;

		string? errorMessage = GeneratorHelpers.GetNamedArgument<string?>(attribute, ErrorMessageArgumentName, null);

		int? minLengthValue = GeneratorHelpers.GetNamedArgument<int>(attribute, "MinimumLength", default);
		string? minValue = minLengthValue?.ToString(System.Globalization.CultureInfo.InvariantCulture);

		return new ValidationRule(ValidationKind.StringLength) { MaxValue = maxValue, MinValue = minValue, ErrorMessage = errorMessage };
	}

	private static ValidationRule? ParseMinLengthAttribute(AttributeData attribute)
	{
		if (!TryGetFirstArgString(attribute, out string minValue))
			return null;

		string? errorMessage = GeneratorHelpers.GetNamedArgument<string?>(attribute, ErrorMessageArgumentName, null);

		return new ValidationRule(ValidationKind.MinLength) { MinValue = minValue, ErrorMessage = errorMessage };
	}

	private static ValidationRule? ParseMaxLengthAttribute(AttributeData attribute)
	{
		if (!TryGetFirstArgString(attribute, out string maxValue))
			return null;

		string? errorMessage = GeneratorHelpers.GetNamedArgument<string?>(attribute, ErrorMessageArgumentName, null);

		return new ValidationRule(ValidationKind.MaxLength) { MaxValue = maxValue, ErrorMessage = errorMessage };
	}

	private static ValidationRule? ParseRegularExpressionAttribute(AttributeData attribute)
	{
		if (!TryGetFirstArgString(attribute, out string pattern))
			return null;

		string? errorMessage = GeneratorHelpers.GetNamedArgument<string?>(attribute, ErrorMessageArgumentName, null);

		return new ValidationRule(ValidationKind.RegularExpression) { Pattern = pattern, ErrorMessage = errorMessage };
	}

	private static ValidationRule ParseSimpleAttribute(AttributeData attribute, ValidationKind kind)
	{
		string? errorMessage = GeneratorHelpers.GetNamedArgument<string?>(attribute, ErrorMessageArgumentName, null);
		return new ValidationRule(kind) { ErrorMessage = errorMessage };
	}

	private static ValidationRule? ParseCompareAttribute(AttributeData attribute)
	{
		if (!TryGetFirstArgString(attribute, out string otherProperty))
			return null;

		string? errorMessage = GeneratorHelpers.GetNamedArgument<string?>(attribute, ErrorMessageArgumentName, null);

		return new ValidationRule(ValidationKind.Compare) { CompareProperty = otherProperty, ErrorMessage = errorMessage };
	}

	private static ValidationRule? ParseValuesAttribute(AttributeData attribute, ValidationKind kind)
	{
		ImmutableArray<TypedConstant> args = attribute.ConstructorArguments;

		if (args.Length < 1 || args[0].Kind != TypedConstantKind.Array)
			return null;

		ImmutableArray<string> values = [.. args[0].Values
			.Select(FormatConstantValue)
			.Where(v => v is not null)
			.Select(v => v!)];

		if (values.IsEmpty)
			return null;

		string? errorMessage = GeneratorHelpers.GetNamedArgument<string?>(attribute, ErrorMessageArgumentName, null);

		return new ValidationRule(kind) { Values = values.ToEquatableArray(), ErrorMessage = errorMessage };
	}

	private static bool TryGetFirstArgString(AttributeData attribute, out string value)
	{
		ImmutableArray<TypedConstant> args = attribute.ConstructorArguments;
		string? text = args.Length < 1 ? null : GeneratorHelpers.FormatInvariant(args[0].Value);

		value = text ?? string.Empty;
		return text is not null;
	}

	private static string? FormatConstantValue(TypedConstant constant)
	{
		if (constant.IsNull)
			return "null";

		return constant.Value switch
		{
			string str => $"\"{GeneratorHelpers.EscapeString(str)}\"",
			char c => $"'{c}'",
			bool b => b ? "true" : "false",
			_ => GeneratorHelpers.FormatInvariant(constant.Value)
		};
	}

	private static ValidationRule? TryParseCustomValidationAttribute(AttributeData attribute)
	{
		if (attribute.AttributeClass is null)
			return null;

		// Check if this attribute inherits from ValidationAttribute
		INamedTypeSymbol? baseType = attribute.AttributeClass.BaseType;
		bool isValidationAttribute = false;

		while (baseType is not null)
		{
			if (baseType.ToDisplayString() == DataAnnotationNames.ValidationAttribute)
			{
				isValidationAttribute = true;
				break;
			}

			baseType = baseType.BaseType;
		}

		if (!isValidationAttribute)
			return null;

		string? errorMessage = GeneratorHelpers.GetNamedArgument<string?>(attribute, ErrorMessageArgumentName, null);
		string attributeTypeName = attribute.AttributeClass.ToDisplayString();

		return new ValidationRule(ValidationKind.CustomValidation) { CustomAttributeTypeName = attributeTypeName, ErrorMessage = errorMessage };
	}

	private static bool IsCollectionType(ITypeSymbol type)
	{
		if (type.TypeKind == TypeKind.Array)
			return true;

		if (type.SpecialType == SpecialType.System_String)
			return false;

		foreach (INamedTypeSymbol iface in type.AllInterfaces)
		{
			if (iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
				return true;
		}

		return type is INamedTypeSymbol namedType && namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;
	}

	private enum ValidationKind
	{
		Required,
		Range,
		StringLength,
		MinLength,
		MaxLength,
		RegularExpression,
		EmailAddress,
		Url,
		Phone,
		CreditCard,
		Compare,
		AllowedValues,
		DeniedValues,
		CustomValidation
	}

	private sealed record ValidationRule(ValidationKind Kind)
	{
		public string? MinValue { get; set; }
		public string? MaxValue { get; set; }
		public string? Pattern { get; set; }
		public string? ErrorMessage { get; set; }
		public string? CompareProperty { get; set; }
		public string? CustomAttributeTypeName { get; set; }
		public EquatableArray<string>? Values { get; set; }
	}

	private readonly record struct PropertyValidationInfo(string Name, string TypeName, bool IsCollection, bool IsArray, EquatableArray<ValidationRule> Rules);

	private sealed record ValidatorRequest(TypeTargetInfo Type, EquatableArray<PropertyValidationInfo> Properties, bool ImplementsIValidatableObject);

	private static Dictionary<string, RegexFieldInfo> CollectRegexPatterns(EquatableArray<PropertyValidationInfo> validatedProperties)
	{
		Dictionary<string, RegexFieldInfo> patterns = [];
		int counter = 0;

		foreach (PropertyValidationInfo property in validatedProperties)
		{
			foreach (ValidationRule rule in property.Rules)
			{
				if (rule.Kind == ValidationKind.RegularExpression && rule.Pattern is string pattern && !patterns.ContainsKey(pattern))
				{
					patterns[pattern] = new RegexFieldInfo($"s_regex{counter}");
					counter++;
				}
			}
		}

		return patterns;
	}

	private sealed record RegexFieldInfo(string FieldName);

	private sealed record SimpleAttributeRule(string AttributeTypeName, string ValueDescription);
}
