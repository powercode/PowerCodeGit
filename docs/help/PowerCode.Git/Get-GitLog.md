---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Get-GitLog.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-21-2026
PlatyPS schema version: 2024-05-01
title: Get-GitLog
---

# Get-GitLog

## SYNOPSIS

Retrieves commit history from a git repository, equivalent to git log.

## SYNTAX

### Log (Default)

```
Get-GitLog [[-Branch] <string>] [[-Path] <string[]>] [-AllBranches] [-MaxCount <int>]
 [-Author <string>] [-Since <datetime>] [-Until <datetime>] [-MessagePattern <string>]
 [-FirstParent] [-NoMerges] [-RepoPath <string>] [<CommonParameters>]
```

### Options

```
Get-GitLog -Options <GitLogOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Get-GitLog cmdlet retrieves commit history from a git repository. By default it walks the current branch. Use `-MaxCount` to limit results, `-Author` to filter by author, `-Since`/`-Until` for date ranges, and `-Path` to restrict to commits touching specific files.

Each commit is returned as a GitCommitInfo object with properties such as Sha, ShortSha, AuthorName, AuthorDate, MessageShort, and Message.

## EXAMPLES

### Example 1 - Get recent commits

Gets the last 5 commits from the current branch.

```powershell
Get-GitLog -MaxCount 5
```

### Example 2 - Filter by author

Gets commits authored by a specific person.

```powershell
Get-GitLog -Author 'Alice'
```

## PARAMETERS

### -AllBranches

Includes commits from all local branches, not just the current one.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Log
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

Filters commits to those whose author name or email matches the specified string.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Log
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Branch

The branch name to traverse for log entries. When omitted, the current HEAD branch is used.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Log
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -FirstParent

Follows only the first parent of merge commits, producing a linear history. Equivalent to `git log --first-parent`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Log
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MaxCount

Limits the number of commits returned. Must be 1 or greater.

```yaml
Type: System.Int32
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Log
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MessagePattern

Filters commits to those whose commit message matches the specified pattern.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Log
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -NoMerges

Excludes merge commits from the output. Equivalent to `git log --no-merges`.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Log
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

A pre-built GitLogOptions object for full programmatic control. When specified, all other parameters are ignored.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitLogOptions
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

One or more pathspec patterns. Only commits that touched files matching these patterns are returned (equivalent to `git log -- <pathspec>…`).
Supports git-style glob patterns: `*` matches within a single directory, `**` matches across directory boundaries, and `?` matches a single character.
For example, `**/*.cs` matches all C# files and `src/` matches everything under `src/`.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: Log
  Position: 2
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

### -Since

Returns only commits authored on or after this date.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Log
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Until

Returns only commits authored on or before this date.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Log
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

### PowerCode.Git.Abstractions.Models.GitCommitInfo

A commit object with Sha, ShortSha, AuthorName, AuthorEmail, AuthorDate, CommitterName, CommitterEmail, CommitDate, MessageShort, Message, and ParentShas properties.

## NOTES

Commits are returned in reverse chronological order (newest first).

## RELATED LINKS

- [Save-GitCommit](Save-GitCommit.md)
- [Get-GitDiff](Get-GitDiff.md)
- [Get-GitBranch](Get-GitBranch.md)
