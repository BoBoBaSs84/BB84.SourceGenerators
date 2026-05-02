// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Collections.Immutable;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class GeneratorDriverTests
{
	private static readonly MetadataReference[] References =
	[
		MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location),
		MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location),
		MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
		MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
	];

	[TestMethod]
	public void EnumeratorExtensionsGeneratorShouldDetectDescriptionAttributeWhenUnqualified()
	{
		string source = @"
using System.ComponentModel;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEnumeratorExtensions]
	public enum MyEnum
	{
		[Description(""No value"")]
		None = 0,
		[DescriptionAttribute(""One value"")]
		One = 1,
		[System.ComponentModel.Description(""Two value"")]
		Two = 2,
		Three = 3
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<EnumeratorExtensionsGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);

		string generated = generatedSources.First(s => s.Contains("GetDescriptionFast(this MyEnum value)"));
		Assert.Contains("MyEnum.None => \"No value\"", generated);
		Assert.Contains("MyEnum.One => \"One value\"", generated);
		Assert.Contains("MyEnum.Two => \"Two value\"", generated);
		Assert.Contains("MyEnum.Three => nameof(MyEnum.Three)", generated);
	}

	[TestMethod]
	public void ToStringGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateToString]
  public partial class MyModel
  {
    public int Id { get; set; }
    public string Name { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ToStringGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources, "Expected at least one generated source.");
		string generated = generatedSources.First(s => s.Contains("override string ToString()"));
		Assert.Contains("Id", generated);
		Assert.Contains("Name", generated);
	}

	[TestMethod]
	public void ToStringGeneratorEmptyClassShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateToString]
	public partial class EmptyModel
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ToStringGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("override string ToString()"));
		Assert.Contains("EmptyModel", generated);
	}

	[TestMethod]
	public void ToStringGeneratorShouldExcludeNameofProperties()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateToString(""Secret"", nameof(ExcludeModel.Internal))]
	public partial class ExcludeModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Secret { get; set; }
		public string Internal { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ToStringGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("override string ToString()"));
		Assert.Contains("Id", generated);
		Assert.Contains("Name", generated);
		Assert.DoesNotContain("Secret", generated);
		Assert.DoesNotContain("Internal", generated);
	}

	[TestMethod]
	public void EqualityGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEquality]
  public partial class EqModel
  {
    public int Id { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<EqualityGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("Equals"));
		Assert.Contains("IEquatable", generated);
		Assert.Contains("GetHashCode", generated);
		Assert.Contains("operator ==", generated);
	}

	[TestMethod]
	public void EqualityGeneratorEmptyClassShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEquality]
  public partial class EmptyEqModel
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<EqualityGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	[TestMethod]
	public void EqualityGeneratorShouldExcludeNameofProperties()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateEquality(nameof(EqExModel.Secret))]
	public partial class EqExModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Secret { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<EqualityGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class EqExModel"));
		Assert.Contains("Id", generated);
		Assert.Contains("Name", generated);
		Assert.DoesNotContain("Secret", generated);
	}

	[TestMethod]
	public void BuilderGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateBuilder]
  public partial class BuildModel
  {
    public int Id { get; set; }
    public string Name { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<BuilderGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("BuildModelBuilder"));
		Assert.Contains("BuildModelBuilder", generated);
		Assert.Contains("WithId", generated);
		Assert.Contains("WithName", generated);
		Assert.Contains("Build()", generated);
	}

	[TestMethod]
	public void BuilderGeneratorEmptyClassShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateBuilder]
  public partial class EmptyBuildModel
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<BuilderGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	[TestMethod]
	public void ValidatorGeneratorShouldGenerateSource()
	{
		string source = @"
using System.ComponentModel.DataAnnotations;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateValidator]
  public partial class ValModel
  {
    [Required]
    public string Name { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ValidatorGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("Dictionary<string, List<string>> Validate()"));
		Assert.Contains("Validate()", generated);
	}

	[TestMethod]
	public void ValidatorGeneratorEmptyClassShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateValidator]
	public partial class EmptyValModel
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ValidatorGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	[TestMethod]
	public void ValidatorGeneratorAbstractClassShouldReportDiagnostic()
	{
		string source = @"
using System.ComponentModel.DataAnnotations;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateValidator]
	public abstract partial class AbstractModel
	{
		[Range(1, int.MaxValue)]
		public int Id { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ValidatorGenerator>(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0001"));
	}

	[TestMethod]
	public void ValidatorGeneratorShouldIncludeInheritedProperties()
	{
		string source = @"
using System.ComponentModel.DataAnnotations;
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public abstract class AbstractModel
	{
		[Range(1, int.MaxValue)]
		public int Id { get; set; }
	}

	[GenerateValidator]
	public partial class ConcreteModel : AbstractModel
	{
		[Required]
		public string Name { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<ValidatorGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class ConcreteModel"));
		Assert.Contains("Name", generated);
		Assert.Contains("Id", generated);
	}

	[TestMethod]
	public void CloneableGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateCloneable]
  public partial class CloneModel
  {
    public int Id { get; set; }
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<CloneableGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("DeepClone"));
		Assert.Contains("Clone()", generated);
		Assert.Contains("DeepClone()", generated);
	}

	[TestMethod]
	public void CloneableGeneratorShouldExcludeNameofProperties()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateCloneable(nameof(CloneExModel.Secret))]
	public partial class CloneExModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Secret { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<CloneableGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class CloneExModel"));
		Assert.Contains("Id", generated);
		Assert.Contains("Name", generated);
		Assert.DoesNotContain("Secret", generated);
	}

	[TestMethod]
	public void EnumeratorExtensionsGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEnumeratorExtensions]
  public enum MyEnum
  {
    None = 0,
    One = 1,
    Two = 2
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<EnumeratorExtensionsGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("MyEnumExtensions"));
		Assert.Contains("ToStringFast", generated);
		Assert.Contains("IsDefinedFast", generated);
		Assert.Contains("GetNamesFast", generated);
		Assert.Contains("GetValuesFast", generated);
	}

	[TestMethod]
	public void EnumeratorExtensionsGeneratorEmptyEnumShouldNotGenerate()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateEnumeratorExtensions]
  public enum EmptyEnum
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<EnumeratorExtensionsGenerator>(source);

		// Empty enum should not produce generator output (generator returns early)
		string[] nonAttributeSources = [.. generatedSources.Where(s => !s.Contains("class") || s.Contains("Extensions")).Where(s => s.Contains("EmptyEnumExtensions"))];
		Assert.IsEmpty(nonAttributeSources, "Empty enum should not produce extension methods.");
	}

	[TestMethod]
	public void NotificationPropertiesGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateNotifications]
  public partial class NotifyModel
  {
    private int _id;
    private string _name;
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<NotificationsGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("INotifyPropertyChanged"));
		Assert.Contains("INotifyPropertyChanging", generated);
	}

	[TestMethod]
	public void AssemblyInformationGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
  [GenerateAssemblyInformation]
  public partial class InfoModel
  {
  }
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<AssemblyInformationGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("const string Title"));
		Assert.Contains("const string Version", generated);
		Assert.Contains("const string Company", generated);
		Assert.Contains("const string Copyright", generated);
		Assert.Contains("const string FileVersion", generated);
		Assert.Contains("const string InformationalVersion", generated);
	}

	[TestMethod]
	public void SingletonGeneratorShouldGenerateLazySource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateSingleton]
	public partial class MySingleton
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<SingletonGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class MySingleton"));
		Assert.Contains("Lazy<MySingleton>", generated);
		Assert.Contains("Instance", generated);
		Assert.Contains("private MySingleton()", generated);
	}

	[TestMethod]
	public void SingletonGeneratorShouldGenerateNonLazySource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateSingleton(false)]
	public partial class MyNonLazySingleton
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<SingletonGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class MyNonLazySingleton"));
		Assert.DoesNotContain("System.Lazy<", generated);
		Assert.Contains("Instance { get; } = new MyNonLazySingleton();", generated);
		Assert.Contains("private MyNonLazySingleton()", generated);
	}

	[TestMethod]
	public void SingletonGeneratorShouldUseInterfaceType()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IMyService { }

	[GenerateSingleton]
	public partial class MyService : IMyService
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<SingletonGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class MyService"));
		Assert.Contains("IMyService Instance", generated);
	}

	[TestMethod]
	public void SingletonGeneratorShouldReportDiagnosticForRequiredProperty()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateSingleton]
	public partial class BadSingleton
	{
		public required string Name { get; set; }
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<SingletonGenerator>(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0002"));
	}

	[TestMethod]
	public void DecoratorGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IMyService
	{
		string GetValue();
		string Name { get; set; }
	}

	[GenerateDecorator]
	public partial class MyServiceDecorator : IMyService
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<DecoratorGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class MyServiceDecorator"));
		Assert.Contains("_inner", generated);
		Assert.Contains("virtual", generated);
		Assert.Contains("GetValue()", generated);
		Assert.Contains("Name", generated);
		Assert.Contains("MyServiceDecorator(IMyService inner)", generated);
	}

	[TestMethod]
	public void DecoratorGeneratorShouldReportDiagnosticForNoInterface()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	[GenerateDecorator]
	public partial class BadDecorator
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<DecoratorGenerator>(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0003"));
	}

	[TestMethod]
	public void DecoratorGeneratorShouldHandleMultipleInterfaces()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IFirst
	{
		void DoFirst();
	}

	public interface ISecond
	{
		int DoSecond();
	}

	[GenerateDecorator]
	public partial class MultiDecorator : IFirst, ISecond
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<DecoratorGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class MultiDecorator"));
		Assert.Contains("DoFirst()", generated);
		Assert.Contains("DoSecond()", generated);
	}

	[TestMethod]
	public void FactoryGeneratorShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IAnimal { }

	public class Dog : IAnimal { }
	public class Cat : IAnimal { }

	[GenerateFactory(typeof(IAnimal))]
	public static partial class AnimalFactory
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<FactoryGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class AnimalFactory"));
		Assert.Contains("Create(string key)", generated);
		Assert.Contains("Dog", generated);
		Assert.Contains("Cat", generated);
		Assert.Contains("GetKeys()", generated);
	}

	[TestMethod]
	public void FactoryGeneratorShouldUseCustomKey()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IVehicle { }

	[GenerateFactoryKey(""sedan"")]
	public class Car : IVehicle { }

	[GenerateFactory(typeof(IVehicle))]
	public static partial class VehicleFactory
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<FactoryGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
		string generated = generatedSources.First(s => s.Contains("partial class VehicleFactory"));
		Assert.Contains("sedan", generated);
	}

	[TestMethod]
	public void FactoryGeneratorShouldReportDiagnosticForNonInterfaceType()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public class NotAnInterface { }

	[GenerateFactory(typeof(NotAnInterface))]
	public static partial class BadFactory
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<FactoryGenerator>(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0004"));
	}

	[TestMethod]
	public void FactoryGeneratorShouldReportDiagnosticForDuplicateKeys()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IService { }

	[GenerateFactoryKey(""same"")]
	public class ServiceA : IService { }

	[GenerateFactoryKey(""same"")]
	public class ServiceB : IService { }

	[GenerateFactory(typeof(IService))]
	public static partial class ServiceFactory
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<FactoryGenerator>(source);

		Assert.IsNotEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "BB84SG0005"));
	}

	[TestMethod]
	public void FactoryGeneratorNoImplementationsShouldGenerateSource()
	{
		string source = @"
using BB84.SourceGenerators.Attributes;

namespace TestNamespace
{
	public interface IEmpty { }

	[GenerateFactory(typeof(IEmpty))]
	public static partial class EmptyFactory
	{
	}
}";

		(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources) = RunGenerator<FactoryGenerator>(source);

		Assert.IsEmpty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
		Assert.IsNotEmpty(generatedSources);
	}

	private static (ImmutableArray<Diagnostic> Diagnostics, string[] GeneratedSources) RunGenerator<TGenerator>(string source)
		where TGenerator : IIncrementalGenerator, new()
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "TestAssembly",
			syntaxTrees: [syntaxTree],
			references: References,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		// AttributeSourceGenerator must run to inject attribute definitions into the compilation
		IIncrementalGenerator[] generators = [new AttributeSourceGenerator(), new TGenerator()];

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		string[] generatedSources = [.. runResult.GeneratedTrees.Select(t => t.GetText().ToString())];

		return (diagnostics, generatedSources);
	}
}
