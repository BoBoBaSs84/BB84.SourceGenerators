namespace BB84.SourceGenerators.Helpers;

/// <summary>
/// Represents the full names of the source generators in this project. This is used to identify
/// the generators when they are invoked by the compiler, and to avoid hardcoding the full names
/// in multiple places in the codebase.
/// </summary>
internal static class GeneratorNames
{
	internal static readonly string AbstractionGeneratorFullName = typeof(AbstractionGenerator).FullName;
	internal static readonly string AssemblyInformationGeneratorFullName = typeof(AssemblyInformationGenerator).FullName;
	internal static readonly string AttributeSourceGeneratorFullName = typeof(AttributeSourceGenerator).FullName;
	internal static readonly string AutoMapperGeneratorFullName = typeof(AutoMapperGenerator).FullName;
	internal static readonly string BuilderGeneratorFullName = typeof(BuilderGenerator).FullName;
	internal static readonly string CloneableGeneratorFullName = typeof(CloneableGenerator).FullName;
	internal static readonly string DecoratorGeneratorFullName = typeof(DecoratorGenerator).FullName;
	internal static readonly string DisposableGeneratorFullName = typeof(DisposableGenerator).FullName;
	internal static readonly string EnumeratorExtensionsGeneratorFullName = typeof(EnumeratorExtensionsGenerator).FullName;
	internal static readonly string EqualityGeneratorFullName = typeof(EqualityGenerator).FullName;
	internal static readonly string FactoryGeneratorFullName = typeof(FactoryGenerator).FullName;
	internal static readonly string IniFileGeneratorFullName = typeof(IniFileGenerator).FullName;
	internal static readonly string NotificationsGeneratorFullName = typeof(NotificationsGenerator).FullName;
	internal static readonly string SingletonGeneratorFullName = typeof(SingletonGenerator).FullName;
	internal static readonly string ToStringGeneratorFullName = typeof(ToStringGenerator).FullName;
	internal static readonly string ValidatorGeneratorFullName = typeof(ValidatorGenerator).FullName;
}
