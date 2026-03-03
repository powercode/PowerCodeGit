---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Compare-GitTree.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 03-03-2026
PlatyPS schema version: 2024-05-01
title: Compare-GitTree
---

# Compare-GitTree

## SYNOPSIS

Compares two tree-ish references and returns file-level diff entries, equivalent to git diff <base> <compare>.

## SYNTAX

### __AllParameterSets

```
Compare-GitTree [-Base] <string> [-Compare] <string> [-Where <scriptblock>]
 [-Transform <scriptblock>] [-IgnoreWhitespace] [-Path <string[]>] [-RepoPath <string>]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Compare-GitTree cmdlet computes the diff between two commits, branches, or tags and emits one GitDiffEntry per changed file. This is the tree-to-tree equivalent of `git diff <base> <compare>`.

Use `-Where` to filter entries with a PowerShell ScriptBlock predicate. The current GitDiffEntry is available as `$change` and `$args[0]`.

Use `-Transform` to project each entry into a different shape before it reaches the pipeline.

## EXAMPLES

### Example 1 - Compare two branches

Shows all file changes between the main and feature branches.

```powershell
Compare-GitTree -Base main -Compare feature
```

### Example 2 - Filter to only added files

Uses the `-Where` parameter to return only files that were added.

```powershell
Compare-GitTree -Base HEAD~1 -Compare HEAD -Where { $change.Status -eq 'Added' }
```

### Example 3 - Extract just the file paths

Uses the `-Transform` parameter to project each entry to its new path.

```powershell
Compare-GitTree -Base main -Compare feature -Transform { $change.NewPath }
```

### Example 4 - Restrict comparison to specific paths

Compares only the specified file between two commits.

```powershell
Compare-GitTree -Base HEAD~3 -Compare HEAD -Path 'src/app.cs'
```

## PARAMETERS

### -Base

The base tree-ish reference (branch name, tag, or commit SHA) to compare from.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Compare

The comparison tree-ish reference (branch name, tag, or commit SHA) to compare to.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IgnoreWhitespace

When specified, whitespace-only changes are ignored. Reserved for future LibGit2Sharp support.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
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

### -Path

One or more repository-relative file paths to restrict the comparison output.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
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

### -RepoPath

Path to the git repository. When omitted, the current PowerShell working directory is used.

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

### -Transform

A ScriptBlock that transforms each diff entry before it is written to the pipeline. The current GitDiffEntry is available as `$change` and `$args[0]`.

```yaml
Type: System.Management.Automation.ScriptBlock
DefaultValue: ''
SupportsWildcards: false
Aliases: []
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

### -Where

A ScriptBlock predicate that filters diff entries. The current GitDiffEntry is available as `$change` and `$args[0]`. Return `$true` to include the entry in results.

```yaml
Type: System.Management.Automation.ScriptBlock
DefaultValue: ''
SupportsWildcards: false
Aliases: []
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitDiffEntry

One object per changed file, with OldPath, NewPath, Status, LinesAdded, LinesDeleted, and Patch properties.

## NOTES

This cmdlet uses LibGit2Sharp in an isolated AssemblyLoadContext. The -IgnoreWhitespace switch is accepted but not yet honoured because LibGit2Sharp CompareOptions does not expose the property.

## RELATED LINKS

- [Get-GitDiff](Get-GitDiff.md)
- [Get-GitCommitFile](Get-GitCommitFile.md)

