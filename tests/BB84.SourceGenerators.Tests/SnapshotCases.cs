// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using Microsoft.CodeAnalysis;

namespace BB84.SourceGenerators.Tests;

/// <summary>
/// A single snapshot input.
/// </summary>
/// <param name="Source">The C# source to compile.</param>
/// <param name="Nullable">The nullable context of the compilation.</param>
internal sealed record SnapshotCase(string Source, NullableContextOptions Nullable = NullableContextOptions.Disable);

/// <summary>
/// The inputs of <see cref="SnapshotTests"/>, keyed by generator and case name.
/// </summary>
internal static class SnapshotCases
{
	/// <summary>
	/// Disposable input that requests async disposal without <c>IAsyncDisposable</c> being available.
	/// </summary>
	internal const string DisposableAsyncUnsupported = """
		using BB84.SourceGenerators.Attributes;

		namespace TestNamespace
		{
			[GenerateDisposable(async: true)]
			public partial class UnsupportedAsyncModel
			{
				[DisposeResource]
				private readonly System.IDisposable _resource;
			}
		}
		""";

#if NET
	internal static readonly Dictionary<string, SnapshotCase> Abstraction = new()
	{
		["Plain"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public static class Calculator
				{
					public static int Value { get; set; }
					public static int ReadOnlyValue { get; }
					public static int WriteOnlyValue { set { } }
					public static int Add(int a, int b) => a + b;
					public static void Reset() { }
				}

				[GenerateAbstraction(typeof(Calculator), typeof(ICalculatorProvider), typeof(CalculatorProvider))]
				internal sealed partial class CalculatorProvider { }

				public partial interface ICalculatorProvider { }
			}
			"""),
		["Excludes"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public static class Service
				{
					public static int Visible { get; set; }
					public static int Hidden { get; set; }
					public static void Keep() { }
					public static void Drop() { }
				}

				[GenerateAbstraction(typeof(Service), typeof(IServiceProvider2), typeof(ServiceProvider2),
					ExcludeProperties = new string[] { "Hidden" }, ExcludeMethods = new[] { "Drop" })]
				internal sealed partial class ServiceProvider2 { }

				public partial interface IServiceProvider2 { }
			}
			"""),
		["Modifiers"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public static class Modifiers
				{
					public static bool TryParse(string text, out int result) { result = 0; return false; }
					public static int Sum(params int[] values) => 0;
					public static void ByRef(ref int x) { }
					public static void ByIn(in int x) { }
					public static void ByRefReadOnly(ref readonly int x) { }
					public static int Defaulted(int value = 42, string name = "x") => value;
				}

				[GenerateAbstraction(typeof(Modifiers), typeof(IModifiersProvider), typeof(ModifiersProvider))]
				internal sealed partial class ModifiersProvider { }

				public partial interface IModifiersProvider { }
			}
			"""),
		["NullableAndGeneric"] = new("""
			using System.Collections.Generic;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public static class Store
				{
					public static string? Current { get; set; }
					public static List<string> Items { get; } = new();
					public static T Echo<T>(T value) where T : class => value;
					public static string? Find(string? key) => key;
					public static IReadOnlyDictionary<string, int?> Map(IEnumerable<KeyValuePair<string, int?>> pairs) => null!;
				}

				[GenerateAbstraction(typeof(Store), typeof(IStoreProvider), typeof(StoreProvider))]
				public partial class StoreProvider { }

