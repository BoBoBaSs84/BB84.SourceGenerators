// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Windows.Input;

using BB84.SourceGenerators.Attributes;
using BB84.SourceGenerators.Commands;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class ActionCommandGeneratorTests
{
	[TestMethod]
	public void SyncCommandShouldExecute()
	{
		CommandViewModel viewModel = new();

		viewModel.IncrementCommand.Execute();

		Assert.AreEqual(1, viewModel.Count);
		Assert.IsInstanceOfType<IActionCommand>(viewModel.IncrementCommand);
	}

	[TestMethod]
	public void SyncCommandShouldReturnSameInstance()
	{
		CommandViewModel viewModel = new();

		IActionCommand first = viewModel.IncrementCommand;
		IActionCommand second = viewModel.IncrementCommand;

		Assert.AreSame(first, second);
	}

	[TestMethod]
	public void SyncCommandShouldExecuteThroughICommand()
	{
		CommandViewModel viewModel = new();
		ICommand command = viewModel.IncrementCommand;

		command.Execute(null);

		Assert.AreEqual(1, viewModel.Count);
	}

	[TestMethod]
	public void SyncCommandShouldNotExecuteWhenGuardIsFalse()
	{
		CommandViewModel viewModel = new();

		Assert.IsFalse(viewModel.SaveCommand.CanExecute());
		viewModel.SaveCommand.Execute();
		Assert.AreEqual(0, viewModel.SaveCount);

		viewModel.CanSave = true;

		Assert.IsTrue(viewModel.SaveCommand.CanExecute());
		viewModel.SaveCommand.Execute();
		Assert.AreEqual(1, viewModel.SaveCount);
	}

	[TestMethod]
	public void NotifyCanExecuteChangedShouldRaiseCanExecuteChanged()
	{
		CommandViewModel viewModel = new();
		int raised = 0;
		viewModel.SaveCommand.CanExecuteChanged += (s, e) => raised++;

		viewModel.CanSave = true;

		Assert.AreEqual(1, raised);
	}

	[TestMethod]
	public void GenericSyncCommandShouldExecuteWithParameter()
	{
		CommandViewModel viewModel = new();

		viewModel.AddCommand.Execute(5);

		Assert.AreEqual(5, viewModel.Count);
		Assert.IsInstanceOfType<IActionCommand<int>>(viewModel.AddCommand);
	}

	[TestMethod]
	public void GenericSyncCommandShouldUseParameterGuard()
	{
		CommandViewModel viewModel = new();

		Assert.IsFalse(viewModel.AddCommand.CanExecute(-1));
		Assert.IsTrue(viewModel.AddCommand.CanExecute(1));
	}

	[TestMethod]
	public void GenericSyncCommandShouldRejectNullForValueTypeParameter()
	{
		CommandViewModel viewModel = new();
		ICommand command = viewModel.AddCommand;

		Assert.IsFalse(command.CanExecute(null));
		command.Execute(null);

		Assert.AreEqual(0, viewModel.Count);
	}

	[TestMethod]
	public void GenericSyncCommandShouldAcceptNullForReferenceTypeParameter()
	{
		CommandViewModel viewModel = new();
		ICommand command = viewModel.RenameCommand;

		Assert.IsTrue(command.CanExecute(null));
		command.Execute(null);

		Assert.IsNull(viewModel.LastName);
	}

	[TestMethod]
	public async Task AsyncCommandShouldExecute()
	{
		CommandViewModel viewModel = new();

		await viewModel.LoadCommand.ExecuteAsync();

		Assert.AreEqual(1, viewModel.LoadCount);
		Assert.IsInstanceOfType<IAsyncActionCommand>(viewModel.LoadCommand);
	}

	[TestMethod]
	public async Task AsyncCommandShouldTrackExecutionAndBlockReentry()
	{
		CommandViewModel viewModel = new();
		TaskCompletionSource<bool> gate = new();
		viewModel.Gate = gate.Task;

		Task running = viewModel.WaitCommand.ExecuteAsync();

		Assert.IsTrue(viewModel.WaitCommand.IsExecuting);
		Assert.IsFalse(viewModel.WaitCommand.CanExecute());
		Assert.IsTrue(viewModel.WaitCommand.CancelCommand.CanExecute());

		gate.SetResult(true);
		await running;

		Assert.IsFalse(viewModel.WaitCommand.IsExecuting);
		Assert.IsTrue(viewModel.WaitCommand.CanExecute());
		Assert.IsFalse(viewModel.WaitCommand.CancelCommand.CanExecute());
	}

	[TestMethod]
	public async Task AsyncCommandShouldCancelThroughCancelCommand()
	{
		CommandViewModel viewModel = new();

		Task running = viewModel.DelayCommand.ExecuteAsync();
		Assert.IsTrue(viewModel.DelayCommand.IsExecuting);

		viewModel.DelayCancelCommand.Execute();
		await running;

		Assert.IsTrue(viewModel.DelayWasCancelled);
		Assert.IsFalse(viewModel.DelayCommand.IsExecuting);
		Assert.IsFalse(viewModel.DelayCommand.IsCancellationRequested);
	}

	[TestMethod]
	public async Task AsyncCommandShouldObserveExternalToken()
	{
		CommandViewModel viewModel = new();
		using CancellationTokenSource cts = new();

		Task running = viewModel.DelayCommand.ExecuteAsync(cts.Token);
		cts.Cancel();
		await running;

		Assert.IsTrue(viewModel.DelayWasCancelled);
	}

	[TestMethod]
	public async Task AsyncCommandShouldAllowConcurrentExecutions()
	{
		CommandViewModel viewModel = new();
		TaskCompletionSource<bool> gate = new();
		viewModel.Gate = gate.Task;

		Task first = viewModel.ParallelCommand.ExecuteAsync();

		Assert.IsTrue(viewModel.ParallelCommand.CanExecute());
		Task second = viewModel.ParallelCommand.ExecuteAsync();

		gate.SetResult(true);
		await Task.WhenAll(first, second);

		Assert.AreEqual(2, viewModel.ParallelCount);
	}

	[TestMethod]
	public async Task AsyncCommandShouldRouteExceptionsToHandler()
	{
		CommandViewModel viewModel = new();

		await viewModel.FailCommand.ExecuteAsync();

		Assert.IsInstanceOfType<InvalidOperationException>(viewModel.LastException);
	}

	[TestMethod]
	public async Task AsyncCommandShouldRethrowWithoutHandler()
	{
		CommandViewModel viewModel = new();

		await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => CommandViewModel.ThrowCommand.ExecuteAsync());
		Assert.IsFalse(CommandViewModel.ThrowCommand.IsExecuting);
	}

	[TestMethod]
	public async Task GenericAsyncCommandShouldExecuteWithParameter()
	{
		CommandViewModel viewModel = new();

		await viewModel.SearchCommand.ExecuteAsync("query");

		Assert.AreEqual("query", viewModel.LastName);
		Assert.IsInstanceOfType<IAsyncActionCommand<string>>(viewModel.SearchCommand);
	}

	[TestMethod]
	public async Task GenericAsyncCommandWithoutTokenShouldExecute()
	{
		CommandViewModel viewModel = new();

		await viewModel.FindCommand.ExecuteAsync("value");

		Assert.AreEqual("value", viewModel.LastName);
	}

	[TestMethod]
	public void CustomNameShouldBeUsed()
	{
		CommandViewModel viewModel = new();

		viewModel.ResetCountCommand.Execute();

		Assert.AreEqual(-1, viewModel.Count);
	}

	[TestMethod]
	public void StaticCommandShouldExecute()
	{
		CommandViewModel.StaticCount = 0;

		CommandViewModel.BumpCommand.Execute();

		Assert.AreEqual(1, CommandViewModel.StaticCount);
	}
}

