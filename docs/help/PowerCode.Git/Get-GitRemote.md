---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Get-GitRemote.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-25-2026
PlatyPS schema version: 2024-05-01
title: Get-GitRemote
---

# Get-GitRemote

## SYNOPSIS

Lists remotes configured in a git repository, equivalent to git remote -v.

## SYNTAX

### Remote (Default)

```
Get-GitRemote [[-Name] <string>] [-RepoPath <string>] [<CommonParameters>]
```

### Options

```
Get-GitRemote -Options <GitRemoteListOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Get-GitRemote cmdlet returns one or more `GitRemoteInfo` objects representing the
remotes configured in a git repository. When no `-Name` is specified, all remotes are
returned. When `-Name` is provided, only the matching remote is returned (or nothing if
the name is not found).

This is equivalent to running `git remote -v`.

## EXAMPLES

### Example 1 - List all remotes

Returns every remote configured in the current repository.

```powershell
Get-GitRemote
```

### Example 2 - Get a specific remote by name

Returns the remote named `origin`, or nothing if it does not exist.

```powershell
Get-GitRemote -Name origin
```

### Example 3 - Get a remote using positional argument

Returns the remote named `upstream`.

```powershell
Get-GitRemote upstream
```

## PARAMETERS

### -Name

The name of the remote to retrieve. When omitted, all remotes are returned.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Remote
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Options

A pre-built GitRemoteListOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitRemoteListOptions
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

### None

This cmdlet does not accept pipeline input.

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitRemoteInfo

Each remote is returned as a `GitRemoteInfo` object with `Name`, `FetchUrl`, and `PushUrl`
properties.

## NOTES

When `-Name` refers to a remote that does not exist the cmdlet returns no output
and does not emit an error.

## RELATED LINKS

- [New-GitRemote](New-GitRemote.md)
- [Set-GitRemote](Set-GitRemote.md)
- [Remove-GitRemote](Remove-GitRemote.md)
