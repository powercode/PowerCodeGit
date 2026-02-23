---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Get-GitDiff.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-22-2026
PlatyPS schema version: 2024-05-01
title: Get-GitDiff
---

# Get-GitDiff

## SYNOPSIS

Shows changes between the working tree, index, or commits, equivalent to git diff.

## SYNTAX

### WorkingTree (Default)

```
Get-GitDiff [-Path <string[]>] [-IgnoreWhitespace] [-Context <int>] [-Hunk] [-RepoPath <string>] [<CommonParameters>]
```

### Staged

```
Get-GitDiff -Staged [-Path <string[]>] [-IgnoreWhitespace] [-Context <int>] [-Hunk] [-RepoPath <string>]
 [<CommonParameters>]
```

### Commit

```
Get-GitDiff [-Commit] <string> [-Path <string[]>] [-IgnoreWhitespace] [-Context <int>] [-Hunk] [-RepoPath <string>]
 [<CommonParameters>]
```

### Range

```
Get-GitDiff [-FromCommit] <string> [-ToCommit] <string> [-Path <string[]>] [-IgnoreWhitespace]
 [-Context <int>] [-Hunk] [-RepoPath <string>] [<CommonParameters>]
```

### Options

```
Get-GitDiff -Options <GitDiffOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Get-GitDiff cmdlet retrieves diff entries showing file-level changes. By default it compares the working tree against the index (unstaged changes). Use `-Staged` to see index vs HEAD, `-Commit` to compare working tree against a specific commit, or `-FromCommit`/`-ToCommit` for a range diff.

Each entry is returned as a GitDiffEntry object with properties such as OldPath, NewPath, Status, LinesAdded, LinesDeleted, and Patch.

Use the `-Hunk` switch to break each file's diff into individual GitDiffHunk objects. Hunks can be filtered in the pipeline and piped to `Add-GitItem` for selective staging.

## EXAMPLES

### Example 1 - Show unstaged changes

Shows all unstaged working tree changes.

```powershell
Get-GitDiff
```

### Example 2 - Show staged changes

Shows changes that have been staged for the next commit.

```powershell
Get-GitDiff -Staged
```

### Example 3 - Return individual diff hunks

Returns individual diff hunks instead of file-level entries, useful for filtering and selective staging.

```powershell
Get-GitDiff -Hunk
```

### Example 4 - Selectively stage hunks from C# files

Filters hunks to only those in C# files and stages them.

```powershell
Get-GitDiff -Hunk | Where-Object { $_.FilePath -like '*.cs' } | Add-GitItem
```

### Example 5 - Stage only hunks that contain added lines

Uses the `Lines` property to inspect each hunk's parsed diff lines and stages only hunks that contain at least one added line. Hunks that only delete lines are skipped.

```powershell
Get-GitDiff -Hunk |
    Where-Object { $_.Lines | Where-Object Kind -eq 'Added' } |
    Add-GitItem
```

### Example 6 - Show unstaged changes with no surrounding context

Returns the diff with zero context lines around each change, equivalent to `git diff -U0`. Useful when you only want to see the exact lines that changed without surrounding context.

```powershell
Get-GitDiff -Context 0
```

## PARAMETERS

### -Context

The number of context lines surrounding each change in the diff output. Equivalent to `git diff -U<n>` / `--unified=<n>`. When omitted, the library default of 3 context lines is used. Use `-Context 0` to suppress context entirely.

```yaml
Type: System.Int32
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: WorkingTree
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Staged
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Commit
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Range
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Commit

A committish to diff the working tree against.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Commit
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -FromCommit

The starting committish for a range diff.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Range
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Hunk

Emits individual GitDiffHunk objects instead of file-level GitDiffEntry objects. Each hunk represents a single `@@ ... @@` block from the unified diff output. Hunk objects can be filtered and piped to `Add-GitItem` for selective staging.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: WorkingTree
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Staged
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Commit
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Range
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IgnoreWhitespace

Ignores whitespace changes in the diff output.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: WorkingTree
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Staged
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Commit
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Range
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Options

A pre-built GitDiffOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitDiffOptions
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Options
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

One or more repository-relative file paths to restrict the diff output.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: WorkingTree
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Staged
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Commit
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Range
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RepoPath

Path to the git repository. Defaults to the current PowerShell location.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- RepositoryPath
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Staged

Shows staged (index) changes compared to HEAD. Equivalent to `git diff --staged`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Staged
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ToCommit

The ending committish for a range diff.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Range
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitDiffEntry

A diff entry object with OldPath, NewPath, Status, LinesAdded, LinesDeleted, and Patch properties. Emitted by default.

### PowerCode.Git.Abstractions.Models.GitDiffHunk

A diff hunk object with FilePath, OldPath, Status, OldStart, OldLineCount, NewStart, NewLineCount, Header, Content, LinesAdded, and LinesDeleted properties. Emitted when `-Hunk` is specified. The `Lines` property returns a lazily-parsed, cached collection of `GitDiffLine` objects, each with `OldLineNumber`, `NewLineNumber`, `Kind` (`Added`, `Removed`, or `Modified`), and `Content`.

## NOTES

Use Add-GitItem to stage changes and Reset-GitHead to unstage them.

## RELATED LINKS

- [Add-GitItem](Add-GitItem.md)
- [Get-GitStatus](Get-GitStatus.md)
- [Reset-GitHead](Reset-GitHead.md)
