---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Set-GitBranch.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-24-2026
PlatyPS schema version: 2024-05-01
title: Set-GitBranch
---

# Set-GitBranch

## SYNOPSIS

Configures an existing local git branch by setting its upstream remote, upstream merge ref, and/or description.

## SYNTAX

### Branch (Default)

```
Set-GitBranch [-Name] <string> [-Remote <string>] [-Upstream <string>] [-Description <string>]
 [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
Set-GitBranch -Options <GitBranchSetOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.


## DESCRIPTION

The `Set-GitBranch` cmdlet configures an existing local branch in the repository.
Use `-Remote` to set the upstream remote for the branch (`branch.<name>.remote`).
Use `-Upstream` to set the upstream merge ref (`branch.<name>.merge`). When
`-Remote` and `-Upstream` are used together, the branch is fully configured for
tracking a remote branch, equivalent to `git branch --set-upstream-to=<remote>/<upstream>`.
Use `-Description` to set the branch description (`branch.<name>.description`).

## EXAMPLES

### Example 1 - Set a branch description

Sets the description for the `feature/login` branch.

```powershell
Set-GitBranch -Name feature/login -Description 'Login feature work'
```

### Example 2 - Set the upstream remote for a branch

Configures the `feature/login` branch to track the `origin` remote.

```powershell
Set-GitBranch -Name feature/login -Remote origin
```

### Example 3 - Set remote and upstream together

Configures the branch to track `origin/main`, equivalent to
`git branch --set-upstream-to=origin/main`.

```powershell
Set-GitBranch -Name feature/login -Remote origin -Upstream main
```

### Example 4 - Set remote and description in a single call

Combines multiple configuration changes into one command.

```powershell
Set-GitBranch -Name feature/login -Remote origin -Description 'Login feature work'
```

## PARAMETERS

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

### -Description

The branch description to store in `branch.<name>.description`. This is a
free-form text field used by `git format-patch --cover-letter` and other tools.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Branch
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

The name of the local branch to configure. The branch must already exist in the
repository.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Branch
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

A pre-built `GitBranchSetOptions` object for full programmatic control over
branch configuration. Mutually exclusive with the individual parameters in
the `Branch` parameter set.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitBranchSetOptions
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

### -Remote

The remote name to set as `branch.<name>.remote`. The remote must exist in the
repository. When combined with `-Upstream`, fully configures the upstream
tracking reference for the branch.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Branch
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

### -Upstream

The upstream branch name to set as `branch.<name>.merge`. A short name like
`main` is stored as `refs/heads/main`. When combined with `-Remote`, fully
configures the upstream tracking reference.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Branch
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

### PowerCode.Git.Abstractions.Models.GitBranchInfo

A branch object with properties such as `Name`, `TipSha`, `TrackedBranchName`,
`AheadBy`, `BehindBy`, and `Description`.

## NOTES

Only local branches can be configured. Attempting to configure a remote-tracking
branch will produce an error.

The `-Remote` parameter validates that the named remote exists in the repository
before applying the change.

## RELATED LINKS

- [Get-GitBranch](Get-GitBranch.md)
- [New-GitBranch](New-GitBranch.md)
- [Remove-GitBranch](Remove-GitBranch.md)
- [git-branch documentation](https://git-scm.com/docs/git-branch)

