/**
 * @description
 * Provides static helper methods for file and directory processing, extracted from MainWindow.xaml.cs.
 *
 * Now includes additional overloads for DirectoryItemViewModel:
 *  - BuildSelectedFilesSectionFromViewModels
 *  - BuildAllSubFilesFromViewModels
 *
 * Original methods:
 *  - BuildDirectoryStructureString (TreeView-based)
 *  - BuildSelectedFilesSection (TreeView-based)
 *  - ProcessFileContent
 *  - ApplyRegexRedactions
 *  - ChunkFileContent
 *  - ParseRegexPatterns
 *
 * @notes
 * - Improved XML documentation to clarify inputs, outputs, and usage.
 * - Minor style consistency changes.
 * - **Added GetCodeBlockLanguage method to identify the appropriate code fence language for each file.**
 */

using PromptBuilderApp.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace PromptBuilderApp.Helpers
{
	/// <summary>
	/// Provides helper methods for reading/processing file content and building text snippets
	/// for inclusion in AI prompts. Contains separate methods for both TreeView-based selection
	/// and ViewModel-based selection (DirectoryItemViewModel).
	/// </summary>
	public static class FileProcessingHelper
	{
		#region Original TreeView-based Methods
		/// <summary>
		/// Recursively builds a text-based directory structure string from a <see cref="TreeView"/> hierarchy.
		/// Uses an integer <paramref name="level"/> to indent lines for a tree-like layout.
		/// </summary>
		/// <param name="items">The <see cref="ItemCollection"/> from a WPF <see cref="TreeView"/>.</param>
		/// <param name="level">Indentation level to determine prefix spacing.</param>
		/// <returns>A formatted, multi-line string representing the directory structure.</returns>
		public static string BuildDirectoryStructureString(ItemCollection items, int level)
		{
			var sb = new StringBuilder();

			foreach (TreeViewItem item in items)
			{
				if (item.Header is System.Windows.Controls.CheckBox cb)
				{
					string prefix = new string(' ', level * 3) + "├─ ";
					sb.AppendLine(prefix + cb.Content);

					if (item.Items.Count > 0)
					{
						sb.Append(BuildDirectoryStructureString(item.Items, level + 1));
					}
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Builds a snippet string of all checked files (and their contents)
		/// from a TreeView-based directory hierarchy.
		/// </summary>
		/// <param name="items">TreeView items collection to process.</param>
		/// <param name="regexPatterns">List of regex patterns to redact or replace content.</param>
		/// <param name="enableChunking">If true, large file content is split into multiple chunks.</param>
		/// <param name="maxLines">Max lines per chunk if <paramref name="enableChunking"/> is true.</param>
		/// <param name="lineStart">Optional line start index for partial content (1-based).</param>
		/// <param name="lineEnd">Optional line end index for partial content (1-based).</param>
		/// <returns>A formatted string of included file content in code blocks.</returns>
		public static string BuildSelectedFilesSection(
			ItemCollection items,
			List<(Regex pattern, string replacement)> regexPatterns,
			bool enableChunking,
			int maxLines,
			int lineStart,
			int lineEnd
		)
		{
			var sb = new StringBuilder();

			foreach (object obj in items)
			{
				if (obj is TreeViewItem item && item.Header is System.Windows.Controls.CheckBox cb)
				{
					bool isFolderChecked = cb.IsChecked == true;
					if (item.Items.Count > 0)
					{
						// If folder is checked, gather all subfiles
						if (isFolderChecked)
						{
							sb.Append(BuildAllSubFiles(item, regexPatterns, enableChunking, maxLines, lineStart, lineEnd));
						}
						else
						{
							// Keep scanning sub-folders
							sb.Append(BuildSelectedFilesSection(item.Items, regexPatterns, enableChunking, maxLines, lineStart, lineEnd));
						}
					}
					else
					{
						// It's a file
						if (isFolderChecked)
						{
							string filePath = cb.Tag as string;
							if (File.Exists(filePath))
							{
								string processedContent = ProcessFileContent(filePath, regexPatterns, lineStart, lineEnd);

								if (enableChunking)
								{
									sb.Append(ChunkFileContent(filePath, processedContent, maxLines));
								}
								else
								{
									string codeFenceLang = GetCodeBlockLanguage(filePath);
									sb.AppendLine($"```{codeFenceLang}");
									sb.AppendLine($"// FILE: {filePath}");
									sb.AppendLine(processedContent);
									sb.AppendLine("```");
									sb.AppendLine();
								}
							}
						}
					}
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Recursively gathers sub-files if a parent folder is checked (TreeView approach).
		/// </summary>
		public static string BuildAllSubFiles(
			TreeViewItem folderItem,
			List<(Regex pattern, string replacement)> regexPatterns,
			bool enableChunking,
			int maxLines,
			int lineStart,
			int lineEnd
		)
		{
			var sb = new StringBuilder();

			foreach (object obj in folderItem.Items)
			{
				if (obj is TreeViewItem child && child.Header is System.Windows.Controls.CheckBox childCb)
				{
					if (child.Items.Count > 0)
					{
						sb.Append(BuildAllSubFiles(child, regexPatterns, enableChunking, maxLines, lineStart, lineEnd));
					}
					else
					{
						string filePath = childCb.Tag as string;
						if (File.Exists(filePath))
						{
							string processedContent = ProcessFileContent(filePath, regexPatterns, lineStart, lineEnd);

							if (enableChunking)
							{
								sb.Append(ChunkFileContent(filePath, processedContent, maxLines));
							}
							else
							{
								string codeFenceLang = GetCodeBlockLanguage(filePath);
								sb.AppendLine($"```{codeFenceLang}");
								sb.AppendLine($"// FILE: {filePath}");
								sb.AppendLine(processedContent);
								sb.AppendLine("```");
								sb.AppendLine();
							}
						}
					}
				}
			}

			return sb.ToString();
		}
		#endregion

		#region New Overloads for DirectoryItemViewModel
		/// <summary>
		/// Builds a snippet string of all checked files (and their contents)
		/// from a set of <see cref="DirectoryItemViewModel"/> nodes.
		/// </summary>
		/// <param name="items">Collection of directory/file nodes at the current level.</param>
		/// <param name="regexPatterns">List of regex patterns to redact or replace content.</param>
		/// <param name="enableChunking">If true, large file content is split into multiple chunks.</param>
		/// <param name="maxLines">Max lines per chunk if <paramref name="enableChunking"/> is true.</param>
		/// <param name="lineStart">Optional line start index for partial content (1-based).</param>
		/// <param name="lineEnd">Optional line end index for partial content (1-based).</param>
		/// <returns>A formatted string of included file content in code blocks.</returns>
		public static string BuildSelectedFilesSectionFromViewModels(
			IEnumerable<DirectoryItemViewModel> items,
			List<(Regex pattern, string replacement)> regexPatterns,
			bool enableChunking,
			int maxLines,
			int lineStart,
			int lineEnd
		)
		{
			var sb = new StringBuilder();

			foreach (var node in items)
			{
				if (node.Children.Count > 0)
				{
					if (node.IsChecked)
					{
						// If the folder is checked, gather all subfiles
						sb.Append(BuildAllSubFilesFromViewModels(node, regexPatterns, enableChunking, maxLines, lineStart, lineEnd));
					}
					else
					{
						// Keep scanning sub-folders
						sb.Append(BuildSelectedFilesSectionFromViewModels(node.Children, regexPatterns, enableChunking, maxLines, lineStart, lineEnd));
					}
				}
				else
				{
					// It's presumably a file
					if (node.IsChecked)
					{
						string filePath = node.FullPath;
						if (File.Exists(filePath))
						{
							string processedContent = ProcessFileContent(filePath, regexPatterns, lineStart, lineEnd);

							if (enableChunking)
							{
								sb.Append(ChunkFileContent(filePath, processedContent, maxLines));
							}
							else
							{
								string codeFenceLang = GetCodeBlockLanguage(filePath);
								sb.AppendLine($"```{codeFenceLang}");
								sb.AppendLine($"// FILE: {filePath}");
								sb.AppendLine(processedContent);
								sb.AppendLine("```");
								sb.AppendLine();
							}
						}
					}
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Recursively gathers sub-files if a parent folder is checked, from the <see cref="DirectoryItemViewModel"/> perspective.
		/// </summary>
		/// <param name="folder">The parent folder node.</param>
		/// <param name="regexPatterns">List of regex patterns to apply to file content.</param>
		/// <param name="enableChunking">If true, large file content is split into multiple chunks.</param>
		/// <param name="maxLines">Max lines per chunk if <paramref name="enableChunking"/> is true.</param>
		/// <param name="lineStart">Optional line start index for partial content (1-based).</param>
		/// <param name="lineEnd">Optional line end index for partial content (1-based).</param>
		/// <returns>A formatted string of included file content in code blocks.</returns>
		public static string BuildAllSubFilesFromViewModels(
			DirectoryItemViewModel folder,
			List<(Regex pattern, string replacement)> regexPatterns,
			bool enableChunking,
			int maxLines,
			int lineStart,
			int lineEnd
		)
		{
			var sb = new StringBuilder();

			foreach (var child in folder.Children)
			{
				if (child.Children.Count > 0)
				{
					sb.Append(BuildAllSubFilesFromViewModels(child, regexPatterns, enableChunking, maxLines, lineStart, lineEnd));
				}
				else
				{
					string filePath = child.FullPath;
					if (File.Exists(filePath))
					{
						string processedContent = ProcessFileContent(filePath, regexPatterns, lineStart, lineEnd);

						if (enableChunking)
						{
							sb.Append(ChunkFileContent(filePath, processedContent, maxLines));
						}
						else
						{
							string codeFenceLang = GetCodeBlockLanguage(filePath);
							sb.AppendLine($"```{codeFenceLang}");
							sb.AppendLine($"// FILE: {filePath}");
							sb.AppendLine(processedContent);
							sb.AppendLine("```");
							sb.AppendLine();
						}
					}
				}
			}

			return sb.ToString();
		}
		#endregion

		#region Processing & Redaction Methods
		/// <summary>
		/// Loads the file content from disk, applies optional line-range filtering,
		/// then applies any configured regex redactions.
		/// </summary>
		/// <param name="filePath">Path to the file.</param>
		/// <param name="regexPatterns">List of (pattern,replacement) pairs for redaction.</param>
		/// <param name="lineStart">Optional line start (1-based). If zero or below, includes from the top.</param>
		/// <param name="lineEnd">Optional line end (1-based). If zero or below, includes until the end.</param>
		/// <returns>The processed file content as a string.</returns>
		public static string ProcessFileContent(
			string filePath,
			List<(Regex pattern, string replacement)> regexPatterns,
			int lineStart,
			int lineEnd
		)
		{
			string[] lines = File.ReadAllLines(filePath);

			int actualStart = (lineStart > 0) ? Math.Min(lineStart, lines.Length) : 1;
			int actualEnd = (lineEnd > 0) ? Math.Min(lineEnd, lines.Length) : lines.Length;

			var subset = lines
				.Skip(actualStart - 1)
				.Take(actualEnd - (actualStart - 1))
				.ToArray();

			for (int i = 0; i < subset.Length; i++)
			{
				subset[i] = ApplyRegexRedactions(subset[i], regexPatterns);
			}

			return string.Join(Environment.NewLine, subset);
		}

		/// <summary>
		/// Applies a list of (pattern, replacement) pairs to the input line, returning the updated line.
		/// </summary>
		public static string ApplyRegexRedactions(string line, List<(Regex pattern, string replacement)> regexPatterns)
		{
			foreach (var (pattern, replacement) in regexPatterns)
			{
				line = pattern.Replace(line, replacement);
			}
			return line;
		}

		/// <summary>
		/// Splits file content into multiple code blocks if it exceeds <paramref name="maxLines"/>.
		/// Useful for including large files in AI prompts without exceeding token limits.
		/// </summary>
		/// <param name="filePath">The file path (shown in a comment block).</param>
		/// <param name="fileContent">Raw content of the file.</param>
		/// <param name="maxLines">Max lines allowed in one chunk.</param>
		/// <returns>Multiple code blocks if needed, or a single block otherwise.</returns>
		public static string ChunkFileContent(string filePath, string fileContent, int maxLines)
		{
			var lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			string codeFenceLang = GetCodeBlockLanguage(filePath);

			if (lines.Length <= maxLines)
			{
				var singleBlock = new StringBuilder();
				singleBlock.AppendLine($"```{codeFenceLang}");
				singleBlock.AppendLine($"// FILE: {filePath}");
				singleBlock.AppendLine(fileContent);
				singleBlock.AppendLine("```");
				singleBlock.AppendLine();
				return singleBlock.ToString();
			}
			else
			{
				var multiBlock = new StringBuilder();
				int currentLine = 0;
				int chunkIndex = 1;

				while (currentLine < lines.Length)
				{
					var chunk = lines.Skip(currentLine).Take(maxLines).ToArray();
					currentLine += maxLines;

					multiBlock.AppendLine($"```{codeFenceLang}");
					multiBlock.AppendLine($"// FILE: {filePath} (Chunk #{chunkIndex})");
					multiBlock.AppendLine(string.Join(Environment.NewLine, chunk));
					multiBlock.AppendLine("```");
					multiBlock.AppendLine();

					chunkIndex++;
				}

				return multiBlock.ToString();
			}
		}

		/// <summary>
		/// Parses a text input containing lines of format:
		/// (pattern) => replacement
		/// and returns a list of compiled regex patterns with their associated replacements.
		/// </summary>
		/// <param name="text">
		/// The multi-line string with lines of the form:
		/// <c>(ApiKey=)([^\\s]+) => $1[REDACTED]</c>
		/// </param>
		/// <returns>A list of (Regex, string) pairs for redaction.</returns>
		public static List<(Regex pattern, string replacement)> ParseRegexPatterns(string text)
		{
			var result = new List<(Regex, string)>();
			if (string.IsNullOrWhiteSpace(text))
				return result;

			var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string line in lines)
			{
				var parts = line.Split(new[] { " => " }, StringSplitOptions.None);
				if (parts.Length == 2)
				{
					try
					{
						var pattern = new Regex(parts[0], RegexOptions.Compiled);
						var replacement = parts[1];
						result.Add((pattern, replacement));
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine("Regex parse error: " + ex.Message);
					}
				}
			}

			return result;
		}
		#endregion

		#region Extension-Based Language Detection

		/// <summary>
		/// Returns the appropriate code fence language string based on file extension.
		/// Defaults to empty (no syntax) if not recognized.
		/// </summary>
		/// <param name="filePath">Full path to the file.</param>
		/// <returns>A string like 'csharp', 'js', 'ts', etc. for code blocks.</returns>
		private static string GetCodeBlockLanguage(string filePath)
		{
			string ext = Path.GetExtension(filePath).ToLowerInvariant();

			// This switch can be extended with other file types as needed
			return ext switch
			{
				".cs" => "csharp",
				".cshtml" => "cshtml",
				".js" => "js",
				".jsx" => "jsx",
				".ts" => "ts",
				".tsx" => "tsx",
				".md" => "md",
				".txt" => "txt",
				".log" => "log",
				".json" => "json",
				".css" => "css",
				".scss" => "scss",
				_ => ""  // fallback, no specific syntax
			};
		}

		#endregion
	}
}
