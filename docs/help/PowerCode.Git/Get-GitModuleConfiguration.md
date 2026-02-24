---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Get-GitModuleConfiguration.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-24-2026
PlatyPS schema version: 2024-05-01
title: Get-GitModuleConfiguration
---

# Get-GitModuleConfiguration

## SYNOPSIS

Returns the current PowerCode.Git module configuration.

## SYNTAX

### __AllParameterSets

```
Get-GitModuleConfiguration [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Get-GitModuleConfiguration cmdlet returns the current in-process module configuration object. This object contains default parameter values used by other PowerCode.Git cmdlets (such as `LogMaxCount`, `DiffContext`, `BranchReferenceBranch`, and `BranchIncludeDescription`).

Use `Set-GitModuleConfiguration` to change these values.

## EXAMPLES

### Example 1 - View current configuration

Displays the current module configuration with all default values.

```powershell
Get-GitModuleConfiguration
```

### Example 2 - Check a specific setting

Reads the current default LogMaxCount value.

```powershell
(Get-GitModuleConfiguration).LogMaxCount
```

## PARAMETERS

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### PowerCode.Git.ModuleConfiguration

The module configuration object with properties: LogMaxCount, DiffContext, BranchReferenceBranch, and BranchIncludeDescription.

## NOTES

Configuration values are held in-process only. They are lost when the PowerShell session ends or the module is removed.

## RELATED LINKS

- [Set-GitModuleConfiguration](Set-GitModuleConfiguration.md)

