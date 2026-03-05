---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Set-GitRemote.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-25-2026
PlatyPS schema version: 2024-05-01
title: Set-GitRemote
---

# Set-GitRemote

## SYNOPSIS

Renames a remote or updates its URL, equivalent to git remote rename / git remote set-url.

## SYNTAX

### Remote (Default)

```
Set-GitRemote [-Name] <string> [-Url <string>] [-PushUrl <string>] [-NewName <string>]
 [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
Set-GitRemote -Options <GitRemoteUpdateOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Set-GitRemote cmdlet modifies an existing remote. It can:

- Rename a remote with `-NewName` (equivalent to `git remote rename <old> <new>`)
- Update the fetch URL with `-Url` (equivalent to `git remote set-url <name> <url>`)
- Update the push URL with `-PushUrl` (equivalent to `git remote set-url --push <name> <url>`)

Multiple operations can be combined in one call. When `-NewName` is combined with a URL
change the rename is performed first.

## EXAMPLES

### Example 1 - Update the fetch URL of a remote

Changes the fetch URL of the `origin` remote.

```powershell
Set-GitRemote -Name origin -Url https://github.com/my-fork/repo.git
```

### Example 2 - Rename a remote

Renames `origin` to `upstream`.

```powershell
Set-GitRemote -Name origin -NewName upstream
```

### Example 3 - Set a distinct push URL

Configures a separate push URL so that fetches and pushes use different endpoints.

```powershell
Set-GitRemote -Name origin -PushUrl git@github.com:org/repo.git
```

### Example 4 - Rename and update URL in one call

Renames `origin` to `upstream` and simultaneously changes the fetch URL.

```powershell
Set-GitRemote -Name origin -NewName upstream -Url https://github.com/org/repo.git
```

## PARAMETERS

### -Name

The current name of the remote to modify.

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

The new fetch URL for the remote.

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

### -PushUrl

The new push URL for the remote. When omitted the push URL inherits the fetch URL.

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

### -NewName

The new name for the remote. When combined with URL parameters, the rename is applied first.

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

A pre-built GitRemoteUpdateOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitRemoteUpdateOptions
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

Returns the updated `GitRemoteInfo` object after all modifications have been applied.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm`. At least one of `-Url`, `-PushUrl`, or
`-NewName` must be specified; supplying none of them is an error. When `-NewName` is
combined with URL changes the rename is always performed first.

## RELATED LINKS

- [Get-GitRemote](Get-GitRemote.md)
- [New-GitRemote](New-GitRemote.md)
- [Remove-GitRemote](Remove-GitRemote.md)