[GenerateNotifications]
public sealed partial class CommandViewModel
{
	[NotifyCanExecuteChanged(nameof(SaveCommand))]
	private bool _canSave;

	public int Count { get; private set; }
	public int SaveCount { get; private set; }
	public int LoadCount { get; private set; }
	public int ParallelCount { get; private set; }
	public string? LastName { get; private set; }
	public bool DelayWasCancelled { get; private set; }
	public Exception? LastException { get; private set; }
	public Task Gate { get; set; } = Task.CompletedTask;
	public static int StaticCount { get; set; }

	[ActionCommand]
	private void Increment() => Count++;

	[ActionCommand(Name = "ResetCountCommand")]
	private void Reset() => Count = -1;

	[ActionCommand(CanExecute = nameof(CanSave))]
	private void Save() => SaveCount++;

	[ActionCommand(CanExecute = nameof(CanAdd))]
	private void Add(int value) => Count += value;

	private static bool CanAdd(int value) => value >= 0;

	[ActionCommand]
	private void Rename(string? name) => LastName = name;

	[ActionCommand]
	private static void Bump() => StaticCount++;

	[ActionCommand]
	private Task LoadAsync()
	{
		LoadCount++;
		return Task.CompletedTask;
	}

	[ActionCommand]
	private async Task WaitAsync() => await Gate;

	[ActionCommand(IncludeCancelCommand = true)]
	private async Task DelayAsync(CancellationToken cancellationToken)
	{
		try
		{
			await Task.Delay(Timeout.Infinite, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			DelayWasCancelled = true;
			throw;
		}
	}

	[ActionCommand(AllowConcurrentExecutions = true)]
	private async Task ParallelAsync()
	{
		await Gate;
		ParallelCount++;
	}

	[ActionCommand(ExceptionHandler = nameof(OnError))]
	private Task FailAsync() => throw new InvalidOperationException();

	[ActionCommand]
	private static async Task ThrowAsync()
	{
		await Task.Yield();
		throw new InvalidOperationException();
	}

	private void OnError(Exception exception) => LastException = exception;

	[ActionCommand]
	private Task SearchAsync(string query, CancellationToken cancellationToken)
	{
		LastName = query;
		return Task.CompletedTask;
	}

	[ActionCommand]
	private Task FindAsync(string query)
	{
		LastName = query;
		return Task.CompletedTask;
	}
}