---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Receive-GitBranch.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-21-2026
PlatyPS schema version: 2024-05-01
title: Receive-GitBranch
---

# Receive-GitBranch

## SYNOPSIS

Pulls remote changes into the current branch, or creates/updates local tracking branches from piped remote-tracking branches.

## SYNTAX

### Pull (Default)

```
Receive-GitBranch [-MergeStrategy <GitMergeStrategy>] [-Prune] [-AutoStash] [-Tags]
 [-Credential <pscredential>] [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Pipeline

```
Receive-GitBranch -InputBranch <GitBranchInfo> [-Action <ReceiveBranchAction>]
 [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
Receive-GitBranch -Options <GitPullOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Receive-GitBranch cmdlet has two distinct modes:

**Pull mode (default):** Fetches from the remote and merges into the current branch. Use `-MergeStrategy` to control the merge behavior (Merge, Rebase, FastForward), `-Prune` to remove stale remote-tracking branches, and `-AutoStash` to automatically stash and reapply local changes.

**Pipeline mode:** When remote-tracking branches are piped in (e.g. from `Get-GitBranch -Remote`), Receive-GitBranch creates and/or fast-forwards the corresponding local tracking branches. The `-Action` parameter controls behaviour:
- `Create` (default) — creates a local tracking branch for each remote branch that does not already exist locally. Existing local branches are skipped.
- `CreateOrUpdate` — creates missing local branches and fast-forwards existing ones.
- `UpdateOnly` — only fast-forwards existing local branches. Remote branches without a local counterpart are skipped.

Each branch operation is guarded by `ShouldProcess`, so `-WhatIf` and `-Confirm` work per branch.

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

### Example 3 - Create local tracking branches from all remote branches

Pipes all remote-tracking branches and creates a local tracking branch for each one that does not already exist locally.

```powershell
Get-GitBranch -Remote | Receive-GitBranch -Action Create
```

### Example 4 - Sync a subset of remote branches

Creates or fast-forwards local tracking branches for all `feature/*` remote branches.

```powershell
Get-GitBranch -Remote -Include 'origin/feature/*' | Receive-GitBranch -Action CreateOrUpdate
```

### Example 5 - Preview what would be created (WhatIf)

Shows which local branches would be created without performing the operations.

```powershell
Get-GitBranch -Remote | Receive-GitBranch -Action Create -WhatIf
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

### -Action

Controls how piped remote-tracking branches are handled. Only applies to the Pipeline parameter set.

- `Create` (default) — creates a local tracking branch for each remote branch that does not already have a local counterpart. Existing local branches are skipped.
- `CreateOrUpdate` — creates missing local branches and fast-forwards existing ones to match the remote-tracking ref.
- `UpdateOnly` — only fast-forwards existing local branches. Remote branches with no local counterpart are skipped.

```yaml
Type: PowerCode.Git.Abstractions.Models.ReceiveBranchAction
DefaultValue: Create
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Pipeline
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues:
- Create
- CreateOrUpdate
- UpdateOnly
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

### -InputBranch

A remote-tracking branch object received from the pipeline, typically from `Get-GitBranch -Remote`. The `LocalName` property is used to derive the local branch name (e.g. `origin/main` → `main`). Writing a local (non-remote) branch to this parameter produces a non-terminating error.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitBranchInfo
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Pipeline
  Position: Named
  IsRequired: true
  ValueFromPipeline: true
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

### PowerCode.Git.Abstractions.Models.GitBranchInfo

A remote-tracking branch from `Get-GitBranch -Remote`. Used by the Pipeline parameter set.

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitCommitInfo

Returned by the Pull and Options parameter sets. Represents the merge result or tip commit after fast-forward.

### PowerCode.Git.Abstractions.Models.GitBranchInfo

Returned by the Pipeline parameter set for each branch that was created or updated.

## NOTES

Progress is reported via Write-Progress during the fetch.

## RELATED LINKS

- [Send-GitBranch](Send-GitBranch.md)
- [Copy-GitRepository](Copy-GitRepository.md)
- [Get-GitLog](Get-GitLog.md)
