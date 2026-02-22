---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Save-GitCommit.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-22-2026
PlatyPS schema version: 2024-05-01
title: Save-GitCommit
---

# Save-GitCommit

## SYNOPSIS

Creates a commit from the current index, equivalent to git commit.

## SYNTAX

### Commit (Default)

```
Save-GitCommit [[-Message] <string>] [-Amend] [-AllowEmpty] [-All] [-Author <string>]
 [-Date <DateTimeOffset>] [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
Save-GitCommit -Options <GitCommitOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.
## DESCRIPTION

The Save-GitCommit cmdlet creates a new commit from the currently staged changes. Use `-Message` to set the commit message, `-Amend` to modify the previous commit, `-All` to automatically stage tracked modified files before committing, and `-AllowEmpty` to create a commit with no changes.

## EXAMPLES

### Example 1 - Create a commit

Creates a commit with a message.

```powershell
Save-GitCommit -Message 'Add new feature'
```

### Example 2 - Amend the previous commit

Amends the most recent commit.

```powershell
Save-GitCommit -Amend
```

### Example 3 - Commit all tracked changes

Stages and commits all tracked file modifications in one step.

```powershell
Save-GitCommit -All -Message 'Track all changes'
```

## PARAMETERS

### -All

Automatically stages all tracked modified files before committing. Equivalent to `git commit -a`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Commit
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AllowEmpty

Allows creating a commit with no staged changes.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Commit
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Amend

Amends the previous commit instead of creating a new one.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Commit
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Author

The author in 'Name <email>' format. When omitted, the git config identity is used.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Commit
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

### -Date

Overrides the author and committer date. When omitted, the current time is used.

```yaml
Type: System.Nullable`1[System.DateTimeOffset]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Commit
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Message

The commit message text.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- m
ParameterSets:
- Name: Commit
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

A pre-built GitCommitOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitCommitOptions
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

None. This cmdlet does not accept pipeline input.

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitCommitInfo

A commit object representing the newly created commit.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm` for safety.

## RELATED LINKS

- [Add-GitItem](Add-GitItem.md)
- [Get-GitLog](Get-GitLog.md)
- [Reset-GitHead](Reset-GitHead.md)
