---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Add-GitItem.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-22-2026
PlatyPS schema version: 2024-05-01
title: Add-GitItem
---

# Add-GitItem

## SYNOPSIS

Stages files in the working tree for the next commit, equivalent to git add.

## SYNTAX

### Path (Default)

```
Add-GitItem [[-Path] <string[]>] [-Force] [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### All

```
Add-GitItem -All [-Force] [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Update

```
Add-GitItem -Update [-Force] [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
Add-GitItem -Options <GitStageOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### Hunk

```
Add-GitItem -Hunk <GitDiffHunk[]> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Add-GitItem cmdlet stages files from the working tree into the index for the next commit. This is the PowerShell equivalent of `git add`.

Use the `-Path` parameter to stage specific files, `-All` to stage every change (new, modified, and deleted), or `-Update` to stage only already-tracked files. The `-Force` switch allows staging files that would otherwise be ignored by `.gitignore` rules.

Use the `-Hunk` parameter to stage individual diff hunks from `Get-GitDiff -Hunk`, enabling selective staging of specific changes within a file.

## EXAMPLES

### Example 1 - Stage a specific file

Stages a single file for the next commit.

```powershell
Add-GitItem -Path newfile.txt
```

### Example 2 - Stage all changes

Stages all new, modified, and deleted files in the repository.

```powershell
Add-GitItem -All
```

### Example 3 - Stage only tracked files

Stages modifications to already-tracked files without adding new untracked files.

```powershell
Add-GitItem -Update
```

### Example 4 - Stage only modified files from pipeline

Pipes status entries from Get-GitStatus through a filter to stage only modified files, binding the FilePath property to the -Path parameter.

```powershell
Get-GitStatus | Select-Object -ExpandProperty Entries | Where-Object Status -EQ Modified | Add-GitItem
```

### Example 5 - Stage only hunks that contain additions

Pipes diff hunks that have added lines to Add-GitItem for selective hunk-level staging.

```powershell
Get-GitDiff -Hunk | Where-Object LinesAdded -gt 0 | Add-GitItem
```

## PARAMETERS

### -All

Stages all changes in the working tree, including new, modified, and deleted files. Equivalent to `git add -A`.

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

### -Force

Allows staging of files that are otherwise ignored by `.gitignore` rules. Equivalent to `git add -f`.

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
- Name: Update
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

One or more diff hunk objects to stage. Accepts pipeline input from `Get-GitDiff -Hunk`. Each hunk is applied to the index via `git apply --cached`, enabling selective staging of individual changes within a file.

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

### -Options

A pre-built GitStageOptions object for full programmatic control. When specified, all other parameters are ignored.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitStageOptions
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

One or more repository-relative file paths to stage. Accepts pipeline input.

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

### -Update

Stages only changes to already-tracked files (modifications and deletions), but does not add new untracked files. Equivalent to `git add -u`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Update
  Position: Named
  IsRequired: true
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

### System.String

An array of file paths passed via `-Path`.

### PowerCode.Git.Abstractions.Models.GitStatusEntry

A status entry whose `FilePath` property binds to `-Path` by property name.

### PowerCode.Git.Abstractions.Models.GitDiffHunk

A diff hunk object from `Get-GitDiff -Hunk`, accepted via the `-Hunk` parameter for selective hunk staging.

## OUTPUTS

### System.Object

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm` for safety.

## RELATED LINKS

- [Get-GitStatus](Get-GitStatus.md)
- [Reset-GitHead](Reset-GitHead.md)
- [Save-GitCommit](Save-GitCommit.md)
