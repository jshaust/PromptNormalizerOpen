/**
 * @description
 * ViewModel for the Prompt Normalizer application. Handles data binding
 * and commands, decoupling the UI from logic (basic MVVM).
 *
 * Key changes for Reload Folder feature:
 *  - Renamed LoadFolderCommand to ReloadFolderCommand and the method to ReloadFolderAsync.
 *  - Preserves the user's checked state when reloading.
 *  - **Step 3 Change**: Added WindowTitle property and logic to update it whenever RootFolder is set.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Forms = System.Windows.Forms;
using PromptBuilderApp.Helpers;
using SharpToken;

namespace PromptBuilderApp.ViewModels
{
	/// <summary>
	/// Main ViewModel for the Prompt Normalizer WPF application. 
	/// Manages user inputs for project requests, rules, specs, etc.,
	/// plus directory scanning and file selection logic.
	/// </summary>
	public class MainViewModel : ViewModelBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MainViewModel"/> class.
		/// Sets default property values and instantiates all ICommand fields.
		/// </summary>
		public MainViewModel()
		{
			BrowseCommand = new RelayCommand(BrowseForFolder, _ => !IsScanning);
			ReloadFolderCommand = new RelayCommand(async _ => await ReloadFolderAsync(), _ => !IsScanning);
			GeneratePromptCommand = new RelayCommand(GeneratePrompt, _ => !IsScanning);
			ClearAllFieldsCommand = new RelayCommand(ClearAllFields, _ => !IsScanning);
			CopyToClipboardCommand = new RelayCommand(CopyToClipboard, _ => !IsScanning);

			// Default property values
			MaxLines = "300";
			LineRangeStart = "0";
			LineRangeEnd = "0";
			SkipBin = true;
			SkipObj = true;
			SkipVs = true;
			SkipGit = true;
			SkipNodeModules = true;
			IncludeDirectoryStructure = true;
			SelectedTemplate = "Codegen Prompt";

			DirectoryItems = new ObservableCollection<DirectoryItemViewModel>();

			_windowTitle = "Prompt Normalizer";
		}

		#region Properties

		private bool _isScanning;

		/// <summary>
		/// Gets or sets a value indicating whether the ViewModel is actively scanning folders.
		/// Used to show/hide progress UI and disable certain commands while scanning.
		/// </summary>
		public bool IsScanning
		{
			get => _isScanning;
			set
			{
				if (SetProperty(ref _isScanning, value))
				{
					(BrowseCommand as RelayCommand)?.RaiseCanExecuteChanged();
					(ReloadFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
					(GeneratePromptCommand as RelayCommand)?.RaiseCanExecuteChanged();
					(ClearAllFieldsCommand as RelayCommand)?.RaiseCanExecuteChanged();
					(CopyToClipboardCommand as RelayCommand)?.RaiseCanExecuteChanged();
				}
			}
		}

		/// <summary>
		/// Collection of directory items (folders/files) for data binding.
		/// Populated when the user loads a folder, and each node can be individually checked.
		/// </summary>
		public ObservableCollection<DirectoryItemViewModel> DirectoryItems { get; }

		private string _windowTitle;
		/// <summary>
		/// Gets or sets the window title, displayed in MainWindow.xaml
		/// and updated whenever the RootFolder changes.
		/// </summary>
		public string WindowTitle
		{
			get => _windowTitle;
			set => SetProperty(ref _windowTitle, value);
		}

		private string _projectRequest;
		/// <summary>
		/// Gets or sets the user's main "Project Request" text, describing the project overview or scope.
		/// </summary>
		public string ProjectRequest
		{
			get => _projectRequest;
			set => SetProperty(ref _projectRequest, value);
		}

		private string _projectRules;
		/// <summary>
		/// Gets or sets the text for "Project Rules," enumerating constraints or guidelines.
		/// </summary>
		public string ProjectRules
		{
			get => _projectRules;
			set => SetProperty(ref _projectRules, value);
		}

		private string _technicalSpec;
		/// <summary>
		/// Gets or sets the "Technical Specification" text, containing any detailed requirements or specs.
		/// </summary>
		public string TechnicalSpec
		{
			get => _technicalSpec;
			set => SetProperty(ref _technicalSpec, value);
		}

		private string _implementationPlan;
		/// <summary>
		/// Gets or sets the "Implementation Plan" text, which may also appear in the final prompt.
		/// </summary>
		public string ImplementationPlan
		{
			get => _implementationPlan;
			set => SetProperty(ref _implementationPlan, value);
		}

		private string _regexPatterns;
		/// <summary>
		/// Gets or sets the text containing one or more "(pattern) => replacement" lines
		/// for applying redactions to the selected file contents.
		/// </summary>
		public string RegexPatterns
		{
			get => _regexPatterns;
			set => SetProperty(ref _regexPatterns, value);
		}

		private string _maxLines;
		/// <summary>
		/// Gets or sets the max lines per file chunk if chunking is enabled.
		/// Typically stored as a string for direct binding in the UI.
		/// </summary>
		public string MaxLines
		{
			get => _maxLines;
			set => SetProperty(ref _maxLines, value);
		}

		private string _lineRangeStart;
		/// <summary>
		/// Gets or sets the user-defined line range start, typically a string for text binding.
		/// </summary>
		public string LineRangeStart
		{
			get => _lineRangeStart;
			set => SetProperty(ref _lineRangeStart, value);
		}

		private string _lineRangeEnd;
		/// <summary>
		/// Gets or sets the user-defined line range end, typically a string for text binding.
		/// </summary>
		public string LineRangeEnd
		{
			get => _lineRangeEnd;
			set => SetProperty(ref _lineRangeEnd, value);
		}

		private bool _skipBin;
		/// <summary>
		/// Gets or sets a value indicating whether "bin" folders should be skipped during folder load.
		/// </summary>
		public bool SkipBin
		{
			get => _skipBin;
			set => SetProperty(ref _skipBin, value);
		}

		private bool _skipObj;
		/// <summary>
		/// Gets or sets a value indicating whether "obj" folders should be skipped during folder load.
		/// </summary>
		public bool SkipObj
		{
			get => _skipObj;
			set => SetProperty(ref _skipObj, value);
		}

		private bool _skipVs;
		/// <summary>
		/// Gets or sets a value indicating whether ".vs" folders should be skipped during folder load.
		/// </summary>
		public bool SkipVs
		{
			get => _skipVs;
			set => SetProperty(ref _skipVs, value);
		}

		private bool _skipGit;
		/// <summary>
		/// Gets or sets a value indicating whether ".git" folders should be skipped during folder load.
		/// </summary>
		public bool SkipGit
		{
			get => _skipGit;
			set => SetProperty(ref _skipGit, value);
		}

		private bool _skipNodeModules;
		/// <summary>
		/// Gets or sets a value indicating whether "node_modules" folders should be skipped during folder load.
		/// </summary>
		public bool SkipNodeModules
		{
			get => _skipNodeModules;
			set => SetProperty(ref _skipNodeModules, value);
		}

		private bool _includeDirectoryStructure;
		/// <summary>
		/// Gets or sets a value indicating whether to include a human-readable directory structure
		/// in the final prompt.
		/// </summary>
		public bool IncludeDirectoryStructure
		{
			get => _includeDirectoryStructure;
			set => SetProperty(ref _includeDirectoryStructure, value);
		}

		private string _selectedTemplate;
		/// <summary>
		/// Gets or sets the user's selected prompt template, e.g. "None", "Codegen Prompt", or "Review Prompt".
		/// Used to decide which prompt text to generate in <see cref="GeneratePrompt(object)"/>.
		/// </summary>
		public string SelectedTemplate
		{
			get => _selectedTemplate;
			set => SetProperty(ref _selectedTemplate, value);
		}

		private string _rootFolder;
		/// <summary>
		/// Gets or sets the root folder path chosen by the user. Used as the basis for folder scanning.
		/// Whenever this changes, we update <see cref="WindowTitle"/> to show the current folder name.
		/// </summary>
		public string RootFolder
		{
			get => _rootFolder;
			set
			{
				if (SetProperty(ref _rootFolder, value))
				{
					UpdateWindowTitle();
				}
			}
		}

		private string _outputPreview;
		/// <summary>
		/// Gets or sets the main output text that is displayed to the user as the final prompt preview.
		/// </summary>
		public string OutputPreview
		{
			get => _outputPreview;
			set => SetProperty(ref _outputPreview, value);
		}

		private string _tokenEstimate;
		/// <summary>
		/// Gets or sets a rough token estimate (for GPT-4 or GPT-3.5) based on the generated prompt text.
		/// </summary>
		public string TokenEstimate
		{
			get => _tokenEstimate;
			set => SetProperty(ref _tokenEstimate, value);
		}

		private bool _enableChunkingInFuture;
		/// <summary>
		/// Gets or sets a value indicating whether chunking is enabled for large file content.
		/// </summary>
		public bool EnableChunkingInFuture
		{
			get => _enableChunkingInFuture;
			set => SetProperty(ref _enableChunkingInFuture, value);
		}

		#endregion

		#region Commands
		/// <summary>
		/// Command to browse for a folder (using a FolderBrowserDialog), then immediately load it.
		/// </summary>
		public ICommand BrowseCommand { get; }

		/// <summary>
		/// Command to reload the folder specified by <see cref="RootFolder"/>, preserving checked states.
		/// </summary>
		public ICommand ReloadFolderCommand { get; }

		/// <summary>
		/// Command to generate the final prompt text based on selected template, user inputs,
		/// and file selections.
		/// </summary>
		public ICommand GeneratePromptCommand { get; }

		/// <summary>
		/// Command to clear all fields and reset the form (including directory items).
		/// </summary>
		public ICommand ClearAllFieldsCommand { get; }

		/// <summary>
		/// Command to copy the current <see cref="OutputPreview"/> text to the clipboard.
		/// </summary>
		public ICommand CopyToClipboardCommand { get; }
		#endregion

		#region Command Implementations

		/// <summary>
		/// Opens a folder browser dialog, sets <see cref="RootFolder"/>, and triggers folder reload.
		/// </summary>
		/// <param name="parameter">Not used.</param>
		private void BrowseForFolder(object parameter)
		{
			if (IsScanning) return;

			using (var dialog = new Forms.FolderBrowserDialog())
			{
				dialog.Description = "Select your .NET solution folder";
				dialog.UseDescriptionForTitle = true;
				dialog.ShowNewFolderButton = false;

				if (dialog.ShowDialog() == Forms.DialogResult.OK)
				{
					RootFolder = dialog.SelectedPath;
					_ = ReloadFolderAsync();
				}
			}
		}

		/// <summary>
		/// Asynchronously reloads the directory structure from <see cref="RootFolder"/> into <see cref="DirectoryItems"/>,
		/// preserving previously checked states if any.
		/// </summary>
		private async Task ReloadFolderAsync()
		{
			if (string.IsNullOrWhiteSpace(RootFolder)) return;

			try
			{
				IsScanning = true;
				OutputPreview = "Reloading folder... please wait.";

				// Step 1: Gather old states
				var oldStates = new Dictionary<string, bool>();
				foreach (var existingItem in DirectoryItems)
				{
					PopulateStates(existingItem, oldStates);
				}

				// Clear out existing items
				DirectoryItems.Clear();

				// Simulate short delay
				await Task.Delay(500);

				if (!Directory.Exists(RootFolder))
				{
					OutputPreview = $"Folder not found: {RootFolder}";
					return;
				}

				// Reload new structure
				var rootNode = LoadDirectoryNode(RootFolder);
				if (rootNode != null)
				{
					DirectoryItems.Add(rootNode);
				}

				// Step 2: Reapply old states
				foreach (var newItem in DirectoryItems)
				{
					ReapplyStates(newItem, oldStates);
				}

				OutputPreview = $"Reload complete! Folder: {RootFolder}";
			}
			finally
			{
				IsScanning = false;
			}
		}

		/// <summary>
		/// Recursively capture the existing IsChecked states in a dictionary of FullPath => bool.
		/// </summary>
		private void PopulateStates(DirectoryItemViewModel node, Dictionary<string, bool> states)
		{
			states[node.FullPath] = node.IsChecked;
			foreach (var child in node.Children)
			{
				PopulateStates(child, states);
			}
		}

		/// <summary>
		/// Recursively reapply captured states to the newly loaded nodes if they match by FullPath.
		/// </summary>
		private void ReapplyStates(DirectoryItemViewModel node, Dictionary<string, bool> states)
		{
			if (states.TryGetValue(node.FullPath, out bool wasChecked))
			{
				node.IsChecked = wasChecked;
			}
			foreach (var child in node.Children)
			{
				ReapplyStates(child, states);
			}
		}

		/// <summary>
		/// Loads a single directory node (including child subdirectories/files) recursively.
		/// Respects the 'Skip' checkboxes (e.g., SkipBin) to skip certain folders.
		/// </summary>
		/// <param name="path">The full path of the directory to load.</param>
		/// <returns>A <see cref="DirectoryItemViewModel"/> representing the folder and its children.</returns>
		private DirectoryItemViewModel LoadDirectoryNode(string path)
		{
			var dirInfo = new DirectoryInfo(path);
			if (ShouldSkip(dirInfo.Name)) return null;

			var node = new DirectoryItemViewModel
			{
				Name = dirInfo.Name,
				FullPath = path,
				IsChecked = false
			};

			DirectoryInfo[] subDirs;
			try
			{
				subDirs = dirInfo.GetDirectories();
			}
			catch
			{
				subDirs = Array.Empty<DirectoryInfo>();
			}

			foreach (var sd in subDirs)
			{
				if (ShouldSkip(sd.Name)) continue;
				var childDir = LoadDirectoryNode(sd.FullName);
				if (childDir != null)
				{
					node.Children.Add(childDir);
				}
			}

			FileInfo[] files;
			try
			{
				files = dirInfo.GetFiles();
			}
			catch
			{
				files = Array.Empty<FileInfo>();
			}

			foreach (var f in files)
			{
				if (ShouldSkipFile(f)) continue;

				var fileNode = new DirectoryItemViewModel
				{
					Name = f.Name,
					FullPath = f.FullName
				};
				node.Children.Add(fileNode);
			}

			return node;
		}

		/// <summary>
		/// Determines if the folderName should be skipped (e.g., 'bin', 'obj', '.vs', '.git', or 'node_modules')
		/// based on current user checkboxes.
		/// </summary>
		private bool ShouldSkip(string folderName)
		{
			if (SkipBin && folderName.Equals("bin", StringComparison.OrdinalIgnoreCase)) return true;
			if (SkipObj && folderName.Equals("obj", StringComparison.OrdinalIgnoreCase)) return true;
			if (SkipVs && folderName.Equals(".vs", StringComparison.OrdinalIgnoreCase)) return true;
			if (SkipGit && folderName.Equals(".git", StringComparison.OrdinalIgnoreCase)) return true;
			if (SkipNodeModules && folderName.Equals("node_modules", StringComparison.OrdinalIgnoreCase)) return true;
			return false;
		}

		/// <summary>
		/// Determines if a given file should be skipped based on extension or name (e.g., '.exe', '.dll', '.gitignore').
		/// </summary>
		private bool ShouldSkipFile(FileInfo file)
		{
			string[] skipExts = { ".exe", ".pdb", ".dll", ".obj", ".cache" };
			if (file.Name.Equals(".gitignore", StringComparison.OrdinalIgnoreCase)) return true;
			if (Array.Exists(skipExts, ext => ext.Equals(file.Extension, StringComparison.OrdinalIgnoreCase))) return true;

			return false;
		}

		/// <summary>
		/// Command to generate the final prompt text based on selected template, user inputs,
		/// and file selections.
		/// </summary>
		private void GeneratePrompt(object parameter)
		{
			if (IsScanning) return;

			var patterns = FileProcessingHelper.ParseRegexPatterns(RegexPatterns);
			bool enableChunking = EnableChunkingInFuture;
			int.TryParse(MaxLines, out int chunkLines);
			int.TryParse(LineRangeStart, out int lineStart);
			int.TryParse(LineRangeEnd, out int lineEnd);

			// Gather code snippet from checked DirectoryItems
			string existingCode = FileProcessingHelper.BuildSelectedFilesSectionFromViewModels(
				DirectoryItems,
				patterns,
				enableChunking,
				chunkLines,
				lineStart,
				lineEnd
			);

			// Optionally include directory structure
			string directoryStructure = "";
			if (IncludeDirectoryStructure)
			{
				directoryStructure = BuildDirectoryStructureStringFromViewModels(DirectoryItems, 0);
			}

			string finalPrompt;
			if (SelectedTemplate == "Codegen Prompt")
			{
				finalPrompt = BuildCodegenPrompt(existingCode);
			}
			else if (SelectedTemplate == "Review Prompt")
			{
				finalPrompt = BuildReviewPrompt(existingCode);
			}
			else
			{
				finalPrompt = BuildDefaultPrompt(existingCode, directoryStructure);
			}

			// Estimate tokens
			int tokenCount = TokenCounter.EstimateTokens(finalPrompt, "gpt-4");
			TokenEstimate = $"Est. Tokens: {tokenCount}";

			OutputPreview = finalPrompt;
		}

		/// <summary>
		/// Builds the final text for the "Codegen Prompt" scenario.
		/// </summary>
		private string BuildCodegenPrompt(string existingCode)
		{
			return $@"You are an AI code generator responsible for implementing a web application based on a provided technical specification and implementation plan.

                Your task is to systematically implement each step of the plan, one at a time.

                First, carefully review the following inputs:

                <project_request>
                {ProjectRequest}
                </project_request>

                <project_rules>
                {ProjectRules}
                </project_rules>

                <technical_specification>
                {TechnicalSpec}
                </technical_specification>

                <implementation_plan>
                {ImplementationPlan}
                </implementation_plan>

                <existing_code>
                {existingCode}
                </existing_code>

                Your task is to:
                1. Identify the next incomplete step from the implementation plan (marked with `- [ ]`)
                2. Generate the necessary code for all files specified in that step
                3. Return the generated code

                The implementation plan is just a suggestion meant to provide a high-level overview of the objective. Use it to guide you, but you do not have to adhere to it strictly. Make sure to follow the given rules as you work along the lines of the plan.

                For EVERY file you modify or create, you MUST provide the COMPLETE file contents in the format above.

                Each file should be wrapped in a code block with its file path above it and a ""Here's what I did and why"":
                
                Here's what I did and why: [text here...]
                Filepath: src/components/Example.tsx
                ```
                /**
                 * @description 
                 * This component handles [specific functionality].
                 * It is responsible for [specific responsibilities].
                 * 
                 * Key features:
                 * - Feature 1: Description
                 * - Feature 2: Description
                 * 
                 * @dependencies
                 * - DependencyA: Used for X
                 * - DependencyB: Used for Y
                 * 
                 * @notes
                 * - Important implementation detail 1
                 * - Important implementation detail 2
                 */

                BEGIN WRITING FILE CODE
                // Complete implementation with extensive inline comments & documentation...
                ```

                Documentation requirements:
                - File-level documentation explaining the purpose and scope
                - Component/function-level documentation detailing inputs, outputs, and behavior
                - Inline comments explaining complex logic or business rules
                - Type documentation for all interfaces and types
                - Notes about edge cases and error handling
                - Any assumptions or limitations

                Guidelines:
                - Implement exactly one step at a time
                - Ensure all code follows the project rules and technical specification
                - Include ALL necessary imports and dependencies
                - Write clean, well-documented code with appropriate error handling
                - Always provide COMPLETE file contents - never use ellipsis (...) or placeholder comments
                - Never skip any sections of any file - provide the entire file every time
                - Handle edge cases and add input validation where appropriate
                - Follow TypeScript best practices and ensure type safety
                - Include necessary tests as specified in the testing strategy

                Begin by identifying the next incomplete step from the plan, then generate the required code (with complete file contents and documentation).

                Above each file, include a ""Here's what I did and why"" explanation of what you did for that file.

                Then end with ""STEP X COMPLETE. Here's what I did and why:"" followed by an explanation of what you did and then a ""USER INSTRUCTIONS: Please do the following:"" followed by manual instructions for the user for things you can't do like installing libraries, updating configurations on services, etc.

                You also have permission to update the implementation plan if needed. If you update the implementation plan, include each modified step in full and return them as markdown code blocks at the end of the user instructions. No need to mark the current step as complete - that is implied.";
		}

		/// <summary>
		/// Builds the final text for the "Review Prompt" scenario.
		/// </summary>
		private string BuildReviewPrompt(string existingCode)
		{
			return $@"You are an expert code reviewer and optimizer responsible for analyzing the implemented code and creating a detailed optimization plan. Your task is to review the code that was implemented according to the original plan and generate a new implementation plan focused on improvements and optimizations.

                Please review the following context and implementation:

                <project_request>
                {ProjectRequest}
                </project_request>

                <project_rules>
                {ProjectRules}
                </project_rules>

                <technical_specification>
                {TechnicalSpec}
                </technical_specification>

                <implementation_plan>
                {ImplementationPlan}
                </implementation_plan>

                <existing_code>
                {existingCode}
                </existing_code>

                First, analyze the implemented code against the original requirements and plan. Consider the following areas:

                1. Code Organization and Structure
                   - Review implementation of completed steps against the original plan
                   - Identify opportunities to improve folder/file organization
                   - Look for components that could be better composed or hierarchically organized
                   - Find opportunities for code modularization
                   - Consider separation of concerns

                2. Code Quality and Best Practices
                   - Look for TypeScript/React anti-patterns
                   - Identify areas needing improved type safety
                   - Find places needing better error handling
                   - Look for opportunities to improve code reuse
                   - Review naming conventions

                3. UI/UX Improvements
                   - Review UI components against requirements
                   - Look for accessibility issues
                   - Identify component composition improvements
                   - Review responsive design implementation
                   - Check error message handling

                Wrap your analysis in <analysis> tags, then create a detailed optimization plan using the following format:

                ```md
                # Optimization Plan
                ## [Category Name]
                - [ ] Step 1: [Brief title]
                  - **Task**: [Detailed explanation of what needs to be optimized/improved]
                  - **Files**: [List of files]
                    - `path/to/file1.cs`: [Description of changes]
                  - **Step Dependencies**: [Any steps that must be completed first]
                  - **User Instructions**: [Any manual steps required]
                [Additional steps...]
                ```

                For each step in your plan:
                1. Focus on specific, concrete improvements
                2. Keep changes manageable (no more than 20 files per step, ideally less)
                3. Ensure steps build logically on each other
                4. Preserve starter template code and patterns
                5. Maintain existing functionality
                6. Follow project rules and technical specifications

                Your plan should be detailed enough for a code generation AI to implement each step in a single iteration. Order steps by priority and dependency requirements.

                Remember:
                - Focus on implemented code, not starter template code
                - Maintain consistency with existing patterns
                - Ensure each step is atomic and self-contained
                - Include clear success criteria for each step
                - Consider the impact of changes on the overall system

                Begin your response with your analysis of the current implementation, then proceed to create your detailed optimization plan.";
		}

		/// <summary>
		/// Builds the default prompt text (i.e., if "None" is selected),
		/// which includes project request, rules, specs, an optional directory structure,
		/// and selected file contents.
		/// </summary>
		/// <param name="existingCode">The aggregated file content from the user's checked items.</param>
		/// <param name="directoryStructure">Optional text-based directory structure listing.</param>
		/// <returns>A multi-line string representing the default prompt layout.</returns>
		private string BuildDefaultPrompt(string existingCode, string directoryStructure)
		{
			var sb = new StringBuilder();
			sb.AppendLine("PROJECT REQUEST:");
			sb.AppendLine(ProjectRequest);
			sb.AppendLine();

			sb.AppendLine("PROJECT RULES:");
			sb.AppendLine(ProjectRules);
			sb.AppendLine();

			sb.AppendLine("TECHNICAL SPECIFICATION:");
			sb.AppendLine(TechnicalSpec);
			sb.AppendLine();

			if (!string.IsNullOrEmpty(directoryStructure))
			{
				sb.AppendLine("DIRECTORY STRUCTURE:");
				sb.AppendLine(directoryStructure);
				sb.AppendLine();
			}

			sb.AppendLine("SELECTED FILES & CONTENT:");
			sb.Append(existingCode);
			sb.AppendLine();

			sb.AppendLine("IMPLEMENTATION PLAN:");
			sb.AppendLine(ImplementationPlan);

			return sb.ToString();
		}

		/// <summary>
		/// Recursively builds a text-based directory structure from the ViewModel side,
		/// for <see cref="DirectoryItems"/>. Used if <see cref="IncludeDirectoryStructure"/> is true.
		/// </summary>
		/// <param name="items">Collection of directory item viewmodels at the current level.</param>
		/// <param name="level">Indentation level for prefix spacing.</param>
		/// <returns>A multi-line string representing the hierarchical structure.</returns>
		private string BuildDirectoryStructureStringFromViewModels(IEnumerable<DirectoryItemViewModel> items, int level)
		{
			var sb = new StringBuilder();

			foreach (var node in items)
			{
				string prefix = new string(' ', level * 3) + "├─ ";
				sb.AppendLine(prefix + node.Name);

				if (node.Children.Count > 0)
				{
					sb.Append(BuildDirectoryStructureStringFromViewModels(node.Children, level + 1));
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Clears all user-input fields, resets the directory items, and switches to the "None" template.
		/// </summary>
		private void ClearAllFields(object parameter)
		{
			if (IsScanning) return;

			ProjectRequest = string.Empty;
			ProjectRules = string.Empty;
			TechnicalSpec = string.Empty;
			ImplementationPlan = string.Empty;
			RegexPatterns = string.Empty;
			MaxLines = "300";
			LineRangeStart = "0";
			LineRangeEnd = "0";
			OutputPreview = string.Empty;
			TokenEstimate = string.Empty;
			SkipBin = true;
			SkipObj = true;
			SkipVs = true;
			SkipGit = true;
			SkipNodeModules = true;
			IncludeDirectoryStructure = true;
			SelectedTemplate = "None";
			EnableChunkingInFuture = false;

			DirectoryItems.Clear();
		}

		/// <summary>
		/// Copies the current <see cref="OutputPreview"/> text to the system clipboard, if any text is present.
		/// </summary>
		private void CopyToClipboard(object parameter)
		{
			if (IsScanning) return;

			if (!string.IsNullOrWhiteSpace(OutputPreview))
			{
				System.Windows.Clipboard.SetText(OutputPreview);
				OutputPreview += "\n[Copied to Clipboard]";
			}
		}

		#endregion

		#region Helper Methods

		/// <summary>
		/// Updates <see cref="WindowTitle"/> based on the last folder name in <see cref="RootFolder"/>.
		/// If no folder is set, default to "Prompt Normalizer".
		/// </summary>
		private void UpdateWindowTitle()
		{
			if (!string.IsNullOrEmpty(RootFolder))
			{
				string folderName = Path.GetFileName(RootFolder.TrimEnd('\\', '/'));
				WindowTitle = $"{folderName} | Prompt Normalizer";
			}
			else
			{
				WindowTitle = "Prompt Normalizer";
			}
		}

		#endregion
	}
}
