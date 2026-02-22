---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Get-GitStatus.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-21-2026
PlatyPS schema version: 2024-05-01
title: Get-GitStatus
---

# Get-GitStatus

## SYNOPSIS

Retrieves the working tree and index status of a git repository, equivalent to git status.

## SYNTAX

### Status (Default)

```
Get-GitStatus [-IncludeIgnored] [-Path <string[]>] [-UntrackedFiles <GitUntrackedFilesMode>]
 [-RepoPath <string>] [<CommonParameters>]
```

### Options

```
Get-GitStatus -Options <GitStatusOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Get-GitStatus cmdlet returns a summary of the working tree and index status. The result includes counts of staged, modified, and untracked files, the current branch name, and detailed file entries.

Use `-IncludeIgnored` to also report files matched by `.gitignore`.

## EXAMPLES

### Example 1 - Get repository status

Gets the current working tree status.

```powershell
Get-GitStatus
```

### Example 2 - Include ignored files

Gets the status including files matched by .gitignore.

```powershell
Get-GitStatus -IncludeIgnored
```

## PARAMETERS

### -IncludeIgnored

Includes files matched by `.gitignore` in the status results.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Status
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

A pre-built GitStatusOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitStatusOptions
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

One or more paths to restrict the status query to.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Status
  Position: Named
  IsRequired: false
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

### -UntrackedFiles

Controls how untracked files are shown. Accepts values that control directory recursion.

```yaml
Type: System.Nullable`1[PowerCode.Git.Abstractions.Models.GitUntrackedFilesMode]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Status
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

### PowerCode.Git.Abstractions.Models.GitStatusResult

A status result object with CurrentBranch, StagedCount, ModifiedCount, UntrackedCount, and file entry collections.

## NOTES

The returned object provides summary counts and detailed per-file status entries.

## RELATED LINKS

- [Add-GitItem](Add-GitItem.md)
- [Get-GitDiff](Get-GitDiff.md)
- [Save-GitCommit](Save-GitCommit.md)
