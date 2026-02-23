---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Start-GitRebase.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-22-2026
PlatyPS schema version: 2024-05-01
title: Start-GitRebase
---

# Start-GitRebase

## SYNOPSIS

Starts a rebase operation, replaying commits from the current branch on top of the specified upstream branch.

## SYNTAX

### Rebase (Default)

```
Start-GitRebase [-Upstream] <string> [-Onto <string>] [-AutoStash] [-RepoPath <string>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

### Interactive

```
Start-GitRebase [-Upstream] <string> -Interactive [-AutoSquash] [-Exec <string>] [-RebaseMerges]
 [-UpdateRefs] [-Onto <string>] [-AutoStash] [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### Options

```
Start-GitRebase -Options <GitRebaseOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Start-GitRebase cmdlet replays commits from the current branch on top of the specified upstream branch, equivalent to `git rebase <upstream>`. Use `-Interactive` for an interactive rebase session, `-Onto` for a three-way rebase, and `-AutoStash` to automatically stash and restore uncommitted changes. Accepts pipeline input from `Get-GitBranch`, but stops with a terminating error if more than one branch is received.

## EXAMPLES

### Example 1 - Rebase the current branch onto main

Replays the current branch's commits on top of the main branch.

```powershell
Start-GitRebase -Upstream main
```

### Example 2 - Rebase using pipeline input from Get-GitBranch

Pipes a single branch from `Get-GitBranch` as the upstream target.

```powershell
Get-GitBranch -Pattern main | Start-GitRebase
```

### Example 3 - Rebase with autostash

Automatically stashes uncommitted changes before rebasing and restores them afterwards.

```powershell
Start-GitRebase -Upstream main -AutoStash
```

### Example 4 - Interactive rebase with autosquash

Automatically squashes `fixup!` and `squash!` commits into their target commits. Commit messages must follow the naming convention `fixup! <original message>`.

```powershell
Start-GitRebase -Upstream main -Interactive -AutoSquash
```

### Example 5 - Interactive rebase with exec

Runs `dotnet test` after each replayed commit. The rebase aborts automatically if any exec step exits non-zero.

```powershell
Start-GitRebase -Upstream main -Interactive -Exec 'dotnet test'
```

## PARAMETERS

### -AutoSquash

Automatically applies `fixup!` and `squash!` commit ordering when populating the interactive rebase todo list. Equivalent to `git rebase -i --autosquash`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Interactive
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AutoStash

Automatically stashes uncommitted changes before the rebase and restores them afterwards. Equivalent to `git rebase --autostash`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Rebase
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Interactive
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

### -Exec

A shell command to execute after each rebased commit. An `exec` line is inserted after every `pick` line in the todo list. Equivalent to `git rebase -i --exec <cmd>`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Interactive
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Interactive

Opens an interactive rebase session. Equivalent to `git rebase -i`. Activates the `Interactive` parameter set, which unlocks `-AutoSquash`, `-Exec`, `-RebaseMerges`, and `-UpdateRefs`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Interactive
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Onto

An optional target ref for a three-way rebase. Equivalent to `git rebase --onto <Onto> <Upstream>`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Rebase
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Interactive
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

A pre-built GitRebaseOptions object for full programmatic control over the rebase.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitRebaseOptions
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

### -RebaseMerges

Recreates merge commits during the rebase rather than linearising history. Equivalent to `git rebase --rebase-merges`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Interactive
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

### -UpdateRefs

Automatically updates any branch refs that point to commits being rebased. Useful when working with stacked branches. Equivalent to `git rebase --update-refs`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Interactive
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Upstream

The name of the upstream branch to rebase the current branch onto. Binds from the `Name` property when pipeline input comes from `Get-GitBranch`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Name
ParameterSets:
- Name: Rebase
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
- Name: Interactive
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
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

The upstream branch name, bound via the `Name` property from `Get-GitBranch` pipeline input.

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitRebaseResult

A result object indicating whether the rebase completed successfully or encountered conflicts.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm` for safety. Only one branch may be piped from `Get-GitBranch`; piping multiple branches produces a terminating error.

## RELATED LINKS

- [Resume-GitRebase](Resume-GitRebase.md)
- [Stop-GitRebase](Stop-GitRebase.md)
- [Get-GitBranch](Get-GitBranch.md)
