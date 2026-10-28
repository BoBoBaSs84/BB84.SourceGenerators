// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Commands
{
	/// <summary>
	/// Provides the shared execution pipeline for asynchronous commands: execution tracking,
	/// cancellation, exception routing and <see cref="global::System.Windows.Input.ICommand.CanExecuteChanged"/> notification.
	/// </summary>
	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public abstract class AsyncActionCommandBase
	{
		private readonly ActionCommand _cancelCommand;
		private readonly global::System.Action<global::System.Exception>? _onException;
		private readonly bool _allowConcurrentExecutions;
		private global::System.Threading.CancellationTokenSource _cancellation = new global::System.Threading.CancellationTokenSource();
		private int _executionCount;
		private bool _cancellationRequested;

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncActionCommandBase"/> class.
		/// </summary>
		/// <param name="onException">
		/// The optional handler that receives exceptions thrown by the command. When <see langword="null"/>, exceptions are rethrown.
		/// </param>
		/// <param name="allowConcurrentExecutions">
		/// Indicates whether the command can be executed again while a previous execution is still running.
		/// </param>
		protected AsyncActionCommandBase(global::System.Action<global::System.Exception>? onException, bool allowConcurrentExecutions)
		{
			_cancelCommand = new ActionCommand(Cancel, () => IsExecuting);
			_onException = onException;
			_allowConcurrentExecutions = allowConcurrentExecutions;
		}

		/// <summary>
		/// Occurs when changes occur that affect whether or not the command should execute.
		/// </summary>
		public event global::System.EventHandler? CanExecuteChanged;

		/// <summary>
		/// Gets a value indicating whether the command is currently executing.
		/// </summary>
		public bool IsExecuting => global::System.Threading.Volatile.Read(ref _executionCount) > 0;

		/// <summary>
		/// Gets a value indicating whether cancellation has been requested for the currently executing operation.
		/// </summary>
		public bool IsCancellationRequested => IsExecuting && _cancellationRequested;

		/// <summary>
		/// Gets a command that, when executed, cancels the currently running operation.
		/// </summary>
		public IActionCommand CancelCommand => _cancelCommand;

		/// <summary>
		/// Gets a value indicating whether a new execution may be started with respect to concurrency.
		/// </summary>
		protected bool CanStartExecution => _allowConcurrentExecutions || !IsExecuting;

		/// <summary>
		/// Requests cancellation of the currently executing operation.
		/// </summary>
		public void Cancel()
		{
			if (!IsExecuting)
				return;

			_cancellationRequested = true;
			global::System.Threading.CancellationTokenSource previous =
				global::System.Threading.Interlocked.Exchange(ref _cancellation, new global::System.Threading.CancellationTokenSource());
			previous.Cancel();
			RaiseCanExecuteChanged();
		}

		/// <summary>
		/// Raises the <see cref="CanExecuteChanged"/> event for this command and its <see cref="CancelCommand"/>.
		/// </summary>
		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, global::System.EventArgs.Empty);
			_cancelCommand.RaiseCanExecuteChanged();
		}

		/// <summary>
		/// Runs the specified body with a token linked to <see cref="Cancel"/> and the given external token.
		/// </summary>
		/// <param name="body">The asynchronous operation to run.</param>
		/// <param name="cancellationToken">The external token to link with.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		protected async global::System.Threading.Tasks.Task ExecuteCoreAsync(
			global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task> body,
			global::System.Threading.CancellationToken cancellationToken)
		{
			using (global::System.Threading.CancellationTokenSource linked =
				global::System.Threading.CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token, cancellationToken))
			{
				global::System.Threading.Interlocked.Increment(ref _executionCount);
				RaiseCanExecuteChanged();

				try
				{
					await body(linked.Token);
				}
				catch (global::System.OperationCanceledException) when (linked.IsCancellationRequested)
				{
				}
				catch (global::System.Exception exception) when (_onException != null)
				{
					_onException?.Invoke(exception);
				}
				finally
				{
					if (global::System.Threading.Interlocked.Decrement(ref _executionCount) == 0)
						_cancellationRequested = false;

					RaiseCanExecuteChanged();
				}
			}
		}
	}

	/// <summary>
	/// An asynchronous, cancellable command that delegates its execution and execution guard to the provided delegates.
	/// </summary>
	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public sealed class AsyncActionCommand : AsyncActionCommandBase, IAsyncActionCommand
	{
		private readonly global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task> _execute;
		private readonly global::System.Func<bool>? _canExecute;

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncActionCommand"/> class.
		/// </summary>
		/// <param name="execute">The asynchronous operation to invoke when the command is executed.</param>
		/// <param name="canExecute">The optional guard that determines whether the command can execute.</param>
		/// <param name="onException">The optional handler that receives exceptions thrown by <paramref name="execute"/>.</param>
		/// <param name="allowConcurrentExecutions">Indicates whether concurrent executions are allowed.</param>
		public AsyncActionCommand(
			global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task> execute,
			global::System.Func<bool>? canExecute = null,
			global::System.Action<global::System.Exception>? onException = null,
			bool allowConcurrentExecutions = false)
			: base(onException, allowConcurrentExecutions)
		{
			_execute = execute ?? throw new global::System.ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		/// <inheritdoc/>
		public bool CanExecute()
			=> CanStartExecution && (_canExecute?.Invoke() ?? true);

		/// <inheritdoc/>
		public bool CanExecute(object? parameter)
			=> CanExecute();

		/// <inheritdoc/>
		public global::System.Threading.Tasks.Task ExecuteAsync()
			=> ExecuteAsync(global::System.Threading.CancellationToken.None);

		/// <inheritdoc/>
		public global::System.Threading.Tasks.Task ExecuteAsync(global::System.Threading.CancellationToken cancellationToken)
			=> CanExecute()
				? ExecuteCoreAsync(_execute, cancellationToken)
				: global::System.Threading.Tasks.Task.CompletedTask;

		/// <inheritdoc/>
		public async void Execute(object? parameter)
			=> await ExecuteAsync();
	}

	/// <summary>
	/// An asynchronous, cancellable command with a parameter of type <typeparamref name="T"/> that delegates
	/// its execution and execution guard to the provided delegates.
	/// </summary>
	/// <typeparam name="T">The type of the command parameter.</typeparam>
	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public sealed class AsyncActionCommand<T> : AsyncActionCommandBase, IAsyncActionCommand<T>
	{
		private readonly global::System.Func<T, global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task> _execute;
		private readonly global::System.Func<T, bool>? _canExecute;

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncActionCommand{T}"/> class.
		/// </summary>
		/// <param name="execute">The asynchronous operation to invoke when the command is executed.</param>
		/// <param name="canExecute">The optional guard that determines whether the command can execute.</param>
		/// <param name="onException">The optional handler that receives exceptions thrown by <paramref name="execute"/>.</param>
		/// <param name="allowConcurrentExecutions">Indicates whether concurrent executions are allowed.</param>
		public AsyncActionCommand(
			global::System.Func<T, global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task> execute,
			global::System.Func<T, bool>? canExecute = null,
			global::System.Action<global::System.Exception>? onException = null,
			bool allowConcurrentExecutions = false)
			: base(onException, allowConcurrentExecutions)
		{
			_execute = execute ?? throw new global::System.ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		/// <inheritdoc/>
		public bool CanExecute(T parameter)
			=> CanStartExecution && (_canExecute?.Invoke(parameter) ?? true);

		/// <inheritdoc/>
		public bool CanExecute(object? parameter)
			=> CommandParameter.TryConvert(parameter, out T value) && CanExecute(value);

		/// <inheritdoc/>
		public global::System.Threading.Tasks.Task ExecuteAsync(T parameter)
			=> ExecuteAsync(parameter, global::System.Threading.CancellationToken.None);

		/// <inheritdoc/>
		public global::System.Threading.Tasks.Task ExecuteAsync(T parameter, global::System.Threading.CancellationToken cancellationToken)
			=> CanExecute(parameter)
				? ExecuteCoreAsync(token => _execute(parameter, token), cancellationToken)
				: global::System.Threading.Tasks.Task.CompletedTask;

		/// <inheritdoc/>
		public async void Execute(object? parameter)
		{
			if (CommandParameter.TryConvert(parameter, out T value))
				await ExecuteAsync(value);
		}
	}
}