---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/New-GitRemote.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-25-2026
PlatyPS schema version: 2024-05-01
title: New-GitRemote
---

# New-GitRemote

## SYNOPSIS

Adds a new remote to a git repository, equivalent to git remote add.

## SYNTAX

### Remote (Default)

```
New-GitRemote [-Name] <string> [-Url] <string> [-PushUrl <string>] [-RepoPath <string>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
New-GitRemote -Options <GitRemoteAddOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The New-GitRemote cmdlet registers a new remote in the git repository. A fetch URL is
required. Optionally, a separate push URL can be specified via `-PushUrl`.

This is equivalent to running `git remote add <name> <url>`.

## EXAMPLES

### Example 1 - Add a remote

Adds a remote named `upstream` pointing at the given URL.

```powershell
New-GitRemote -Name upstream -Url https://github.com/powercode/PowerGit.git
```

### Example 2 - Add a remote with a distinct push URL

Adds a remote with separate fetch and push URLs.

```powershell
New-GitRemote -Name origin -Url https://github.com/org/repo.git -PushUrl git@github.com:org/repo.git
```

## PARAMETERS

### -Name

The name for the new remote.

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
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Url

The fetch URL for the new remote.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Remote
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -PushUrl

An optional push URL that differs from the fetch URL.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Remote
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

A pre-built GitRemoteAddOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitRemoteAddOptions
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

### None

This cmdlet does not accept pipeline input.

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitRemoteInfo

Returns a `GitRemoteInfo` object representing the newly added remote.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm`. An error is emitted when a remote with the
same name already exists.

## RELATED LINKS

- [Get-GitRemote](Get-GitRemote.md)
- [Set-GitRemote](Set-GitRemote.md)
- [Remove-GitRemote](Remove-GitRemote.md)
