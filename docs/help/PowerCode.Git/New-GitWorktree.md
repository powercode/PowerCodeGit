---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerGit/blob/{{BranchName}}/docs/help/PowerCode.Git/New-GitWorktree.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-21-2026
PlatyPS schema version: 2024-05-01
title: New-GitWorktree
---

# New-GitWorktree

## SYNOPSIS

Creates a new worktree in a git repository, equivalent to git worktree add.

## SYNTAX

### Create (Default)

```
New-GitWorktree [-Name] <string> [-Path] <string> [-Branch <string>] [-Locked] [-RepoPath <string>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
New-GitWorktree -Options <GitWorktreeAddOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The New-GitWorktree cmdlet creates a new linked worktree for a git repository, allowing you to work on multiple branches simultaneously in separate directories. Use `-Branch` to check out a specific branch in the new worktree.

## EXAMPLES

### Example 1 - Create a worktree

Creates a new worktree at the specified path. A new branch named `feature` is created automatically and checked out in the worktree.

```powershell
New-GitWorktree -Name feature -Path ../feature-worktree
```

### Example 2 - Create a worktree for an existing branch

Checks out an existing branch into a new worktree. The `-Name` must differ from `-Branch` because the worktree internally creates a tracking reference with the given name.

```powershell
New-GitWorktree -Name hotfix-wt -Path ../hotfix-worktree -Branch hotfix/p1
```

## PARAMETERS

### -Branch

The branch or committish to check out in the new worktree. When omitted, the current HEAD is used.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Create
  Position: Named
  IsRequired: false
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

### -Locked

Creates the worktree in a locked state to prevent pruning.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Create
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

The name for the new worktree.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Create
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Options

A pre-built GitWorktreeAddOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitWorktreeAddOptions
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

The filesystem path where the worktree will be created.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Create
  Position: 1
  IsRequired: true
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

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitWorktreeInfo

A worktree object representing the newly created worktree.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm` for safety. Use Remove-GitWorktree to delete a linked worktree.

## RELATED LINKS

- [Get-GitWorktree](Get-GitWorktree.md)
- [Remove-GitWorktree](Remove-GitWorktree.md)
- [Lock-GitWorktree](Lock-GitWorktree.md)
