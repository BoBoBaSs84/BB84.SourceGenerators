// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;

using BB84.SourceGenerators.Analyzers;
using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Helpers;
using BB84.SourceGenerators.Models;
using BB84.SourceGenerators.Requests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BB84.SourceGenerators;

/// <summary>
/// Responsible for generating the dispose pattern for classes marked
/// with the <see cref="GenerateDisposableAttribute"/> attribute.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DisposableGenerator : IIncrementalGenerator
{
	private static readonly (string MetadataName, string TypeName, string ShortName) AttributeNames =
		GeneratorHelpers.GetAttributeNames<GenerateDisposableAttribute>();
	private static readonly (string MetadataName, string TypeName, string ShortName) DisposeResourceAttributeNames =
		GeneratorHelpers.GetAttributeNames<DisposeResourceAttribute>();

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> GeneratorHelpers.RegisterRequestGenerator<DisposableRequest>(context, AttributeNames.MetadataName, GeneratorHelpers.IsClassNode, Transform, TrackingNames.Disposable, Execute);

	private static GeneratorResult<DisposableRequest>? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is not ClassDeclarationSyntax classDeclaration || context.TargetSymbol is not INamedTypeSymbol classSymbol)
			return null;

		TypeTargetInfo type = TypeTargetInfo.Create(classDeclaration, classSymbol);
		EquatableArray<DiagnosticInfo> diagnostics = EquatableArray<DiagnosticInfo>.Empty;

		bool generateFinalizer = GetGenerateFinalizer(classSymbol);
		bool generateAsync = GetGenerateAsync(classSymbol);

		// Check if IAsyncDisposable is available in the target framework
		if (generateAsync && context.SemanticModel.Compilation.GetTypeByMetadataName(TypeNames.IAsyncDisposable) is null)
		{
			diagnostics = new[] { type.CreateDiagnostic(DiagnosticDescriptors.AsyncDisposableNotSupportedDiagnostic, type.Name) }.ToEquatableArray();
			generateAsync = false;
		}

		bool isSealed = classDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword);

		DisposableRequest request = new(type, generateFinalizer, generateAsync, isSealed, GetDisposableFields(classDeclaration).ToEquatableArray());

		return new GeneratorResult<DisposableRequest>(request, diagnostics);
	}

	private static void Execute(SourceProductionContext context, DisposableRequest request)
	{
		TypeTargetInfo type = request.Type;
		ImmutableArray<(string FieldName, int Order, int SourceIndex)> disposableFields = request.Fields.AsImmutableArray();

		SourceBuilder sb = new();

		sb.BeginTypeFile(GeneratorNames.DisposableGeneratorFullName, type.NamespaceName, type.OuterClasses, GetUsings(request.GenerateAsync));

		AppendClassStart(sb, type.Name, type.Accessibility, request.GenerateAsync);
		AppendDisposedField(sb);
		AppendThrowIfDisposed(sb, type.Name, request.IsSealed);
		AppendDisposeBool(sb, disposableFields, request.IsSealed);
		AppendDisposePublic(sb);

		if (request.GenerateFinalizer)
			AppendFinalizer(sb, type.Name);

		if (request.GenerateAsync)
		{
			AppendDisposeAsyncCore(sb, disposableFields, request.IsSealed);
			AppendDisposeAsync(sb);
		}

		sb.CloseClass();
		sb.EndTypeFile(type.OuterClasses);

		GeneratorHelpers.AddTypeSource(context, type.OuterClasses, type.Name, "Disposable", sb);
	}

	private static List<string> GetUsings(bool generateAsync)
	{
		List<string> usings = [NamespaceNames.System];
		if (generateAsync)
			usings.Add(NamespaceNames.SystemThreadingTasks);

		return usings;
	}

	private static bool GetGenerateFinalizer(INamedTypeSymbol namedTypeSymbol)
		=> GeneratorHelpers.GetConstructorArgumentValue(namedTypeSymbol, AttributeNames.TypeName, 0, false);

	private static bool GetGenerateAsync(INamedTypeSymbol namedTypeSymbol)
		=> GeneratorHelpers.GetConstructorArgumentValue(namedTypeSymbol, AttributeNames.TypeName, 1, false);

	private static ImmutableArray<(string FieldName, int Order, int SourceIndex)> GetDisposableFields(ClassDeclarationSyntax classDeclaration)
	{
		List<(string FieldName, int Order, int SourceIndex)> fields = [];
		int sourceIndex = 0;

		foreach (MemberDeclarationSyntax member in classDeclaration.Members)
		{
			if (member is not FieldDeclarationSyntax fieldDeclaration)
				continue;

			int order = GetDisposeResourceOrder(fieldDeclaration);

			if (order < 0)
			{
				sourceIndex++;
				continue;
			}

			foreach (VariableDeclaratorSyntax variable in fieldDeclaration.Declaration.Variables)
			{
				fields.Add((variable.Identifier.Text, order, sourceIndex));
				sourceIndex++;
			}
		}

		// Non-zero orders first (ascending), then zero-order fields in source order
		fields.Sort((a, b) =>
		{
			return a.Order != 0 && b.Order != 0
				? a.Order.CompareTo(b.Order)
				: a.Order != 0 ? -1 : b.Order != 0 ? 1 : a.SourceIndex.CompareTo(b.SourceIndex);
		});

		return [.. fields];
	}

	/// <summary>
	/// Returns the order value if the field has [DisposeResource], or -1 if not present.
	/// </summary>
	private static int GetDisposeResourceOrder(FieldDeclarationSyntax fieldDeclaration)
	{
		foreach (AttributeListSyntax attributeList in fieldDeclaration.AttributeLists)
		{
			foreach (AttributeSyntax attribute in attributeList.Attributes)
			{
				string name = attribute.Name.ToString();

				if (name != DisposeResourceAttributeNames.ShortName && name != DisposeResourceAttributeNames.TypeName)
					continue;

				if (attribute.ArgumentList is null || attribute.ArgumentList.Arguments.Count == 0)
					return 0;

				AttributeArgumentSyntax firstArg = attribute.ArgumentList.Arguments[0];
				string argText = firstArg.Expression.ToString();

				return int.TryParse(argText, out int order) ? order : 0;
			}
		}

		return -1;
	}

	private static void AppendClassStart(SourceBuilder sb, string className, string accessibility, bool generateAsync)
	{
		string interfaces = generateAsync ? "IDisposable, IAsyncDisposable" : "IDisposable";
		sb.OpenClass(accessibility, className, interfaces);
	}

	private static void AppendDisposedField(SourceBuilder sb)
	{
		sb.AppendLine("private bool _disposed;");
		sb.AppendLine();
	}

	private static void AppendThrowIfDisposed(SourceBuilder sb, string className, bool isSealed)
	{
		string modifier = isSealed ? "private" : "protected";
		sb.AppendSummary("Throws <see cref=\"ObjectDisposedException\"/> if this instance has been disposed.");
		sb.AppendLine($"{modifier} void ThrowIfDisposed()");
		sb.OpenBrace();
		sb.AppendLine("if (_disposed)");
		sb.Indent();
		sb.AppendLine($"throw new ObjectDisposedException(nameof({className}));");
		sb.Outdent();
		sb.CloseBrace();
		sb.AppendLine();
	}

	private static void AppendDisposeBool(SourceBuilder sb, ImmutableArray<(string FieldName, int Order, int SourceIndex)> fields, bool isSealed)
	{
		string modifier = isSealed ? "private" : "protected virtual";
		sb.AppendSummary("Releases managed and optionally unmanaged resources.");
		sb.AppendLine("/// <param name=\"disposing\">");
		sb.AppendLine("/// <see langword=\"true\"/> to release both managed and unmanaged resources;");
		sb.AppendLine("/// <see langword=\"false\"/> to release only unmanaged resources.");
		sb.AppendLine("/// </param>");
		sb.AppendLine($"{modifier} void Dispose(bool disposing)");
		sb.OpenBrace();
		sb.AppendLine("if (!_disposed)");
		sb.OpenBrace();
		sb.AppendLine("if (disposing)");
		sb.OpenBrace();

		foreach ((string fieldName, int _, int _) in fields)
			sb.AppendLine($"{fieldName}?.Dispose();");

		sb.CloseBrace();
		sb.AppendLine();
		sb.AppendLine("_disposed = true;");
		sb.CloseBrace();
		sb.CloseBrace();
		sb.AppendLine();
	}

	private static void AppendDisposePublic(SourceBuilder sb)
	{
		sb.AppendLine("/// <inheritdoc/>");
		sb.AppendLine("public void Dispose()");
		sb.OpenBrace();
		sb.AppendLine("Dispose(true);");
		sb.AppendLine("GC.SuppressFinalize(this);");
		sb.CloseBrace();
	}

	private static void AppendFinalizer(SourceBuilder sb, string className)
	{
		sb.AppendLine();
		sb.AppendSummary($"Finalizer for <see cref=\"{className}\"/>.");
		sb.AppendLine($"~{className}()");
		sb.OpenBrace();
		sb.AppendLine("Dispose(false);");
		sb.CloseBrace();
	}

	private static void AppendDisposeAsyncCore(SourceBuilder sb, ImmutableArray<(string FieldName, int Order, int SourceIndex)> fields, bool isSealed)
	{
		string modifier = isSealed ? "private" : "protected virtual";
		sb.AppendLine();
		sb.AppendLine("/// <summary>");
		sb.AppendLine("/// Performs application-defined tasks associated with freeing, releasing,");
		sb.AppendLine("/// or resetting managed resources asynchronously.");
		sb.AppendLine("/// </summary>");
		sb.AppendLine("/// <returns>A task that represents the asynchronous dispose operation.</returns>");
		sb.AppendLine($"{modifier} async ValueTask DisposeAsyncCore()");
		sb.OpenBrace();

		foreach ((string fieldName, int _, int _) in fields)
		{
			sb.AppendLine($"if ({fieldName} is IAsyncDisposable {fieldName}Async)");
			sb.Indent();
			sb.AppendLine($"await {fieldName}Async.DisposeAsync().ConfigureAwait(false);");
			sb.Outdent();
			sb.AppendLine("else");
			sb.Indent();
			sb.AppendLine($"{fieldName}?.Dispose();");
			sb.Outdent();
			sb.AppendLine();
		}

		sb.CloseBrace();
	}

	private static void AppendDisposeAsync(SourceBuilder sb)
	{
		sb.AppendLine();
		sb.AppendLine("/// <inheritdoc/>");
		sb.AppendLine("public async ValueTask DisposeAsync()");
		sb.OpenBrace();
		sb.AppendLine("await DisposeAsyncCore().ConfigureAwait(false);");
		sb.AppendLine("Dispose(false);");
		sb.AppendLine("GC.SuppressFinalize(this);");
		sb.CloseBrace();
	}
}
