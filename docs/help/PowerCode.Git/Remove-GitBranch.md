---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-21-2026
PlatyPS schema version: 2024-05-01
title: Remove-GitBranch
---

# Remove-GitBranch

## SYNOPSIS

Deletes a branch from a git repository, equivalent to git branch -d.

## SYNTAX

### Delete (Default)

```
Remove-GitBranch [-Name] <string> [-Force] [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### Options

```
Remove-GitBranch -Options <GitBranchDeleteOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Remove-GitBranch cmdlet deletes a local branch. By default it performs a safe delete that refuses to remove unmerged branches. Use `-Force` to force-delete an unmerged branch, equivalent to `git branch -D`.

## EXAMPLES

### Example 1 - Delete a merged branch

Deletes a branch that has been fully merged.

```powershell
Remove-GitBranch -Name feature/old-feature
```

### Example 2 - Force-delete an unmerged branch

Force-deletes a branch regardless of merge status.

```powershell
Remove-GitBranch -Name feature/old-feature -Force
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

### -Force

Force-deletes the branch even if it is not fully merged. Equivalent to `git branch -D`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Delete
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

The name of the branch to delete.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Delete
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Options

A pre-built GitBranchDeleteOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitBranchDeleteOptions
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

The Name parameter accepts pipeline input by property name.

## OUTPUTS

### System.Object

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm` for safety. The confirm impact is Medium.

## RELATED LINKS

- [Get-GitBranch](Get-GitBranch.md)
- [New-GitBranch](New-GitBranch.md)
- [Switch-GitBranch](Switch-GitBranch.md)

