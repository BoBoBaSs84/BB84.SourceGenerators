// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Commands
{
	/// <summary>
	/// Represents an asynchronous, cancellable command that can be executed and queried for its ability to execute.
	/// </summary>
	public interface IAsyncActionCommand : global::System.Windows.Input.ICommand
	{
		/// <summary>
		/// Gets a value indicating whether the command is currently executing.
		/// </summary>
		bool IsExecuting { get; }

		/// <summary>
		/// Gets a value indicating whether cancellation has been requested for the currently executing operation.
		/// </summary>
		bool IsCancellationRequested { get; }

		/// <summary>
		/// Gets a command that, when executed, cancels the currently running operation.
		/// </summary>
		IActionCommand CancelCommand { get; }

		/// <summary>
		/// Executes the command asynchronously if it can be executed.
		/// </summary>
		/// <returns>A task that represents the asynchronous operation.</returns>
		global::System.Threading.Tasks.Task ExecuteAsync();

		/// <summary>
		/// Executes the command asynchronously if it can be executed, observing the specified cancellation token.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		global::System.Threading.Tasks.Task ExecuteAsync(global::System.Threading.CancellationToken cancellationToken);

		/// <summary>
		/// Determines whether the command can execute in its current state.
		/// </summary>
		/// <returns><see langword="true"/> if the command can be executed; otherwise, <see langword="false"/>.</returns>
		bool CanExecute();

		/// <summary>
		/// Requests cancellation of the currently executing operation.
		/// </summary>
		void Cancel();

		/// <summary>
		/// Raises the <see cref="global::System.Windows.Input.ICommand.CanExecuteChanged"/> event.
		/// </summary>
		void RaiseCanExecuteChanged();
	}

	/// <summary>
	/// Represents an asynchronous, cancellable command that can be executed with a parameter of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the command parameter.</typeparam>
	public interface IAsyncActionCommand<T> : global::System.Windows.Input.ICommand
	{
		/// <summary>
		/// Gets a value indicating whether the command is currently executing.
		/// </summary>
		bool IsExecuting { get; }

		/// <summary>
		/// Gets a value indicating whether cancellation has been requested for the currently executing operation.
		/// </summary>
		bool IsCancellationRequested { get; }

		/// <summary>
		/// Gets a command that, when executed, cancels the currently running operation.
		/// </summary>
		IActionCommand CancelCommand { get; }

		/// <summary>
		/// Executes the command asynchronously with the specified parameter if it can be executed.
		/// </summary>
		/// <param name="parameter">The command parameter.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		global::System.Threading.Tasks.Task ExecuteAsync(T parameter);

		/// <summary>
		/// Executes the command asynchronously with the specified parameter if it can be executed,
		/// observing the specified cancellation token.
		/// </summary>
		/// <param name="parameter">The command parameter.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		global::System.Threading.Tasks.Task ExecuteAsync(T parameter, global::System.Threading.CancellationToken cancellationToken);

		/// <summary>
		/// Determines whether the command can execute with the specified parameter.
		/// </summary>
		/// <param name="parameter">The command parameter.</param>
		/// <returns><see langword="true"/> if the command can be executed; otherwise, <see langword="false"/>.</returns>
		bool CanExecute(T parameter);

		/// <summary>
		/// Requests cancellation of the currently executing operation.
		/// </summary>
		void Cancel();

		/// <summary>
		/// Raises the <see cref="global::System.Windows.Input.ICommand.CanExecuteChanged"/> event.
		/// </summary>
		void RaiseCanExecuteChanged();
	}
}