---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Reset-GitHead.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-22-2026
PlatyPS schema version: 2024-05-01
title: Reset-GitHead
---

# Reset-GitHead

## SYNOPSIS

Resets the current HEAD to a specified state, equivalent to git reset.

## SYNTAX

### Mixed (Default)

```
Reset-GitHead [[-Revision] <string>] [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Soft

```
Reset-GitHead [[-Revision] <string>] -Soft [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### Hard

```
Reset-GitHead [[-Revision] <string>] -Hard [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

### Paths

```
Reset-GitHead -Path <string[]> [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
Reset-GitHead -Options <GitResetOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Reset-GitHead cmdlet resets the current HEAD to a specified state. The default mode is mixed, which resets the index but not the working tree. Use `-Soft` to keep both index and working tree, or `-Hard` to discard all changes. Use the `-Path` parameter to unstage specific files without moving HEAD.

## EXAMPLES

### Example 1 - Mixed reset

Resets the index to HEAD, keeping working tree changes.

```powershell
Reset-GitHead
```

### Example 2 - Hard reset to previous commit

Discards all changes and resets to the previous commit.

```powershell
Reset-GitHead -Revision HEAD~1 -Hard
```

### Example 3 - Unstage a file

Removes a file from the index without modifying the working tree.

```powershell
Reset-GitHead -Path file.txt
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

### -Hard

Resets the index and working tree. All changes since the target revision are discarded. Equivalent to `git reset --hard`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Hard
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Options

A pre-built GitResetOptions object for full programmatic control. When specified, all other parameters are ignored.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitResetOptions
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

### -Path

One or more file paths to unstage. When specified, HEAD is not moved and mode parameters are ignored.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Paths
  Position: Named
  IsRequired: true
  ValueFromPipeline: true
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

### -Revision

The revision to reset to. Defaults to HEAD when omitted.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Mixed
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Soft
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Hard
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Soft

Moves HEAD only. The index and working tree are left unchanged. Equivalent to `git reset --soft`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Soft
  Position: Named
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

### System.String[]

An array of repository-relative file paths bound via the `-Path` parameter.

## OUTPUTS

### System.Object

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm` for safety. The confirm impact is High due to the potential for data loss with -Hard.

## RELATED LINKS

- [Add-GitItem](Add-GitItem.md)
- [Get-GitStatus](Get-GitStatus.md)
- [Save-GitCommit](Save-GitCommit.md)
