---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Remove-GitRemote.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-25-2026
PlatyPS schema version: 2024-05-01
title: Remove-GitRemote
---

# Remove-GitRemote

## SYNOPSIS

Deletes a remote from a git repository, equivalent to git remote remove.

## SYNTAX

### Remote (Default)

```
Remove-GitRemote [-Name] <string> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### Options

```
Remove-GitRemote -Options <GitRemoteRemoveOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Remove-GitRemote cmdlet removes an existing remote from the repository, deleting all
associated tracking branches and configuration. This is equivalent to running
`git remote remove <name>`.

## EXAMPLES

### Example 1 - Remove a remote by name

Removes the remote named `upstream` from the current repository.

```powershell
Remove-GitRemote -Name upstream
```

### Example 2 - Remove a remote piped from Get-GitRemote

Pipes the `origin` remote from Get-GitRemote and removes it.

```powershell
Get-GitRemote -Name origin | Remove-GitRemote
```

## PARAMETERS

### -Name

The name of the remote to remove.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Remote
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

A pre-built GitRemoteRemoveOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitRemoteRemoveOptions
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

The `-Name` parameter accepts pipeline input by property name, allowing `GitRemoteInfo`
objects from `Get-GitRemote` to be piped directly.

## OUTPUTS

### System.Object

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm` for safety. The confirm impact is Medium.
All associated tracking branches and remote-tracking configuration are removed along with
the remote.

## RELATED LINKS

- [Get-GitRemote](Get-GitRemote.md)
- [New-GitRemote](New-GitRemote.md)
- [Set-GitRemote](Set-GitRemote.md)
