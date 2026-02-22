---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Get-GitBranch.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-22-2026
PlatyPS schema version: 2024-05-01
title: Get-GitBranch
---

# Get-GitBranch

## SYNOPSIS

Lists branches in a git repository, equivalent to git branch.

## SYNTAX

### List (Default)

```
Get-GitBranch [[-Include] <string[]>] [-Remote] [-All] [-Pattern <string>] [-Contains <string>]
 [-Merged <string>] [-NoMerged <string>] [-Exclude <string[]>] [-RepoPath <string>]
 [<CommonParameters>]
```

### Options

```
Get-GitBranch -Options <GitBranchListOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

This cmdlet has the following aliases,
  None.

## DESCRIPTION

The Get-GitBranch cmdlet lists branches in a git repository. By default, only local branches are shown. Use `-Remote` to show only remote-tracking branches, or `-All` to show both.

Each branch is returned as a GitBranchInfo object with properties such as Name, TipSha, IsHead, and IsRemote.

## EXAMPLES

### Example 1 - List local branches

Lists all local branches in the current repository.

```powershell
Get-GitBranch
```

### Example 2 - List remote branches

Lists only remote-tracking branches.

```powershell
Get-GitBranch -Remote
```

### Example 3 - List all branches

Lists both local and remote-tracking branches.

```powershell
Get-GitBranch -All
```

### Example 4 - Include only feature and bugfix branches

Returns only branches whose names match any of the specified wildcard patterns.

```powershell
Get-GitBranch -Include 'feature/*', 'bugfix/*'
```

### Example 5 - Exclude temporary branches

Returns all branches except those matching the exclude pattern.

```powershell
Get-GitBranch -Exclude 'temp/*'
```

### Example 6 - Combine Include and Exclude

Includes feature branches but excludes the temporary ones.

```powershell
Get-GitBranch -Include 'feature/*' -Exclude 'feature/temp'
```

## PARAMETERS

### -All

Lists both local and remote-tracking branches. Equivalent to `git branch -a`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- a
ParameterSets:
- Name: List
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Contains

Filters the output to only branches containing the specified commit.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: List
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Exclude

Wildcard patterns used to exclude branches by name. Branches matching any pattern are removed from the result. Applied after `-Include`.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: List
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Include

Wildcard patterns used to include branches by name. Only branches matching at least one pattern are returned. When not specified, all branches are included.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: List
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Merged

Filters the output to only branches merged into the specified commit.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: List
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -NoMerged

Filters the output to only branches NOT merged into the specified commit.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: List
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

A pre-built GitBranchListOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitBranchListOptions
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

### -Pattern

A glob pattern to filter branch names. Equivalent to `git branch -l <pattern>`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: List
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Remote

Lists only remote-tracking branches. Equivalent to `git branch -r`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: List
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

None. This cmdlet does not accept pipeline input.

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitBranchInfo

A branch object with properties including Name, TipSha, TipShortSha, IsHead, and IsRemote.

## NOTES

Use New-GitBranch to create a branch and Remove-GitBranch to delete one.

## RELATED LINKS

- [New-GitBranch](New-GitBranch.md)
- [Remove-GitBranch](Remove-GitBranch.md)
- [Switch-GitBranch](Switch-GitBranch.md)
