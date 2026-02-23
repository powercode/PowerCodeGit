---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: ''
Locale: sv-SE
Module Name: PowerCode.Git
ms.date: 02-23-2026
PlatyPS schema version: 2024-05-01
title: Get-GitConfiguration
---

# Get-GitConfiguration

## SYNOPSIS

Reads git configuration values from the repository, user, or system configuration files.

## SYNTAX

### List (Default)

```
Get-GitConfiguration [[-Name] <string>] [-Scope <GitConfigScope>] [-RepoPath <string>]
 [<CommonParameters>]
```

### Options

```
Get-GitConfiguration -Options <GitConfigGetOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

This cmdlet has the following aliases,
  {{Insert list of aliases}}

## DESCRIPTION

The Get-GitConfiguration cmdlet reads configuration values from git configuration files. Git stores configuration in multiple scopes: system (`$(prefix)/etc/gitconfig`), global (`~/.gitconfig`), local (`.git/config`), and worktree (`.git/config.worktree`). By default, values are read from all available scopes with the last value found taking precedence. Use the `-Scope` parameter to restrict reads to a single scope. When `-Name` is omitted, all configuration entries are returned.

## EXAMPLES

### Example 1

{{ Add example description here }}

## PARAMETERS

### -Name

The fully-qualified configuration key to retrieve, such as `user.name` or `core.autocrlf`. The name consists of a section, an optional subsection, and a key separated by dots. When omitted, all configuration entries are returned.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -Options

A pre-built GitConfigGetOptions object that specifies all retrieval parameters. When specified, the Name and Scope parameters are ignored and the values from the options object are used instead.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitConfigGetOptions
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

### -Scope

Limits the read to a specific configuration scope. Valid values are Local (`.git/config`), Global (`~/.gitconfig`), System (`$(prefix)/etc/gitconfig`), and Worktree (`.git/config.worktree`). When unset, git searches all scopes with the last value found taking precedence.

```yaml
Type: System.Nullable`1[PowerCode.Git.Abstractions.Models.GitConfigScope]
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitConfigEntry

An object representing a git configuration entry, containing the fully-qualified key name, value, and the scope from which the entry was read.

## NOTES

This cmdlet wraps `git config list` and `git config get`. Configuration values are read from system, global, local, and worktree scopes in that order, with later values overriding earlier ones.

## RELATED LINKS

- [Set-GitConfiguration](Set-GitConfiguration.md)
- [git-config documentation](https://git-scm.com/docs/git-config)

