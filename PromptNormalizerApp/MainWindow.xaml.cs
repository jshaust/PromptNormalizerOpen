/**
 * @description
 * Code-behind for the Prompt Normalizer WPF app. With MVVM, we minimize logic here.
 * 
 * Key changes:
 * 1. DataContext is set to MainViewModel in XAML (or we could do so here).
 * 2. We removed most event handlers in favor of commands/properties in MainViewModel.
 * 3. This file now focuses on Window initialization only.
 *
 * @notes
 * - Folder scanning logic is partially moved to the ViewModel or left for future steps.
 */

using System.Windows;

namespace PromptBuilderApp
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			// If you wanted to set DataContext in code-behind instead of XAML:
			// DataContext = new MainViewModel();
		}
	}
}
