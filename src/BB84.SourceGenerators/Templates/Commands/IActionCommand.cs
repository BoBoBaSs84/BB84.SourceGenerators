// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Commands
{
	/// <summary>
	/// Represents a synchronous command that can be executed and queried for its ability to execute.
	/// </summary>
	public interface IActionCommand : global::System.Windows.Input.ICommand
	{
		/// <summary>
		/// Determines whether the command can execute in its current state.
		/// </summary>
		/// <returns><see langword="true"/> if the command can be executed; otherwise, <see langword="false"/>.</returns>
		bool CanExecute();

		/// <summary>
		/// Executes the command if it can be executed.
		/// </summary>
		void Execute();

		/// <summary>
		/// Raises the <see cref="global::System.Windows.Input.ICommand.CanExecuteChanged"/> event.
		/// </summary>
		void RaiseCanExecuteChanged();
	}

	/// <summary>
	/// Represents a synchronous command that can be executed with a parameter of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the command parameter.</typeparam>
	public interface IActionCommand<T> : global::System.Windows.Input.ICommand
	{
		/// <summary>
		/// Determines whether the command can execute with the specified parameter.
		/// </summary>
		/// <param name="parameter">The command parameter.</param>
		/// <returns><see langword="true"/> if the command can be executed; otherwise, <see langword="false"/>.</returns>
		bool CanExecute(T parameter);

		/// <summary>
		/// Executes the command with the specified parameter if it can be executed.
		/// </summary>
		/// <param name="parameter">The command parameter.</param>
		void Execute(T parameter);

		/// <summary>
		/// Raises the <see cref="global::System.Windows.Input.ICommand.CanExecuteChanged"/> event.
		/// </summary>
		void RaiseCanExecuteChanged();
	}
}