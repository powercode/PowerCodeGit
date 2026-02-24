---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Set-GitModuleConfiguration.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-24-2026
PlatyPS schema version: 2024-05-01
title: Set-GitModuleConfiguration
---

# Set-GitModuleConfiguration

## SYNOPSIS

Updates default parameter values for PowerCode.Git cmdlets.

## SYNTAX

### __AllParameterSets

```
Set-GitModuleConfiguration [-LogMaxCount <int>] [-DiffContext <int>]
 [-BranchReferenceBranch <string>] [-BranchIncludeDescription <bool>] [-Reset] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Set-GitModuleConfiguration cmdlet changes one or more default parameter values used by other PowerCode.Git cmdlets. These defaults are held in-process for the lifetime of the module and are not persisted to disk. Use `Get-GitModuleConfiguration` to inspect the current values.

When a default is configured, the corresponding cmdlet uses it automatically unless the user explicitly passes the parameter on the command line.

## EXAMPLES

### Example 1 - Set a default MaxCount for Get-GitLog

Limits Get-GitLog to return at most 50 commits by default.

```powershell
Set-GitModuleConfiguration -LogMaxCount 50
```

### Example 2 - Enable branch descriptions by default

Configures Get-GitBranch to include branch descriptions without needing `-IncludeDescription` each time.

```powershell
Set-GitModuleConfiguration -BranchIncludeDescription $true
```

### Example 3 - Reset all defaults

Clears all configuration values back to their initial state.

```powershell
Set-GitModuleConfiguration -Reset
```

## PARAMETERS

### -BranchIncludeDescription

Gets or sets the default value for `-IncludeDescription` on `Get-GitBranch`. When `$true`, branch descriptions are included by default. Use `$null` to clear the default.

```yaml
Type: System.Nullable`1[System.Boolean]
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

### -BranchReferenceBranch

Gets or sets the default reference branch used by `Get-GitBranch` for ahead/behind comparison. Use `$null` or an empty string to clear.

```yaml
Type: System.String
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

### -DiffContext

Gets or sets the default number of context lines shown by `Get-GitDiff` when `-Context` is not specified. Use `$null` to clear the default.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -LogMaxCount

Gets or sets the default maximum number of commits returned by `Get-GitLog`. Use `0` or `$null` to clear the default.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -Reset

When specified, resets all configuration values to their initial defaults before applying any other parameter values.

```yaml
Type: System.Management.Automation.SwitchParameter
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### System.Object

This cmdlet does not produce output.

## NOTES

Configuration values are held in-process only. They are lost when the PowerShell session ends or the module is removed.

## RELATED LINKS

- [Get-GitModuleConfiguration](Get-GitModuleConfiguration.md)

