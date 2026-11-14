// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Extensions;
using BB84.SourceGenerators.Models;

using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Provides the expressions that convert text values (such as environment variables or command line
/// arguments) to property types and back. Numbers and dates use the invariant culture.
/// </summary>
/// <remarks>
/// The emitted expressions use the short names <c>CultureInfo</c>, <c>NumberStyles</c> and
/// <c>DateTimeStyles</c>, so the generated file must import <c>System.Globalization</c>.
/// </remarks>
internal static class ValueConversions
{
	private const string EncodingHelperName = "TryGetEncoding";

	/// <summary>
	/// Gets the underlying type of a <see cref="Nullable{T}"/> type, or the type itself.
	/// </summary>
	/// <param name="type">The type to unwrap.</param>
	/// <returns>The underlying type.</returns>
	internal static ITypeSymbol GetUnderlyingType(ITypeSymbol type)
		=> type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
			? nullable.TypeArguments[0]
			: type;

	/// <summary>
	/// Determines the conversion kind of the given type, unwrapping <see cref="Nullable{T}"/>.
	/// </summary>
	/// <param name="type">The property type.</param>
	/// <param name="kind">When this method returns <see langword="true"/>, contains the conversion kind.</param>
	/// <param name="allowEncoding">
	/// Indicates whether <c>System.Text.Encoding</c> is supported. Its try-parse expression calls the
	/// helper emitted by <see cref="AppendEncodingHelper(SourceBuilder)"/>, which the caller must emit.
	/// </param>
	/// <returns><see langword="true"/> if the type is supported; otherwise, <see langword="false"/>.</returns>
	internal static bool TryGetKind(ITypeSymbol type, out ValueConversionKind kind, bool allowEncoding = false)
	{
		ITypeSymbol underlying = GetUnderlyingType(type);

		if (underlying.TypeKind == TypeKind.Enum)
		{
			kind = AttributeHelpers.HasAttribute(underlying, TypeNames.FlagsAttribute) ? ValueConversionKind.FlagsEnum : ValueConversionKind.Enum;
			return true;
		}

		ValueConversionKind? result = underlying.SpecialType switch
		{
			SpecialType.System_String => ValueConversionKind.String,
			SpecialType.System_Boolean => ValueConversionKind.Boolean,
			SpecialType.System_Char => ValueConversionKind.Char,
			SpecialType.System_SByte or SpecialType.System_Byte
				or SpecialType.System_Int16 or SpecialType.System_UInt16
				or SpecialType.System_Int32 or SpecialType.System_UInt32
				or SpecialType.System_Int64 or SpecialType.System_UInt64 => ValueConversionKind.Integer,
			SpecialType.System_Single or SpecialType.System_Double => ValueConversionKind.FloatingPoint,
			SpecialType.System_Decimal => ValueConversionKind.Decimal,
			SpecialType.System_DateTime => ValueConversionKind.DateTime,
			_ => underlying.ToFullyQualifiedDisplayString() switch
			{
				"System.DateTimeOffset" => ValueConversionKind.DateTimeOffset,
				"System.TimeSpan" => ValueConversionKind.TimeSpan,
				"System.Guid" => ValueConversionKind.Guid,
				"System.Text.Encoding" when allowEncoding => ValueConversionKind.Encoding,
				_ => null,
			},
		};

		kind = result.GetValueOrDefault();
		return result.HasValue;
	}

	/// <summary>
	/// Creates the conversion of the given type for the given conversion kind.
	/// </summary>
	/// <param name="type">The property type.</param>
	/// <param name="kind">The conversion kind returned by <see cref="TryGetKind(ITypeSymbol, out ValueConversionKind, bool)"/>.</param>
	/// <returns>The conversion.</returns>
	internal static ValueConversion Create(ITypeSymbol type, ValueConversionKind kind)
	{
		ITypeSymbol underlying = GetUnderlyingType(type);

		return new ValueConversion(
			kind,
			underlying.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToPropertyTypeDisplayString(),
			underlying.Name,
			!SymbolEqualityComparer.Default.Equals(underlying, type));
	}

	/// <summary>
	/// Builds a <see cref="bool"/> primary expression (safe to negate with <c>!</c>) that tries to convert <paramref name="valueExpression"/>
	/// and declares the converted value as the out variable <paramref name="resultVariable"/>.
	/// </summary>
	/// <param name="kind">The conversion kind. <see cref="ValueConversionKind.String"/> needs no conversion and is not supported.</param>
	/// <param name="typeName">The fully qualified name of the underlying (non-nullable) type.</param>
	/// <param name="valueExpression">The non-null <see cref="string"/> expression to convert; it is evaluated more than once.</param>
	/// <param name="resultVariable">The name of the out variable that receives the converted value.</param>
	/// <returns>The try-parse expression.</returns>
	internal static string GetTryParseExpression(ValueConversionKind kind, string typeName, string valueExpression, string resultVariable)
	{
		string result = $"out {typeName} {resultVariable}";

		return kind switch
		{
			ValueConversionKind.Boolean => $"{typeName}.TryParse({valueExpression}.Trim() == \"1\" ? \"true\" : {valueExpression}.Trim() == \"0\" ? \"false\" : {valueExpression}, {result})",
			ValueConversionKind.Char => $"{typeName}.TryParse({valueExpression}, {result})",
			ValueConversionKind.Integer => $"{typeName}.TryParse({valueExpression}, NumberStyles.Integer, CultureInfo.InvariantCulture, {result})",
			ValueConversionKind.FloatingPoint => $"{typeName}.TryParse({valueExpression}, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, {result})",
			ValueConversionKind.Decimal => $"{typeName}.TryParse({valueExpression}, NumberStyles.Number, CultureInfo.InvariantCulture, {result})",
			ValueConversionKind.DateTime => $"{typeName}.TryParse({valueExpression}, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, {result})",
			ValueConversionKind.DateTimeOffset => $"{typeName}.TryParse({valueExpression}, CultureInfo.InvariantCulture, DateTimeStyles.None, {result})",
			ValueConversionKind.TimeSpan => $"{typeName}.TryParse({valueExpression}, CultureInfo.InvariantCulture, {result})",
			ValueConversionKind.Guid => $"{typeName}.TryParse({valueExpression}, {result})",
			ValueConversionKind.Enum => $"(global::System.Enum.TryParse<{typeName}>({valueExpression}, true, {result}) && global::System.Enum.IsDefined(typeof({typeName}), {resultVariable}))",
			ValueConversionKind.FlagsEnum => $"global::System.Enum.TryParse<{typeName}>({valueExpression}, true, {result})",
			ValueConversionKind.Encoding => $"{EncodingHelperName}({valueExpression}, {result})",
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};
	}

	/// <summary>
	/// Builds a <see cref="string"/> expression that formats the non-null value <paramref name="expression"/>
	/// so that it can be converted back with <see cref="GetTryParseExpression"/>.
	/// </summary>
	/// <param name="kind">The conversion kind.</param>
	/// <param name="expression">The value expression of the underlying (non-nullable) type.</param>
	/// <returns>The format expression.</returns>
	internal static string GetFormatExpression(ValueConversionKind kind, string expression)
	{
		return kind switch
		{
			ValueConversionKind.String => expression,
			ValueConversionKind.Boolean => $"({expression} ? \"true\" : \"false\")",
			ValueConversionKind.Integer or ValueConversionKind.Decimal => $"{expression}.ToString(CultureInfo.InvariantCulture)",
			ValueConversionKind.FloatingPoint => $"{expression}.ToString(\"R\", CultureInfo.InvariantCulture)",
			ValueConversionKind.DateTime or ValueConversionKind.DateTimeOffset => $"{expression}.ToString(\"O\", CultureInfo.InvariantCulture)",
			ValueConversionKind.TimeSpan => $"{expression}.ToString(\"c\", CultureInfo.InvariantCulture)",
			ValueConversionKind.Encoding => $"{expression}.WebName",
			_ => $"{expression}.ToString()",
		};
	}

	/// <summary>
	/// Appends the private static helper method called by the try-parse expression of
	/// <see cref="ValueConversionKind.Encoding"/>. It accepts an encoding name or a numeric code page.
	/// </summary>
	/// <param name="sb">The source builder to append to.</param>
	internal static void AppendEncodingHelper(SourceBuilder sb)
	{
		sb.AppendSummary("Tries to get the encoding with the specified name or numeric code page.");
		sb.AppendLine("/// <param name=\"value\">The encoding name or code page.</param>");
		sb.AppendLine("/// <param name=\"result\">The encoding, or <see langword=\"null\"/> if it is unknown.</param>");
		sb.AppendLine("/// <returns><see langword=\"true\"/> if the encoding is known; otherwise, <see langword=\"false\"/>.</returns>");
		sb.AppendLine($"private static bool {EncodingHelperName}(string value, out global::System.Text.Encoding result)");
		sb.OpenBrace();
		sb.AppendLine("try");
		sb.OpenBrace();
		sb.AppendLine("result = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int codePage)");
		sb.Indent();
		sb.AppendLine("? global::System.Text.Encoding.GetEncoding(codePage)");
		sb.AppendLine(": global::System.Text.Encoding.GetEncoding(value.Trim());");
		sb.Outdent();
		sb.AppendLine("return true;");
		sb.CloseBrace();
		sb.AppendLine("catch (global::System.ArgumentException)");
		sb.OpenBrace();
		sb.CloseBrace();
		sb.AppendLine("catch (global::System.NotSupportedException)");
		sb.OpenBrace();
		sb.CloseBrace();
		sb.AppendLine();
		sb.AppendLine("result = null!;");
		sb.AppendLine("return false;");
		sb.CloseBrace();
	}
}