				public partial interface IStoreProvider { }
			}
			""", NullableContextOptions.Enable),
		["OtherNamespace"] = new("""
			using BB84.SourceGenerators.Attributes;
			using Abstractions;

			namespace Abstractions
			{
				public partial interface IClockProvider { }
			}

			namespace TestNamespace
			{
				public static class Clock
				{
					public static long Ticks { get; set; }
					public static long Now() => 0;
				}

				[GenerateAbstraction(typeof(Clock), typeof(IClockProvider), typeof(ClockProvider))]
				internal sealed partial class ClockProvider { }
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> ActionCommand = new()
	{
		["Sync"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class ViewModel
				{
					private bool CanSave => true;
					private bool CanSelect(int value) => value > 0;

					[ActionCommand]
					private void Save() { }

					[ActionCommand(CanExecute = nameof(CanSave))]
					private void SaveGuarded() { }

					[ActionCommand(Name = "PickCommand", CanExecute = nameof(CanSelect))]
					private void Select(int value) { }

					[ActionCommand]
					public void Rename(string? name) { }
				}
			}
			""", NullableContextOptions.Enable),
		["Async"] = new("""
			using System;
			using System.Threading;
			using System.Threading.Tasks;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				internal partial class ViewModel
				{
					private bool CanRun => true;

					[ActionCommand]
					private Task LoadAsync() => Task.CompletedTask;

					[ActionCommand]
					private Task LoadWithTokenAsync(CancellationToken ct) => Task.CompletedTask;

					[ActionCommand]
					private Task SendAsync(string value) => Task.CompletedTask;

					[ActionCommand(IncludeCancelCommand = true)]
					private Task SendWithTokenAsync(string value, CancellationToken ct) => Task.CompletedTask;

					[ActionCommand(Name = "GoCommand", CanExecute = nameof(CanRun), ExceptionHandler = nameof(OnError), AllowConcurrentExecutions = true, IncludeCancelCommand = true)]
					private Task RunAsync() => Task.CompletedTask;

					private void OnError(Exception exception) { }
				}
			}
			""", NullableContextOptions.Enable),
		["StaticAndNested"] = new("""
			using System.Threading.Tasks;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public static partial class Commands
				{
					private static bool CanPing => true;

					[ActionCommand(CanExecute = nameof(CanPing))]
					private static void Ping() { }

					[ActionCommand(IncludeCancelCommand = true)]
					private static Task PongAsync() => Task.CompletedTask;
				}

				public partial class Outer
				{
					internal partial record InnerModel
					{
						[ActionCommand]
						private void Run() { }
					}
				}
			}
			""", NullableContextOptions.Enable),
		["NotifyCanExecuteChanged"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateNotifications]
				public partial class ViewModel
				{
					[NotifyCanExecuteChanged(nameof(RunCommand))]
					private bool _canRun;

					[NotifyCanExecuteChanged(nameof(RunCommand), nameof(StopCommand))]
					public partial bool IsReady { get; set; }

					[NotifyCanExecuteChanged("MissingCommand")]
					private int _unknown;

					[ActionCommand(CanExecute = nameof(CanRun))]
					private void Run() { }

					[ActionCommand(CanExecute = nameof(IsReady))]
					private void Stop() { }
				}
			}
			""", NullableContextOptions.Enable),
		["UnsupportedSignatures"] = new("""
			using System.Threading;
			using System.Threading.Tasks;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class ViewModel
				{
					[ActionCommand]
					private int ReturnsValue() => 0;

					[ActionCommand]
					private void TwoParameters(int a, int b) { }

					[ActionCommand]
					private void ByRef(ref int a) { }

					[ActionCommand]
					private void Generic<T>(T value) { }

					[ActionCommand]
					private Task<int> ReturnsTaskOfT() => Task.FromResult(0);

					[ActionCommand]
					private Task WrongOrderAsync(CancellationToken a, int b) => Task.CompletedTask;
				}

				public class NotPartialViewModel
				{
					[ActionCommand]
					private void Run() { }
				}
			}
			""", NullableContextOptions.Enable),
		["InvalidOptions"] = new("""
			using System;
			using System.Threading.Tasks;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class ViewModel
				{
					private int NotBool => 0;
					private bool WrongParameter(string value) => true;
					private bool InstanceGuard => true;
					private void WrongSignature(string message) { }
					private void OnError(Exception exception) { }

					[ActionCommand(CanExecute = "Missing")]
					private void MissingGuard(int value) { }

					[ActionCommand(CanExecute = nameof(NotBool))]
					private void NotBoolGuard(int value) { }

					[ActionCommand(CanExecute = nameof(WrongParameter))]
					private void WrongParameterGuard(int value) { }

					[ActionCommand(CanExecute = nameof(InstanceGuard))]
					private static void StaticWithInstanceGuard() { }

					[ActionCommand(ExceptionHandler = "Missing")]
					private Task MissingHandlerAsync() => Task.CompletedTask;

					[ActionCommand(ExceptionHandler = nameof(WrongSignature))]
					private Task WrongHandlerAsync() => Task.CompletedTask;

					[ActionCommand(ExceptionHandler = nameof(OnError), AllowConcurrentExecutions = true, IncludeCancelCommand = true)]
					private void AsyncOptionsOnSync() { }
				}
			}
			""", NullableContextOptions.Enable),
		["NameConflicts"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class PropertyConflict
				{
					public object RunCommand => null!;

					[ActionCommand]
					private void Run() { }
				}

				public partial class FieldConflict
				{
					private object _runCommand = null!;

					[ActionCommand]
					private void Run() { }
				}

				public partial class CommandConflict
				{
					[ActionCommand]
					private void Run(int value) { }

					[ActionCommand]
					private void Run() { }
				}
			}
			""", NullableContextOptions.Enable),
	};

	internal static readonly Dictionary<string, SnapshotCase> AssemblyInformation = new()
	{
		["Plain"] = new("""
			using BB84.SourceGenerators.Attributes;

			[assembly: System.Reflection.AssemblyTitle("Test Assembly")]
			[assembly: System.Reflection.AssemblyDescription("A \"quoted\" description")]
			[assembly: System.Reflection.AssemblyCompany("Company")]
			[assembly: System.Reflection.AssemblyProduct("Product")]
			[assembly: System.Reflection.AssemblyCopyright("Copyright")]
			[assembly: System.Reflection.AssemblyVersion("1.2.3.4")]
			[assembly: System.Reflection.AssemblyFileVersion("1.2.3.5")]
			[assembly: System.Reflection.AssemblyInformationalVersion("1.2.3-beta")]
			[assembly: System.Reflection.AssemblyMetadata("BuildCommit", "abc123")]
			[assembly: System.Reflection.AssemblyMetadata("BuildHash", "0x001122")]
			[assembly: System.CLSCompliant(true)]
			[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FriendA")]
			[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FriendB")]

			namespace TestNamespace
			{
				[GenerateAssemblyInformation]
				public partial class InfoModel { }
			}
			"""),
		["Nested"] = new("""
			using BB84.SourceGenerators.Attributes;

			[assembly: System.Reflection.AssemblyTitle("Nested")]

			namespace TestNamespace
			{
				internal static partial class Outer
				{
					[GenerateAssemblyInformation]
					internal static partial class Info { }
				}
			}
			"""),
		["NoAssemblyAttributes"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateAssemblyInformation]
				internal sealed partial class InfoModel { }
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> AutoMapper = new()
	{
		["Plain"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public class BaseDto
				{
					public int Id { get; set; }
				}

				public class SourceDto : BaseDto
				{
					public string CustomerName { get; set; }
					public double Price { get; set; }
					public int Ignored { get; set; }
				}

				public class TargetModel
				{
					public int Id { get; set; }
					public string Name { get; set; }
					public double Price { get; set; }
					public int ReadOnly { get; }
				}

				public partial class OrderMapper
				{
					[GenerateAutoMapper]
					[PropertyMapping("CustomerName", "Name")]
					public partial TargetModel Map(SourceDto source);

					[GenerateAutoMapper]
					internal partial SourceDto MapBack(SourceDto source);
				}
			}
			"""),
		["NestedInStaticOuter"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public class SourceDto { public int Id { get; set; } }
				public class TargetModel { public int Id { get; set; } }

				public static partial class Outer
				{
					internal partial class Middle
					{
						public static partial class Mappers
						{
							[GenerateAutoMapper]
							public static partial TargetModel Map(SourceDto source);
						}
					}
				}
			}
			"""),
		["Nullable"] = new("""
			using System.Collections.Generic;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public class SourceDto
				{
					public int Id { get; set; }
					public string? Name { get; set; }
					public List<string>? Tags { get; set; }
					public int? Count { get; set; }
				}

				public class TargetModel
				{
					public int Id { get; set; }
					public string? Name { get; set; }
					public List<string>? Tags { get; set; }
					public int? Count { get; set; }
				}

				public partial class OrderMapper
				{
					[GenerateAutoMapper]
					public partial TargetModel Map(SourceDto? source);
				}
			}
			""", NullableContextOptions.Enable),
		["PropertyDiagnostics"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public class SourceDto
				{
					public string Id { get; set; }
				}

				public class TargetModel
				{
					public int Id { get; set; }
					public string Name { get; set; }
				}

				public partial class OrderMapper
				{
					[GenerateAutoMapper]
					public partial TargetModel Map(SourceDto source);
				}
			}
			"""),
		["PartialDiagnostics"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public class SourceDto { public int Id { get; set; } }
				public class TargetModel { public int Id { get; set; } }

				public partial class NonPartialMethodMapper
				{
					[GenerateAutoMapper]
					public TargetModel Map(SourceDto source) => new TargetModel();
				}

				public class NonPartialClassMapper
				{
					[GenerateAutoMapper]
					public partial TargetModel Map(SourceDto source);
				}
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> Builder = new()
	{
		["Plain"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateBuilder]
				public partial class BuildModel
				{
					public int Id { get; set; }
					public string Name { get; set; }
					public int ReadOnly { get; }
					public static int Shared { get; set; }
				}
			}
			"""),
		["Empty"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateBuilder]
				internal sealed partial class EmptyBuildModel { }
			}
			"""),
		["NestedAndNullable"] = new("""
			using System.Collections.Generic;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class Outer
				{
					internal partial class Middle
					{
						[GenerateBuilder]
						public partial class Inner
						{
							public string? Name { get; set; }
							public int? Age { get; set; }
							public List<string>? Tags { get; set; }
							public Dictionary<string, List<int>> Map { get; set; } = new();
						}
					}
				}
			}
			""", NullableContextOptions.Enable),
	};

	internal static readonly Dictionary<string, SnapshotCase> Cloneable = new()
	{
		["Class"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateCloneable]
				public partial class Item { public int Value { get; set; } }

				[GenerateCloneable("Secret")]
				public partial class Model
				{
					public int Id { get; set; }
					public string Name { get; set; }
					public int Secret { get; set; }
					public Item Child { get; set; }
					public object Other { get; set; }
					public int ReadOnly { get; }
				}
			}
			"""),
		["Collections"] = new("""
			using System.Collections.Generic;
			using System.Collections.Immutable;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateCloneable]
				public partial class Item { public int Value { get; set; } }

				[GenerateCloneable]
				public partial class Bag
				{
					public List<Item> Items { get; set; } = new();
					public List<int>? Numbers { get; set; }
					public List<Item>? NullableItems { get; set; }
					public Dictionary<string, Item> Map { get; set; } = new();
					public Dictionary<string, int>? Counts { get; set; }
					public Item[] ItemArray { get; set; } = [];
					public int[]? Data { get; set; }
					public ImmutableArray<int> Values { get; set; }
					public ImmutableArray<Item> ImmutableItems { get; set; }
					public IReadOnlyList<Item> ReadOnlyItems { get; set; } = [];
					public IReadOnlyList<int> ReadOnlyNumbers { get; set; } = [];
					public Item? Child { get; set; }
				}
			}
			""", NullableContextOptions.Enable),
		["Struct"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateCloneable]
				public partial struct Point
				{
					public int X { get; set; }
					public int Y { get; set; }
				}
			}
			"""),
		["Record"] = new("""
			using System.Collections.Generic;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateCloneable]
				public partial record RecordModel
				{
					public int Id { get; set; }
					public List<int> Items { get; set; } = new();
				}
			}
			"""),
		["Nested"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class Outer
				{
					internal static partial class Middle
					{
						[GenerateCloneable]
						internal sealed partial class Inner { public int Id { get; set; } }
					}
				}
			}
			"""),
		["NestedStruct"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class Outer
				{
					[GenerateCloneable]
					internal partial struct Inner { public int Id { get; set; } }
				}
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> Decorator = new()
	{
		["Plain"] = new("""
			using System;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public interface IMyService
				{
					string GetValue();
					void DoWork();
					string Name { get; set; }
					int ReadOnly { get; }
					event EventHandler Changed;
				}

				[GenerateDecorator]
				public partial class MyServiceDecorator : IMyService { }
			}
			"""),
		["MultipleInterfacesAndGenerics"] = new("""
			using System.Collections.Generic;
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
					T Transform<T>(T input) where T : class;
					TResult Convert<TInput, TResult>(TInput input) where TResult : new();
					string? Find(IReadOnlyList<string?> values);
				}

				[GenerateDecorator]
				internal sealed partial class MultiDecorator : IFirst, ISecond { }
			}
			""", NullableContextOptions.Enable),
		["Modifiers"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public interface IModifiers
				{
					bool TryParse(string text, out int result);
					int Sum(params int[] values);
					void ByRef(ref int x);
					void ByIn(in int x);
					void ByRefReadOnly(ref readonly int x);
				}

				[GenerateDecorator]
				public partial class ModifiersDecorator : IModifiers { }
			}
			"""),
		["Nested"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public interface IService
				{
					void Run();
				}

				public partial class Outer
				{
					[GenerateDecorator]
					private partial class InnerDecorator : IService { }
				}
			}
			"""),
		["NoInterface"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateDecorator]
				public partial class BadDecorator { }
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> Disposable = new()
	{
		["Plain"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateDisposable]
				public partial class BasicModel
				{
					[DisposeResource]
					private readonly System.IDisposable _resource;

					private readonly System.IDisposable _notDisposed;
				}
			}
			"""),
		["SealedFinalizerAsync"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateDisposable]
				public sealed partial class SealedModel
				{
					[DisposeResource]
					private readonly System.IDisposable _resource;
				}

				[GenerateDisposable(generateFinalizer: true)]
				public partial class FinalizerModel
				{
					[DisposeResource]
					private readonly System.IDisposable _resource;
				}

				[GenerateDisposable(true, true)]
				public partial class AsyncModel
				{
					[DisposeResource]
					private readonly System.IDisposable _resource;
				}

				[GenerateDisposable(async: true)]
				public sealed partial class SealedAsyncModel
				{
					[DisposeResource]
					private readonly System.IDisposable _resource;
				}
			}
			"""),
		["Ordered"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateDisposable]
				public partial class OrderedModel
				{
					[DisposeResource]
					private readonly System.IDisposable _first;

					[DisposeResource(order: 1)]
					private readonly System.IDisposable _second;

					[DisposeResource(2)]
					private readonly System.IDisposable _third;

					[DisposeResource(order: 1)]
					private readonly System.IDisposable _fourth;
				}
			}
			"""),
		["Nested"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class Outer
				{
					internal static partial class Middle
					{
						[GenerateDisposable]
						internal partial class Inner
						{
							[DisposeResource]
							private readonly System.IDisposable _resource;
						}
					}
				}
			}
			"""),
		["Empty"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateDisposable]
				internal partial class EmptyModel { }
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> EnumeratorExtensions = new()
	{
		["Plain"] = new("""
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
			}
			"""),
		["Descriptions"] = new("""
			using System.ComponentModel;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateEnumeratorExtensions]
				internal enum MyEnum
				{
					[Description("No value")]
					None = 0,
					[DescriptionAttribute("One value")]
					One = 1,
					[System.ComponentModel.Description("Two value")]
					Two = 2,
					Three = 3
				}
			}
			"""),
		["FlagsAndUnderlyingType"] = new("""
			using System;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[Flags]
				[GenerateEnumeratorExtensions]
				public enum Permissions : long
				{
					None = 0,
					Read = 1,
					Write = 2,
					Execute = 4,
					All = Read | Write | Execute
				}
			}
			"""),
		["Nested"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class Outer
				{
					[GenerateEnumeratorExtensions]
					public enum InnerEnum
					{
						A,
						B
					}
				}
			}
			"""),
		["Empty"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateEnumeratorExtensions]
				public enum EmptyEnum { }
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> Equality = new()
	{
		["Plain"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateEquality]
				public partial class EqModel
				{
					public int Id { get; set; }
					public string Name { get; set; }
					public double Value { get; set; }
				}

				[GenerateEquality]
				internal sealed partial class EmptyEqModel { }
			}
			"""),
		["ExcludesAndInheritance"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public class BaseModel
				{
					public int BaseId { get; set; }
				}

				[GenerateEquality(nameof(Secret), "Internal")]
				public partial class ExcludeModel : BaseModel
				{
					public int Id { get; set; }
					public string Secret { get; set; }
					public string Internal { get; set; }
				}

				[GenerateEquality(IncludeInherited = false)]
				public partial class OwnOnlyModel : BaseModel
				{
					public string Name { get; set; }
				}
			}
			"""),
		["Collections"] = new("""
			using System.Collections.Generic;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateEquality]
				public partial class CollectionModel
				{
					public List<string> Tags { get; set; }
					public int[] Codes { get; set; }
					public Dictionary<string, int> Scores { get; set; }
					public IReadOnlyDictionary<string, int> ReadOnlyScores { get; set; }
					public IEnumerable<int> Sequence { get; set; }
				}
			}
			"""),
		["NestedAndNullable"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class Outer
				{
					[GenerateEquality]
					internal partial class Inner
					{
						public string? Name { get; set; }
						public int? Count { get; set; }
					}
				}
			}
			""", NullableContextOptions.Enable),
		["Record"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateEquality]
				public partial record RecordModel
				{
					public int Id { get; set; }
				}
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> Factory = new()
	{
		["Plain"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public interface IAnimal { }
				public class Dog : IAnimal { }
				public class Cat : IAnimal { }
				public abstract class AbstractAnimal : IAnimal { }

				[GenerateFactory(typeof(IAnimal))]
				public static partial class AnimalFactory { }
			}
			"""),
		["CustomKeys"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public interface IVehicle { }

				[GenerateFactoryKey("sedan")]
				public class Car : IVehicle { }

				public class Truck : IVehicle { }

				[GenerateFactory(typeof(IVehicle))]
				internal static partial class VehicleFactory { }
			}
			"""),
		["Nested"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public interface IShape { }
				public class Circle : IShape { }

				public static partial class Outer
				{
					[GenerateFactory(typeof(IShape))]
					public static partial class ShapeFactory { }
				}
			}
			"""),
		["NoImplementations"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public interface IEmpty { }

				[GenerateFactory(typeof(IEmpty))]
				public static partial class EmptyFactory { }
			}
			"""),
		["Diagnostics"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public class NotAnInterface { }

				[GenerateFactory(typeof(NotAnInterface))]
				public static partial class BadFactory { }

				public interface IService { }

				[GenerateFactoryKey("same")]
				public class ServiceA : IService { }

				[GenerateFactoryKey("same")]
				public class ServiceB : IService { }

				[GenerateFactory(typeof(IService))]
				public static partial class ServiceFactory { }
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> IniFile = new()
	{
		["AllTypes"] = new("""
			using System;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateIniFile]
				internal sealed partial class AllTypesIni
				{
					[GenerateIniFileSection]
					public AllTypesSection AllTypes { get; set; } = new();
				}

				public class AllTypesSection
				{
					[GenerateIniFileValue] public string StringValue { get; set; }
					[GenerateIniFileValue] public int IntValue { get; set; }
					[GenerateIniFileValue] public long LongValue { get; set; }
					[GenerateIniFileValue] public float FloatValue { get; set; }
					[GenerateIniFileValue] public double DoubleValue { get; set; }
					[GenerateIniFileValue] public bool BoolValue { get; set; }
					[GenerateIniFileValue] public decimal DecimalValue { get; set; }
					[GenerateIniFileValue] public DateTime DateTimeValue { get; set; }
					[GenerateIniFileValue] public Guid GuidValue { get; set; }
					[GenerateIniFileValue] public TimeSpan TimeSpanValue { get; set; }
					[GenerateIniFileValue] public DateTimeOffset DateTimeOffsetValue { get; set; }
					public int NotAValue { get; set; }
				}
			}
			"""),
		["NestedSections"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateIniFile]
				public partial class NestedIni
				{
					[GenerateIniFileSection]
					public TopSection Section { get; set; } = new();
				}

				[GenerateIniFile(sectionDelimiter: "/")]
				public partial class DelimitedIni
				{
					[GenerateIniFileSection]
					public TopSection Section { get; set; } = new TopSection();
				}

				public class TopSection
				{
					[GenerateIniFileValue]
					public string Domain { get; set; }

					[GenerateIniFileSection]
					public SubSection SubSection { get; set; } = new();

					[GenerateIniFileSection("Other")]
					public SubSection OtherSubSection { get; set; } = new();
				}

				public class SubSection
				{
					[GenerateIniFileValue]
					public string Foo { get; set; }

					[GenerateIniFileSection]
					public LeafSection Leaf { get; set; } = new();
				}

				public class LeafSection
				{
					[GenerateIniFileValue]
					public int Depth { get; set; }
				}
			}
			"""),
		["EnumsAndFlags"] = new("""
			using System;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public enum LogLevel { Debug, Info, Warning, Error }

				[Flags]
				public enum Permissions { None = 0, Read = 1, Write = 2, Execute = 4 }

				[GenerateIniFile]
				internal sealed partial class EnumIni
				{
					[GenerateIniFileSection]
					public AppSection App { get; set; } = new();
				}

				public class AppSection
				{
					[GenerateIniFileValue] public LogLevel Level { get; set; }
					[GenerateIniFileValue] public Permissions Perms { get; set; }
				}
			}
			"""),
		["Collections"] = new("""
			using System;
			using System.Collections.Generic;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateIniFile]
				internal sealed partial class CollectionIni
				{
					[GenerateIniFileSection]
					public CollectionsSection Collections { get; set; } = new();
				}

				public class CollectionsSection
				{
					[GenerateIniFileValue] public List<int> Numbers { get; set; } = new();
					[GenerateIniFileValue] public List<string> Names { get; set; } = new();
					[GenerateIniFileValue] public string[] Tags { get; set; } = [];
					[GenerateIniFileValue] public double[] Ratios { get; set; } = [];
					[GenerateIniFileValue] public List<TimeSpan> Durations { get; set; } = new();
					[GenerateIniFileValue] public List<bool> Flags { get; set; } = new();
				}
			}
			"""),
		["CommentsAndNames"] = new("""
			using System;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public partial class Outer
				{
					[GenerateIniFile(StringComparison.Ordinal, SerializeComments = true)]
					internal sealed partial class CommentedIni
					{
						/// <summary>
						/// General application settings
						/// </summary>
						[GenerateIniFileSection("CustomSection")]
						public GeneralSection General { get; set; } = new();
					}
				}

				public class GeneralSection
				{
					/// <summary>
					/// The display name of the application
					/// </summary>
					[GenerateIniFileValue("app_name")]
					public string AppName { get; set; }

					/// <summary>
					/// The current version number
					/// </summary>
					[GenerateIniFileValue]
					public int Version { get; set; }

					[GenerateIniFileValue]
					public TimeSpan Timeout { get; set; }
				}
			}
			"""),
		["Diagnostics"] = new("""
			#nullable enable
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public class MySection
				{
					[GenerateIniFileValue]
					public string? Name { get; set; }
				}

				[GenerateIniFile]
				internal sealed partial class NullableIni
				{
					[GenerateIniFileSection]
					public MySection? Section { get; set; } = new();
				}

				[GenerateIniFile]
				internal sealed partial class UninitializedIni
				{
					[GenerateIniFileSection]
					public MySection Section { get; set; }
				}

				[GenerateIniFile]
				internal sealed partial class NoGetterIni
				{
					[GenerateIniFileSection]
					public MySection Section { private get; set; } = new();
				}
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> Notifications = new()
	{
		["Fields"] = new("""
			using System.Collections.Generic;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateNotifications]
				public partial class NotifyModel
				{
					private int _id;
					private string _name;
					private List<string> _items;
					private int? m_count;
					private static int _shared;
					private readonly int _readOnly;
				}

				[GenerateNotifications(propertyChanging: false)]
				internal partial class ChangedOnlyModel
				{
					private int _value;
				}

				[GenerateNotifications(propertyChanged: false)]
				internal partial class ChangingOnlyModel
				{
					private int _value;
				}
			}
			"""),
		["AlsoNotifyExcludeHasChanged"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateNotifications(hasChanged: true)]
				public partial class AlsoNotifyModel
				{
					[AlsoNotify(nameof(FullName))]
					private string _firstName;

					[AlsoNotify(nameof(FullName), nameof(Initials))]
					private string _lastName;

					[ExcludeFromNotification]
					private string _secret;

					[AlsoNotify(nameof(FullName))]
					public partial string Title { get; set; }

					[ExcludeFromNotification]
					public partial string Hidden { get; set; }

					public string FullName => FirstName + LastName;
					public string Initials => string.Empty;
				}
			}
			"""),
		["PartialProperties"] = new("""
			using System.Collections.Generic;
			using BB84.SourceGenerators.Attributes;
			using OtherNamespace;

			namespace OtherNamespace
			{
				public class Foo { }
			}

			namespace TestNamespace
			{
				[GenerateNotifications]
				public partial class PartialModel
				{
					public partial string Name { get; set; }
					internal partial int Count { get; set; }
					protected internal partial string? Notes { get; set; }
					public partial int Version { get; private set; }
					private protected partial List<Foo> Items { get; set; }
					public string NotPartial { get; set; }
				}
			}
			""", NullableContextOptions.Enable),
		["PartialHooks"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateNotifications(partialMethods: true)]
				public partial class HookModel
				{
					private string? _name;
					public partial int Count { get; set; }

					[ExcludeFromNotification]
					public partial int Secret { get; set; }
				}

				[GenerateNotifications(false, true, true, true)]
				public sealed partial class ChangingHookModel
				{
					private string? _name;
					partial void OnNameChanging(string? value) { }
				}
			}
			""", NullableContextOptions.Enable),
		["RecordAndNested"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateNotifications]
				public partial record NotifyRecordModel
				{
					private int _id;
				}

				public static partial class Outer
				{
					internal partial class Middle
					{
						[GenerateNotifications]
						public sealed partial class Inner
						{
							private int _value;
						}
					}
				}
			}
			"""),
		["GeneratedInheritance"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateNotifications(hasChanged: true)]
				public partial class BaseModel
				{
					private string? _name;
				}

				[GenerateNotifications]
				public partial class DerivedModel : BaseModel
				{
					[AlsoNotify(nameof(Name))]
					private int _age;
				}

				[GenerateNotifications(propertyChanging: false)]
				public partial class ChangedOnlyBaseModel
				{
					private int _id;
				}

				[GenerateNotifications]
				public partial class ChangingDerivedModel : ChangedOnlyBaseModel
				{
					private int _count;
				}

				[GenerateNotifications]
				public partial class RootModel
				{
					private int _a;
				}

				public partial class MiddleModel : RootModel { }

				[GenerateNotifications(hasChanged: true)]
				public partial class LeafModel : MiddleModel
				{
					private int _b;
				}

				[GenerateNotifications]
				public partial record BaseRecord
				{
					private int _id;
				}

				[GenerateNotifications]
				public partial record DerivedRecord : BaseRecord
				{
					private int _count;
				}
			}
			""", NullableContextOptions.Enable),
		["HandWrittenInheritance"] = new("""
			using System.ComponentModel;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public abstract class SingleRaiseBase : INotifyPropertyChanged
				{
					public event PropertyChangedEventHandler PropertyChanged;
					protected void RaisePropertyChanged(string propertyName)
						=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
				}

				[GenerateNotifications(propertyChanging: false)]
				public partial class SingleRaiseDerived : SingleRaiseBase
				{
					[AlsoNotify(nameof(Display))]
					private string _name;
					public string Display => _name;
				}

				public abstract class ParamsRaiseBase : INotifyPropertyChanged, INotifyPropertyChanging
				{
					public event PropertyChangedEventHandler PropertyChanged;
					public event PropertyChangingEventHandler PropertyChanging;
					protected void RaisePropertyChanged(params string[] propertyNames) { }
					protected void RaisePropertyChanging(params string[] propertyNames) { }
				}

				[GenerateNotifications]
				public partial class ParamsRaiseDerived : ParamsRaiseBase
				{
					[AlsoNotify(nameof(Display))]
					private string _name;
					public string Display => _name;
				}

				public abstract class HasChangedBase : INotifyPropertyChanged
				{
					public event PropertyChangedEventHandler PropertyChanged;
					public bool HasChanged { get; protected set; }
					protected void RaisePropertyChanged(string propertyName) { }
					protected void RaisePropertyChanged(params string[] propertyNames) { }
				}

				[GenerateNotifications(propertyChanging: false, hasChanged: true)]
				public partial class HasChangedDerived : HasChangedBase
				{
					private int _value;
				}

				public abstract class NoRaiseBase : INotifyPropertyChanged
				{
					public event PropertyChangedEventHandler PropertyChanged;
					private void RaisePropertyChanged(string propertyName) { }
				}

				[GenerateNotifications(propertyChanging: false)]
				public partial class NoRaiseDerived : NoRaiseBase
				{
					private string _name;
				}
			}
			"""),
		["Diagnostics"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateNotifications]
				public partial record PositionalNotifyModel(int Id, string Name);

				[GenerateNotifications]
				public partial class InvalidAccessorModel
				{
					public partial string GetOnly { get; }
					public partial string GetInit { get; init; }
					public partial string SetOnly { set; }
				}

				[GenerateNotifications]
				public partial class ConflictModel
				{
					[ExcludeFromNotification]
					private string _name;
					public partial string Name { get; set; }
				}

				[GenerateNotifications]
				public partial class StaticModel
				{
					public static partial string Name { get; set; }
				}
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> Singleton = new()
	{
		["Lazy"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateSingleton]
				public partial class MySingleton { }
			}
			"""),
		["NonLazy"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateSingleton(false)]
				internal sealed partial class MyNonLazySingleton { }
			}
			"""),
		["InterfaceAndNested"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public interface IMyService { }

				[GenerateSingleton]
				public partial class MyService : IMyService { }

				public partial class Outer
				{
					[GenerateSingleton(useLazy: false)]
					internal partial class Inner : IMyService { }
				}
			}
			"""),
		["Diagnostics"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateSingleton]
				public partial class RequiredPropertySingleton
				{
					public required string Name { get; set; }
				}

				[GenerateSingleton]
				public partial class RequiredFieldSingleton
				{
					public required string Value;
				}
			}
			"""),
	};

	internal static readonly Dictionary<string, SnapshotCase> ToStringOverride = new()
	{
		["Plain"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateToString]
				public partial class BasicModel
				{
					public int Id { get; set; }
					public string Name { get; set; }
					public static int Shared { get; set; }
				}

				[GenerateToString]
				internal sealed partial class EmptyModel { }
			}
			"""),
		["Options"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateToString(Formattable = true, Separator = " | ", NullPlaceholder = "N/A")]
				public partial class OptionsModel
				{
					public int Value { get; set; }
					public string Name { get; set; }
					[ToStringFormat("X")] public int Code { get; set; }
					[ToStringOrder(0)] public double Ratio { get; set; }
					[ToStringFormat("yyyy-MM-dd")] [ToStringOrder(1)] public System.DateTime Date { get; set; }
				}
			}
			"""),
		["Collections"] = new("""
			using System.Collections.Generic;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateToString]
				public partial class CountModel
				{
					public List<int> Items { get; set; }
					public int[] Array { get; set; }
				}

				[GenerateToString(CollectionFormat = CollectionFormat.Elements)]
				public partial class ElementsModel
				{
					public List<int> Items { get; set; }
					public Dictionary<string, int> Map { get; set; }
				}

				[GenerateToString(CollectionFormat = CollectionFormat.TypeAndCount)]
				public partial class TypeCountModel
				{
					public List<int> Items { get; set; }
					public IEnumerable<string> Sequence { get; set; }
				}
			}
			"""),
		["ExcludesAndInheritance"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public class BaseModel { public int BaseProp { get; set; } }

				[GenerateToString("Secret", nameof(ExcludeModel.Internal))]
				public partial class ExcludeModel : BaseModel
				{
					public int Id { get; set; }
					public string Secret { get; set; }
					public string Internal { get; set; }
				}

				[GenerateToString(IncludeInherited = true)]
				public partial class DerivedModel : BaseModel { public int Own { get; set; } }
			}
			"""),
		["Records"] = new("""
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateToString]
				public partial record RecordModel { public int Id { get; set; } public string Name { get; set; } }

				[GenerateToString]
				public partial record PositionalRecordModel(int Id, string Name);

				[GenerateToString(Formattable = true)]
				public partial record FormattableRecordModel { public int Value { get; set; } }

				[GenerateToString]
				public partial record BaseRecordModel { public int Id { get; set; } }

				[GenerateToString]
				public sealed partial record DerivedRecordModel : BaseRecordModel { public string Name { get; set; } }
			}
			"""),
		["NestedAndNullable"] = new("""
			using System.Collections.Generic;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public static partial class Outer
				{
					[GenerateToString(NullPlaceholder = "<null>")]
					internal partial class Inner
					{
						public string? Name { get; set; }
						public int? Count { get; set; }
						public List<string>? Tags { get; set; }
					}
				}
			}
			""", NullableContextOptions.Enable),
	};

	internal static readonly Dictionary<string, SnapshotCase> Validator = new()
	{
		["AllRules"] = new("""
			using System.ComponentModel.DataAnnotations;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateValidator]
				public partial class ValModel
				{
					[Required]
					public string? Name { get; set; }

					[RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
					public string? Email { get; set; }

					[RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
					public string? OtherEmail { get; set; }

					[EmailAddress]
					public string? CompanyEmail { get; set; }

					[Url]
					public string? Website { get; set; }

					[Phone]
					public string? PhoneNumber { get; set; }

					[CreditCard]
					public string? CC { get; set; }

					[Range(1, 150)]
					public int Age { get; set; }

					[Range(0.5, 9.5)]
					public double Ratio { get; set; }

					[StringLength(500, MinimumLength = 3)]
					public string? Bio { get; set; }

					[StringLength(10)]
					public string? Code { get; set; }

					[MinLength(5)]
					[MaxLength(50)]
					public string? Password { get; set; }

					[Compare(nameof(Password))]
					public string? ConfirmPassword { get; set; }

					[AllowedValues("a", "b")]
					public string? Allowed { get; set; }

					[DeniedValues(1, 2)]
					public int Denied { get; set; }
				}
			}
			""", NullableContextOptions.Enable),
		["ErrorMessages"] = new("""
			using System.ComponentModel.DataAnnotations;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				[GenerateValidator]
				internal sealed partial class MessageModel
				{
					[Required(ErrorMessage = "Name is \"required\"")]
					public string Name { get; set; }

					[Range(1, 10, ErrorMessage = "Out of range")]
					public int Level { get; set; }

					[StringLength(5, ErrorMessage = "Too long")]
					public string Short { get; set; }

					[MinLength(1, ErrorMessage = "Too short")]
					[MaxLength(2, ErrorMessage = "Too many")]
					public string Bounded { get; set; }

					[RegularExpression("^x$", ErrorMessage = "Not x")]
					public string X { get; set; }

					[EmailAddress(ErrorMessage = "Bad mail")]
					public string Mail { get; set; }

					[Compare(nameof(Mail), ErrorMessage = "Mismatch")]
					public string MailAgain { get; set; }

					[AllowedValues("on", ErrorMessage = "Only on")]
					public string Switch { get; set; }

					[DeniedValues("off", ErrorMessage = "Not off")]
					public string OtherSwitch { get; set; }
				}
			}
			"""),
		["CollectionsAndCustom"] = new("""
			using System;
			using System.Collections.Generic;
			using System.ComponentModel.DataAnnotations;
			using BB84.SourceGenerators.Attributes;

			namespace TestNamespace
			{
				public sealed class EvenAttribute : ValidationAttribute
				{
					public override bool IsValid(object? value) => value is int i && i % 2 == 0;
				}

				[GenerateValidator]
				public partial class CollectionModel : IValidatableObject
				{
					[MinLength(1)]
					[MaxLength(3)]
					public List<string> Items { get; set; } = new();

					[MinLength(2)]
					public int[] Numbers { get; set; } = [];

					[Required]
					public List<int>? Required { get; set; }

					[Even]
					public int Number { get; set; }

					[Even(ErrorMessage = "Must be even")]
					public int Other { get; set; }

					public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => [];
				}
			}
			""", NullableContextOptions.Enable),
		["InheritedAndNested"] = new("""
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

				public static partial class Outer
				{
					[GenerateValidator]
					internal partial class Inner
					{
						[Required]
						public string Name { get; set; }
					}

					[GenerateValidator]
					internal partial class EmptyValModel { }
				}
			}
			"""),
		["Diagnostics"] = new("""
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
			}
			"""),
	};
#endif
}
