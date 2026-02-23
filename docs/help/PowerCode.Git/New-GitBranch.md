---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerGit/blob/{{BranchName}}/docs/help/PowerCode.Git/New-GitBranch.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-21-2026
PlatyPS schema version: 2024-05-01
title: New-GitBranch
---

# New-GitBranch

## SYNOPSIS

Creates a new branch in a git repository, equivalent to git branch.

## SYNTAX

### Create (Default)

```
New-GitBranch [-Name] <string> [[-StartPoint] <string>] [-Track] [-Force] [-Description <string>]
 [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
New-GitBranch -Options <GitBranchCreateOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The New-GitBranch cmdlet creates a new branch in a git repository. By default the branch starts from HEAD. Use `-StartPoint` to branch from a specific commit, tag, or branch. The `-Force` switch overwrites an existing branch with the same name.

## EXAMPLES

### Example 1 - Create a branch

Creates a new branch from HEAD.

```powershell
New-GitBranch -Name feature/my-feature
```

### Example 2 - Create a branch from a start point

Creates a new branch starting from a specific tag.

```powershell
New-GitBranch -Name hotfix/p1 -StartPoint v2.0.0
```

### Example 3 - Create a branch with a description

Creates a new branch and sets a branch description stored in local config.

```powershell
New-GitBranch -Name feature/login -Description 'Implement user login flow'
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

An optional description for the branch. The value is stored in the local repository configuration as `branch.<name>.description`.

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

### -Force

Overwrites an existing branch with the same name. Equivalent to `git branch -f`.

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

The name of the new branch to create.

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

A pre-built GitBranchCreateOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitBranchCreateOptions
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

### -StartPoint

The commit, tag, or branch to start from. Defaults to HEAD when omitted.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Create
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Track

Sets up the new branch to track the remote upstream branch. Equivalent to `git branch --track`.

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

A branch object representing the newly created branch.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm` for safety. Use Switch-GitBranch to check out the new branch.

## RELATED LINKS

- [Get-GitBranch](Get-GitBranch.md)
- [Remove-GitBranch](Remove-GitBranch.md)
- [Switch-GitBranch](Switch-GitBranch.md)
