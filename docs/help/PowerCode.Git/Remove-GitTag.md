---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Remove-GitTag.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-25-2026
PlatyPS schema version: 2024-05-01
title: Remove-GitTag
---

# Remove-GitTag

## SYNOPSIS

Deletes a tag from a git repository, equivalent to git tag -d.

## SYNTAX

### Delete (Default)

```
Remove-GitTag [-Name] <string> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### Options

```
Remove-GitTag -Options <GitTagDeleteOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Remove-GitTag cmdlet deletes a local tag from the repository. This is equivalent to running `git tag -d <name>`.

## EXAMPLES

### Example 1 - Delete a tag

Deletes a tag by name.

```powershell
Remove-GitTag -Name v1.0.0
```

### Example 2 - Delete a tag piped from Get-GitTag

Pipes tag objects from Get-GitTag to Remove-GitTag to delete matching tags.

```powershell
Get-GitTag -Include 'v0.*' | Remove-GitTag
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

### -Name

The name of the tag to delete.

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

A pre-built GitTagDeleteOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitTagDeleteOptions
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

- [Get-GitTag](Get-GitTag.md)
- [Set-GitTag](Set-GitTag.md)
