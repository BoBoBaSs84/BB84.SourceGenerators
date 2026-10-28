// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
namespace BB84.SourceGenerators.Commands
{
	/// <summary>
	/// A synchronous command that delegates its execution and execution guard to the provided delegates.
	/// </summary>
	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public sealed class ActionCommand : IActionCommand
	{
		private readonly global::System.Action _execute;
		private readonly global::System.Func<bool>? _canExecute;

		/// <summary>
		/// Initializes a new instance of the <see cref="ActionCommand"/> class.
		/// </summary>
		/// <param name="execute">The action to invoke when the command is executed.</param>
		/// <param name="canExecute">The optional guard that determines whether the command can execute.</param>
		public ActionCommand(global::System.Action execute, global::System.Func<bool>? canExecute = null)
		{
			_execute = execute ?? throw new global::System.ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		/// <inheritdoc/>
		public event global::System.EventHandler? CanExecuteChanged;

		/// <inheritdoc/>
		public bool CanExecute()
			=> _canExecute?.Invoke() ?? true;

		/// <inheritdoc/>
		public bool CanExecute(object? parameter)
			=> CanExecute();

		/// <inheritdoc/>
		public void Execute()
		{
			if (!CanExecute())
				return;

			_execute();
			RaiseCanExecuteChanged();
		}

		/// <inheritdoc/>
		public void Execute(object? parameter)
			=> Execute();

		/// <inheritdoc/>
		public void RaiseCanExecuteChanged()
			=> CanExecuteChanged?.Invoke(this, global::System.EventArgs.Empty);
	}

	/// <summary>
	/// A synchronous command with a parameter of type <typeparamref name="T"/> that delegates its execution
	/// and execution guard to the provided delegates.
	/// </summary>
	/// <typeparam name="T">The type of the command parameter.</typeparam>
	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public sealed class ActionCommand<T> : IActionCommand<T>
	{
		private readonly global::System.Action<T> _execute;
		private readonly global::System.Func<T, bool>? _canExecute;

		/// <summary>
		/// Initializes a new instance of the <see cref="ActionCommand{T}"/> class.
		/// </summary>
		/// <param name="execute">The action to invoke when the command is executed.</param>
		/// <param name="canExecute">The optional guard that determines whether the command can execute.</param>
		public ActionCommand(global::System.Action<T> execute, global::System.Func<T, bool>? canExecute = null)
		{
			_execute = execute ?? throw new global::System.ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		/// <inheritdoc/>
		public event global::System.EventHandler? CanExecuteChanged;

		/// <inheritdoc/>
		public bool CanExecute(T parameter)
			=> _canExecute?.Invoke(parameter) ?? true;

		/// <inheritdoc/>
		public bool CanExecute(object? parameter)
			=> CommandParameter.TryConvert(parameter, out T value) && CanExecute(value);

		/// <inheritdoc/>
		public void Execute(T parameter)
		{
			if (!CanExecute(parameter))
				return;

			_execute(parameter);
			RaiseCanExecuteChanged();
		}

		/// <inheritdoc/>
		public void Execute(object? parameter)
		{
			if (CommandParameter.TryConvert(parameter, out T value))
				Execute(value);
		}

		/// <inheritdoc/>
		public void RaiseCanExecuteChanged()
			=> CanExecuteChanged?.Invoke(this, global::System.EventArgs.Empty);
	}

	/// <summary>
	/// Provides safe conversion of untyped <see cref="global::System.Windows.Input.ICommand"/> parameters.
	/// </summary>
	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal static class CommandParameter
	{
		/// <summary>
		/// Tries to convert the untyped command parameter to <typeparamref name="T"/>. A <see langword="null"/>
		/// parameter converts to <see langword="default"/> only when <typeparamref name="T"/> accepts <see langword="null"/>.
		/// </summary>
		/// <typeparam name="T">The target parameter type.</typeparam>
		/// <param name="parameter">The untyped command parameter.</param>
		/// <param name="value">The converted parameter, if successful.</param>
		/// <returns><see langword="true"/> if the parameter could be converted; otherwise, <see langword="false"/>.</returns>
		public static bool TryConvert<T>(object? parameter, out T value)
		{
			if (parameter is T typed)
			{
				value = typed;
				return true;
			}

			value = default!;
			return parameter is null && default(T) is null;
		}
	}
}