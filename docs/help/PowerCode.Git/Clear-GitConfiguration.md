---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Clear-GitConfiguration.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-25-2026
PlatyPS schema version: 2024-05-01
title: Clear-GitConfiguration
---

# Clear-GitConfiguration

## SYNOPSIS

Removes one or more git configuration keys, equivalent to `git config --unset`.

## SYNTAX

### __AllParameterSets

```
Clear-GitConfiguration [-Name] <string[]> [-Scope <GitConfigScope>] [-RepoPath <string>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

`Clear-GitConfiguration` removes one or more git configuration keys from the active configuration
file. It is equivalent to running `git config --unset <name>` for each key.

By default the key is removed from the repository-local file (`.git/config`). Use `-Scope` to
target a different level: `Global` (`~/.gitconfig`), `System` (`/etc/gitconfig`), or
`Worktree` (`.git/config.worktree`).

The cmdlet supports `-WhatIf` and `-Confirm`. Use `-WhatIf` to preview which keys would be
removed without making any changes. Multiple keys can be removed in one call by passing an array
to `-Name`; each key is processed independently.

## EXAMPLES

### Example 1 — Remove a repository-local key

```powershell
Clear-GitConfiguration -Name user.name
```

Removes the `user.name` key from the repository-local configuration file (`.git/config`) in
the current directory.

### Example 2 — Remove a key from the global configuration

```powershell
Clear-GitConfiguration -Name core.autocrlf -Scope Global
```

Removes `core.autocrlf` from the user-level configuration file (`~/.gitconfig`).

### Example 3 — Remove multiple keys at once

```powershell
Clear-GitConfiguration -Name user.name, user.email
```

Removes both `user.name` and `user.email` from the local configuration in a single call.
Each key is processed independently.

### Example 4 — Preview with -WhatIf

```powershell
Clear-GitConfiguration -Name user.name -WhatIf
```

Displays what would be removed without making any changes.

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

One or more fully-qualified git configuration keys to remove (e.g. `user.name`,
`core.autocrlf`). Accepts an array; each key is processed independently.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RepoPath

Path to the git repository. When omitted the current PowerShell working directory is used.

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

The configuration scope from which to remove the key. When omitted, git's default (local
repository scope) is used.

| Value | File |
|-------|------|
| `Local` | `.git/config` (default) |
| `Global` | `~/.gitconfig` |
| `System` | `/etc/gitconfig` |
| `Worktree` | `.git/config.worktree` |

```yaml
Type: System.Nullable`1[PowerCode.Git.Abstractions.Models.GitConfigScope]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
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

None. This cmdlet does not accept pipeline input.

## OUTPUTS

None. This cmdlet does not produce output.

## NOTES

Removing a key that does not exist is a silent no-op — no error is produced. This matches the
behaviour of `git config --unset` when the key is absent.

## RELATED LINKS

- [Set-GitConfiguration](Set-GitConfiguration.md)
- [Get-GitConfiguration](Get-GitConfiguration.md)

