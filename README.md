# Prompt Normalizer for .NET
Made with o1 Pro using Prompt Normalizer. Thanks to https://www.jointakeoff.com/prompts for the prompt library.

The **Prompt Normalizer** is a WPF-based application designed to streamline the process of generating custom prompts for AI-based .NET code generation and review. It enables you to browse and select .NET solution folders, skip unnecessary files or directories, and assemble your user question plus file snippets into a single AI-ready prompt.

## Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Installation & Setup](#installation--setup)
- [Usage](#usage)
- [Architecture](#architecture)
- [File/Folder Descriptions](#filefolder-descriptions)
- [Regex Redaction](#regex-redaction)
- [Prompt Templates](#prompt-templates)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

This project was originally created to simplify AI prompt authoring for .NET coding tasks. It analyzes and includes selected code files from your chosen .NET solution, applying optional regex redactions and chunking for large files. It then merges them into a structured prompt that can be pasted into an LLM environment (like ChatGPT). Build it yourself, or grab the executable from https://promptnormalizer.com.

**Why**: As codebases grow, it’s cumbersome to assemble prompts that reference multiple files, partial line ranges, or specific disclaimers. Prompt Normalizer addresses this challenge by letting you:
- Automatically detect and filter folders (e.g. `bin`, `obj`, `node_modules`)
- Select which files to include
- Redact sensitive tokens via regex
- Generate a neatly structured final prompt

---

## Features

1. **Folder Browser & Skips**  
   - Browse your local system to pick the root solution folder.  
   - Automatically skip typical build/artifact directories like `bin`, `obj`, `.git`, etc.

2. **Regex Redactions**  
   - Specify line-by-line patterns in the format `(pattern) => replacement`.
   - Redact or sanitize any secrets (like API keys) with ease.

3. **Line Chunking & Range**  
   - Enable chunking for large files, splitting them into smaller sections of N lines.
   - Optionally specify a line range (`start-end`) to only include relevant code lines.

4. **Prompt Templates**  
   - Choose from different specialized prompts (e.g., “Codegen Prompt,” “Review Prompt,” “Request Prompt”) or use “None” to see the default layout.  
   - The user question can be automatically omitted if a special template is selected.

5. **Modern Flat UI**  
   - Sleek WPF styles for a consistent, up-to-date user experience.
   - Organized tabs: 
     - **Metadata & Settings** for prompt options
     - **Selection & Preview** for picking files and viewing final output

6. **Auto Load**  
   - The folder automatically loads after you select it, showing directory structures in a TreeView.

7. **Token Estimation**  
   - Integrates [SharpToken](https://github.com/datalanche/SharpToken) to estimate how many tokens your prompt might use on GPT-4 or GPT-3.5.

---

## Installation & Setup

1. **Prerequisites**  
   - [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (or later)  
   - Visual Studio 2022 (or any IDE supporting WPF .NET projects)  
   - [SharpToken NuGet Package](https://www.nuget.org/packages/SharpToken/) is already referenced in the `.csproj`.

2. **Cloning the Repo**  
   ```bash
   git clone https://github.com/jshaust/PromptNormalizerApp.git
   cd PromptNormalizerApp
   ```
3.  **Open in Visual Studio**

    -   Double-click `PromptNormalizerApp.sln` to open the solution.
    -   Restore NuGet packages if needed.
4.  **Build & Run**

    -   Press F5 in Visual Studio or run `dotnet build && dotnet run` from the CLI inside the project folder.

---

Usage
-----

1.  **Select Folder**

    -   Click **Browse...** to choose your .NET solution folder. The application automatically loads your directory structure into the left TreeView.
2.  **Skip Folders**

    -   Check or uncheck "bin," "obj," and others you might want to exclude from scanning.
3.  **Metadata**

    -   Fill out "Project Summary," "Constraints," and "Technical Specification" to provide context for your prompt.
    -   Add "Regex Redaction Patterns" in the format `(someRegex) => replacement`.
    -   Optionally enable chunking or specify line ranges for partial inclusion.
4.  **Template**

    -   Choose "None" to see a basic prompt structure with your user question.
    -   Or pick "Codegen Prompt," "Review Prompt," or "Request Prompt" to see specialized placeholders.
5.  **Include Directory Structure**

    -   Check if you want the final prompt to show a hierarchical directory listing.
6.  **Generate Prompt**

    -   Switch to the "Selection & Preview" tab, or use **Generate Prompt** on either tab. The final text appears in the preview box.
7.  **Copy Prompt**

    -   Click **Copy to Clipboard** to capture the entire text. Paste it into your AI environment as needed.
8.  **Token Estimate**

    -   "Est. Tokens" displays an approximate token count for GPT-4. This helps you gauge model usage costs or token limits.

---

Architecture
------------

**Core Components:**

-   **`MainWindow.xaml`** (plus `.cs` code-behind): Handles UI layout and user interactions.
-   **`TokenCounter.cs`**: Integrates with SharpToken for token counting.
-   **`DirectoryTree`**: A `TreeView` that is dynamically populated with checkboxes for each folder/file.
-   **`ModernTheme.xaml`**: Resource dictionary for the flat, modern UI styling.
-   **`App.xaml`**: Merges the chosen resource dictionary and sets `StartupUri="MainWindow.xaml"`.

**Flow:**

1.  User picks folder → we scan and display it in `DirectoryTree`.
2.  User selects files → we gather them, optionally chunk them, and build a final string in preview.
3.  The result merges user input (constraints, technical specs, question) plus code from selected files, minus any redacted text.

---

File/Folder Descriptions
------------------------

-   `PromptNormalizerApp.sln` -- The Visual Studio solution file.
-   `PromptNormalizerApp.csproj` -- Main project definition referencing .NET 8 WPF and SharpToken.
-   **`MainWindow.xaml`** -- Defines UI layout with DockPanels, Tabs, and GroupBoxes.
-   **`MainWindow.xaml.cs`** -- Contains code for folder loading, file selection, prompt generation, etc.
-   `ModernTheme.xaml` -- Applies a modern flat design to WPF controls.
-   `App.xaml` -- Merges `ModernTheme.xaml` and sets `StartupUri="MainWindow.xaml"`.
-   `TokenCounter.cs` -- Houses token estimation logic using SharpToken library.

Other automatically generated or standard .NET files (e.g., `AssemblyInfo.cs`, `Properties/`, etc.) also exist, but the above are the main points of interest.

---

Regex Redaction
---------------

-   **Add Patterns** under "Regex Redaction Patterns," one rule per line, e.g.:

    bash

    Copy

    `(ApiKey=)(\S+) => $1[REDACTED]`

    This will convert any line containing `ApiKey=someSecret` into `ApiKey=[REDACTED]`.

-   **Implementation** uses `Regex.Replace` at runtime to systematically remove or mask matched strings.

---

Prompt Templates
----------------

-   **None** -- Builds a default prompt containing:

    -   PROJECT SUMMARY
    -   CONSTRAINTS
    -   DIRECTORY STRUCTURE (optional)
    -   SELECTED FILES & CONTENT
    -   USER QUESTION
-   **Codegen Prompt** -- Instructs the AI to implement steps from a plan.

-   **Review Prompt** -- Asks the AI to review or optimize code per the original plan.

-   **Request Prompt** -- Asks the AI to help build a project request specification from user input.

*(These templates are defined as constant strings in `MainWindow.xaml.cs`.)*

---

Contributing
------------

1.  **Fork** the repo and create your feature branch:

    bash

    Copy

    `git checkout -b feature/awesome-improvement`

2.  **Commit** your changes:

    bash

    Copy

    `git commit -am 'Add some awesome improvement'`

3.  **Push** to your branch:

    bash

    Copy

    `git push origin feature/awesome-improvement`

4.  **Open** a Pull Request describing your changes.

We welcome pull requests that enhance UI, improve code clarity, add new prompt templates, or optimize performance.

---

License
-------

This project is licensed under the MIT License. You're free to modify, distribute, or incorporate this code into your own projects under the terms of MIT.