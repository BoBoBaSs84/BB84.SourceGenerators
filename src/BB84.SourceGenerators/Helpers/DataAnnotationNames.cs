// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Contains fully qualified names of data annotation attributes used by the <see cref="ValidatorGenerator"/>.
/// </summary>
internal static class DataAnnotationNames
{
	internal const string AllowedValues = "System.ComponentModel.DataAnnotations.AllowedValuesAttribute";
	internal const string Compare = "System.ComponentModel.DataAnnotations.CompareAttribute";
	internal const string CreditCard = "System.ComponentModel.DataAnnotations.CreditCardAttribute";
	internal const string DeniedValues = "System.ComponentModel.DataAnnotations.DeniedValuesAttribute";
	internal const string EmailAddress = "System.ComponentModel.DataAnnotations.EmailAddressAttribute";
	internal const string IValidatableObject = "System.ComponentModel.DataAnnotations.IValidatableObject";
	internal const string MaxLength = "System.ComponentModel.DataAnnotations.MaxLengthAttribute";
	internal const string MinLength = "System.ComponentModel.DataAnnotations.MinLengthAttribute";
	internal const string Phone = "System.ComponentModel.DataAnnotations.PhoneAttribute";
	internal const string Range = "System.ComponentModel.DataAnnotations.RangeAttribute";
	internal const string RegularExpression = "System.ComponentModel.DataAnnotations.RegularExpressionAttribute";
	internal const string Required = "System.ComponentModel.DataAnnotations.RequiredAttribute";
	internal const string StringLength = "System.ComponentModel.DataAnnotations.StringLengthAttribute";
	internal const string Url = "System.ComponentModel.DataAnnotations.UrlAttribute";
	internal const string ValidationAttribute = "System.ComponentModel.DataAnnotations.ValidationAttribute";
}
