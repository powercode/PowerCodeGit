---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Restore-GitItem.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-22-2026
PlatyPS schema version: 2024-05-01
title: Restore-GitItem
---

# Restore-GitItem

## SYNOPSIS

Discards working-tree changes or unstages index changes for files in a git repository, equivalent to git restore.

## SYNTAX

### Path (Default)

```
Restore-GitItem [[-Path] <string[]>] [-Staged] [-Source <string>] [-RepoPath <string>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

### All

```
Restore-GitItem -All [-Staged] [-Source <string>] [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### Hunk

```
Restore-GitItem -Hunk <GitDiffHunk[]> [-Staged] [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### InputObject

```
Restore-GitItem -InputObject <psobject> [-Staged] [-Source <string>] [-RepoPath <string>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

### Options

```
Restore-GitItem -Options <GitRestoreOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Restore-GitItem cmdlet discards working-tree changes or unstages index changes for one or more files in a git repository. By default it restores working-tree files to match HEAD. Use `-Staged` to unstage changes from the index instead. Use `-All` to restore every modified file, `-Hunk` to revert individual diff hunks piped from `Get-GitDiff -Hunk`, or pipe status entries from `Get-GitStatus` to restore specific files via property binding.

## EXAMPLES

### Example 1 - Discard working-tree changes for a single file

Restores a single file so its content matches HEAD.

```powershell
Restore-GitItem -Path ./file.txt
```

### Example 2 - Discard all working-tree changes

Restores all modified files in the repository.

```powershell
Restore-GitItem -All
```

### Example 3 - Unstage a staged file

Removes a file from the index (unstages it) without touching the working tree.

```powershell
Restore-GitItem -Path ./file.txt -Staged
```

### Example 4 - Revert individual hunks from Get-GitDiff

Pipes diff hunks from `Get-GitDiff -Hunk` and reverts them by applying an inverted patch.

```powershell
Get-GitDiff -Hunk | Restore-GitItem
```

### Example 5 - Restore files piped from Get-GitStatus

Pipes status entries to restore all modified files using the FilePath property binding.

```powershell
Get-GitStatus | Select-Object -ExpandProperty Entries | Restore-GitItem
```

## PARAMETERS

### -All

Restores all modified files in the repository. Mutually exclusive with `-Path` and `-Hunk`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: All
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- cf
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

### -Hunk

One or more diff hunks to revert. Accepts pipeline input from `Get-GitDiff -Hunk`. Applies an inverted patch via `git apply -R`. Mutually exclusive with `-Path` and `-All`.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitDiffHunk[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Hunk
  Position: Named
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -InputObject

An object whose path is resolved by inspecting its `FilePath`, `NewPath`, or `Path` property (in that order). Use this parameter explicitly when piping objects whose type is not covered by the `Hunk` or `Path` parameter sets.

```yaml
Type: System.Management.Automation.PSObject
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: InputObject
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Options

A pre-built GitRestoreOptions object for full programmatic control. When specified, all other parameters are ignored.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitRestoreOptions
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

One or more repository-relative file paths to restore. Mutually exclusive with `-All` and `-Hunk`.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- FilePath
ParameterSets:
- Name: Path
  Position: 0
  IsRequired: false
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: true
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

### -Source

The tree to restore content from. When omitted, defaults to HEAD for working-tree restores or the index for `--staged` restores. Equivalent to `git restore --source=<tree>`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Path
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: All
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: InputObject
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

Restores the index (staged changes) rather than the working tree. Equivalent to `git restore --staged`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Path
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: All
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Hunk
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: InputObject
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- wi
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

### System.String[]

One or more file paths, bound via the `-Path` parameter.

### PowerCode.Git.Abstractions.Models.GitDiffHunk[]

Diff hunks piped from `Get-GitDiff -Hunk`, bound via the `-Hunk` parameter.

## OUTPUTS

None. This cmdlet does not produce output.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm` for safety. The confirm impact is set to High because restoring files discards changes that cannot be recovered.

## RELATED LINKS

- [Add-GitItem](Add-GitItem.md)
- [Get-GitDiff](Get-GitDiff.md)
- [Get-GitStatus](Get-GitStatus.md)
