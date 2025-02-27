// FILE: C:\Users\JoshAust\source\repos\PromptNormalizerApp\PromptNormalizerApp\ViewModels\ViewModelBase.cs
/**
 * @description
 * A simple base class for ViewModels that implements INotifyPropertyChanged.
 * 
 * @notes
 * - Provides SetProperty<T> utility for updating properties and raising change notifications.
 */

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PromptBuilderApp.ViewModels
{
	public class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value)) return false;

			storage = value;
			OnPropertyChanged(propertyName);
			return true;
		}
	}
}
