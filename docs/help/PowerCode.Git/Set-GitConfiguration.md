---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: ''
Locale: sv-SE
Module Name: PowerCode.Git
ms.date: 02-23-2026
PlatyPS schema version: 2024-05-01
title: Set-GitConfiguration
---

# Set-GitConfiguration

## SYNOPSIS

Sets a git configuration value in the repository, user, or system configuration files.

## SYNTAX

### Config (Default)

```
Set-GitConfiguration [-Name] <string> [-Value] <string> [-Scope <GitConfigScope>]
 [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
Set-GitConfiguration -Options <GitConfigSetOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

This cmdlet has the following aliases,
  {{Insert list of aliases}}

## DESCRIPTION

The Set-GitConfiguration cmdlet writes a value to a git configuration file. By default, the value is written to the repository-local configuration file (`.git/config`). Use the `-Scope` parameter to write to a different scope such as Global (`~/.gitconfig`), System (`$(prefix)/etc/gitconfig`), or Worktree (`.git/config.worktree`). The cmdlet supports `-WhatIf` and `-Confirm` for safety.

## EXAMPLES

### Example 1

{{ Add example description here }}

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

### -Name

The fully-qualified configuration key to set, such as `user.name` or `core.autocrlf`. The name consists of a section, an optional subsection, and a key separated by dots.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Config
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

A pre-built GitConfigSetOptions object that specifies all parameters for the set operation. When specified, the Name, Value, and Scope parameters are ignored and the values from the options object are used instead.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitConfigSetOptions
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

The configuration scope to write into. Valid values are Local (`.git/config`), Global (`~/.gitconfig`), System (`$(prefix)/etc/gitconfig`), and Worktree (`.git/config.worktree`). Defaults to Local when unset.

```yaml
Type: System.Nullable`1[PowerCode.Git.Abstractions.Models.GitConfigScope]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Config
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Value

The value to assign to the configuration key.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Config
  Position: 1
  IsRequired: true
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

### PowerCode.Git.Abstractions.Models.GitConfigEntry

An object representing the git configuration entry that was set, containing the fully-qualified key name, value, and scope.

## NOTES

This cmdlet wraps `git config set`. The confirm impact is set to Medium.

## RELATED LINKS

- [Get-GitConfiguration](Get-GitConfiguration.md)
- [git-config documentation](https://git-scm.com/docs/git-config)

