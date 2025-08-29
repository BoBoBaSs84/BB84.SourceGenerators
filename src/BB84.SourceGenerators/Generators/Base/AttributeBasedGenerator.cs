// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Generators.Base;

/// <summary>
/// Represents a base class for incremental generators that process syntax nodes based on a specific attribute.
/// </summary>
/// <typeparam name="T">The type of attribute that the generator will look for.</typeparam>
public abstract class AttributeBasedGenerator<T> : IIncrementalGenerator
	where T : Attribute
{
	/// <summary>
	/// Gets the name of the attribute that the generator will look for in the syntax nodes.
	/// </summary>
	public string AttributeFullName => typeof(T).FullName;

	/// <summary>
	/// Gets the name of the generator.
	/// </summary>
	public abstract string GeneratorName { get; }

	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValuesProvider<SyntaxNode> provider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				fullyQualifiedMetadataName: AttributeFullName,
				predicate: (node, _) => Predicate(node),
				transform: (context, _) => Transform(context.TargetNode))
			.Where(node => node is not null);

		context.RegisterSourceOutput(provider, Execute);
	}

	/// <summary>
	/// Represents a predicate that determines whether the generator should process the given syntax node.
	/// </summary>
	/// <param name="node">The syntax node to evaluate.</param>
	/// <returns>True if the generator should process the node; otherwise, false.</returns>
	protected abstract bool Predicate(SyntaxNode node);

	/// <summary>
	/// Responsible for transforming the syntax node in the context of the generator.
	/// </summary>
	/// <param name="targetNode">
	/// The syntax node to process. This node should be a valid syntax node that matches the
	/// predicate defined in the generator.
	/// </param>
	/// <returns>
	/// A transformed syntax node that will be processed by the generator.
	/// </returns>
	protected abstract SyntaxNode Transform(SyntaxNode targetNode);

	/// <summary>
	/// Responsible for executing the generator logic on the provided syntax node.
	/// </summary>
	/// <param name="context">
	/// The source production context that provides methods to report diagnostics and generate
	/// source files.
	/// </param>
	/// <param name="syntaxNode">
	/// The syntax node to process. This node should be a valid syntax node that matches the
	/// predicate defined in the generator.
	/// </param>
	protected abstract void Execute(SourceProductionContext context, SyntaxNode syntaxNode);
}
