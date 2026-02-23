---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Get-GitWorktree.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-21-2026
PlatyPS schema version: 2024-05-01
title: Get-GitWorktree
---

# Get-GitWorktree

## SYNOPSIS

Lists worktrees in a git repository, equivalent to git worktree list.

## SYNTAX

### List (Default)

```
Get-GitWorktree [-RepoPath <string>] [<CommonParameters>]
```

### Options

```
Get-GitWorktree -Options <GitWorktreeListOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Get-GitWorktree cmdlet lists all worktrees associated with a git repository. The main working tree and any linked worktrees are returned as GitWorktreeInfo objects.

## EXAMPLES

### Example 1 - List worktrees

Lists all worktrees for the current repository.

```powershell
Get-GitWorktree
```

## PARAMETERS

### -Options

A pre-built GitWorktreeListOptions object that specifies all retrieval parameters, including the repository path.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitWorktreeListOptions
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitWorktreeInfo

A worktree object with properties such as Path, Branch, Head, and IsLocked.

## NOTES

The main working tree is always included in the output.

## RELATED LINKS

- [New-GitWorktree](New-GitWorktree.md)
- [Remove-GitWorktree](Remove-GitWorktree.md)
- [Lock-GitWorktree](Lock-GitWorktree.md)
- [Unlock-GitWorktree](Unlock-GitWorktree.md)
