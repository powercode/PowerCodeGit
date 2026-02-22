---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Resume-GitRebase.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-22-2026
PlatyPS schema version: 2024-05-01
title: Resume-GitRebase
---

# Resume-GitRebase

## SYNOPSIS

Resumes a paused rebase after resolving conflicts or skips the current conflicting commit.

## SYNTAX

### Continue (Default)

```
Resume-GitRebase [-Skip] [-RepoPath <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
Resume-GitRebase -Options <GitRebaseContinueOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Resume-GitRebase cmdlet continues a rebase that was paused due to conflicts. By default it runs `git rebase --continue`, which requires that all conflicts have been resolved and the resolved files staged. Use `-Skip` to drop the current conflicting commit and move on to the next one (`git rebase --skip`).

## EXAMPLES

### Example 1 - Continue a rebase after resolving conflicts

Resumes the rebase operation after all conflicts have been resolved and staged.

```powershell
Resume-GitRebase
```

### Example 2 - Skip the current conflicting commit

Drops the current conflicting commit and continues rebasing with the remaining commits.

```powershell
Resume-GitRebase -Skip
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

### -Options

A pre-built GitRebaseContinueOptions object for full programmatic control over the continue/skip decision.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitRebaseContinueOptions
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

### -Skip

Skips the current conflicting commit instead of resuming normally. Equivalent to `git rebase --skip`. When not specified, `git rebase --continue` is used.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Continue
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

### PowerCode.Git.Abstractions.Models.GitRebaseResult

A result object indicating whether the rebase completed successfully or still has conflicts.

## NOTES

This cmdlet supports `-WhatIf` and `-Confirm` for safety.

## RELATED LINKS

- [Start-GitRebase](Start-GitRebase.md)
- [Stop-GitRebase](Stop-GitRebase.md)
