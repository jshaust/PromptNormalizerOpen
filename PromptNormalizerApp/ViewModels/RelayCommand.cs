// FILE: C:\Users\JoshAust\source\repos\PromptNormalizerApp\PromptNormalizerApp\ViewModels\RelayCommand.cs
/**
 * @description
 * A simple ICommand implementation allowing delegates for Execute and CanExecute.
 *
 * @notes
 * - Commonly used in WPF MVVM as a convenient way to create commands from lambdas.
 */

using System;
using System.Windows.Input;

namespace PromptBuilderApp.ViewModels
{
	public class RelayCommand : ICommand
	{
		private readonly Action<object> _execute;
		private readonly Func<object, bool> _canExecute;

		public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute == null || _canExecute(parameter);
		}

		public void Execute(object parameter)
		{
			_execute?.Invoke(parameter);
		}

#pragma warning disable 67 // Event never used
		public event EventHandler CanExecuteChanged;
#pragma warning restore 67

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
