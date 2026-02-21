---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-21-2026
PlatyPS schema version: 2024-05-01
title: Receive-GitBranch
---

# Receive-GitBranch

## SYNOPSIS

Pulls remote changes into the current branch, equivalent to git pull.

## SYNTAX

### Pull (Default)

```
Receive-GitBranch [-MergeStrategy <GitMergeStrategy>] [-Prune] [-AutoStash] [-Tags]
 [-Credential <pscredential>] [-RepoPath <string>] [<CommonParameters>]
```

### Options

```
Receive-GitBranch -Options <GitPullOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Receive-GitBranch cmdlet fetches from the remote and merges into the current branch. Use `-MergeStrategy` to control the merge behavior (Merge, Rebase, FastForward), `-Prune` to remove stale remote-tracking branches, and `-AutoStash` to automatically stash and reapply local changes.

## EXAMPLES

### Example 1 - Pull from remote

Pulls remote changes using the default merge strategy.

```powershell
Receive-GitBranch
```

### Example 2 - Pull with fast-forward and prune

Pulls with fast-forward merge strategy and prunes stale remote-tracking branches.

```powershell
Receive-GitBranch -MergeStrategy FastForward -Prune
```

## PARAMETERS

### -AutoStash

Automatically stashes local changes before pulling and reapplies them afterward. Equivalent to `git pull --autostash`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Pull
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Credential

A PSCredential object for HTTP authentication with the remote.

```yaml
Type: System.Management.Automation.PSCredential
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Pull
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MergeStrategy

Controls the merge behavior. Accepts Merge, Rebase, or FastForward. Defaults to Merge.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitMergeStrategy
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Pull
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

A pre-built GitPullOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitPullOptions
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

### -Prune

Removes remote-tracking branches that no longer exist on the remote.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Pull
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

### -Tags

Fetches all tags from the remote.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Pull
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

None. This cmdlet does not accept pipeline input.

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitCommitInfo

A commit object representing the merge result, or the tip commit after fast-forward.

## NOTES

Progress is reported via Write-Progress during the fetch.

## RELATED LINKS

- [Send-GitBranch](Send-GitBranch.md)
- [Copy-GitRepository](Copy-GitRepository.md)
- [Get-GitLog](Get-GitLog.md)

