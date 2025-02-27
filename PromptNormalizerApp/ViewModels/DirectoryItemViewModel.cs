/**
 * @description
 * Represents a single directory or file node in the tree, with
 * potential subchildren, a display name, a full path, and a checkbox status.
 *
 * Key changes in Step 5:
 *  - Improved XML documentation for clarity and style consistency.
 */

using System.Collections.ObjectModel;

namespace PromptBuilderApp.ViewModels
{
	/// <summary>
	/// Represents a single directory or file in the user's folder hierarchy.
	/// Allows for child items if this node is a directory, and includes an IsChecked
	/// property to track user selection (checkbox).
	/// </summary>
	public class DirectoryItemViewModel : ViewModelBase
	{
		private bool _isChecked;
		private string _name;
		private string _fullPath;

		/// <summary>
		/// Initializes a new instance of the <see cref="DirectoryItemViewModel"/> class.
		/// Sets up an empty collection for <see cref="Children"/>.
		/// </summary>
		public DirectoryItemViewModel()
		{
			Children = new ObservableCollection<DirectoryItemViewModel>();
		}

		/// <summary>
		/// Gets or sets the display name (folder or file name).
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		/// <summary>
		/// Gets or sets the full path on disk (useful for reading file contents or deeper subfolders).
		/// </summary>
		public string FullPath
		{
			get => _fullPath;
			set => SetProperty(ref _fullPath, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether this node is checked (for potential selection).
		/// Bound to a CheckBox in the TreeView template.
		/// If this node is a folder, checking it will also check all children; unchecking it will uncheck them.
		/// </summary>
		public bool IsChecked
		{
			get => _isChecked;
			set
			{
				if (SetProperty(ref _isChecked, value))
				{
					SetChildrenCheckState(value);
				}
			}
		}

		/// <summary>
		/// Gets a collection of child nodes representing subdirectories or files.
		/// </summary>
		public ObservableCollection<DirectoryItemViewModel> Children { get; }

		/// <summary>
		/// Recursively sets <see cref="IsChecked"/> on all children to match this node's check state.
		/// Used so that checking a folder automatically checks all contained items.
		/// </summary>
		/// <param name="checkState">Boolean check state to apply to children.</param>
		private void SetChildrenCheckState(bool checkState)
		{
			foreach (var child in Children)
			{
				child.IsChecked = checkState;
			}
		}
	}
}
