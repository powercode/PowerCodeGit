---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Get-GitCommitFile.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-22-2026
PlatyPS schema version: 2024-05-01
title: Get-GitCommitFile
---

# Get-GitCommitFile

## SYNOPSIS

Lists the files changed by a specific commit, comparing it against its parent.

## SYNTAX

### Commit (Default)

```
Get-GitCommitFile [[-Commit] <string>] [-InputObject <GitCommitInfo>] [-Path <string[]>]
 [-IgnoreWhitespace] [-Hunk] [-RepoPath <string>] [<CommonParameters>]
```

### Options

```
Get-GitCommitFile -Options <GitCommitFileOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Get-GitCommitFile cmdlet returns the files added, modified, deleted, or renamed by a commit relative to its first parent, equivalent to `git diff-tree --no-commit-id -r <commit>`. By default it inspects HEAD, but you can specify any commit SHA or ref with the `-Commit` parameter, or pipe `GitCommitInfo` objects from `Get-GitLog`.

When the `-Hunk` switch is specified, the cmdlet emits individual `GitDiffHunk` objects (parsed from the unified diff) instead of file-level `GitDiffEntry` objects.

## EXAMPLES

### Example 1 - Show files changed in the latest commit

Lists the files changed by HEAD.

```powershell
Get-GitCommitFile
```

### Example 2 - Show files changed in a specific commit

Lists the files changed by a specific commit SHA.

```powershell
Get-GitCommitFile -Commit abc1234
```

### Example 3 - Pipe commits from Get-GitLog

Shows changed files for the most recent commit received via pipeline.

```powershell
Get-GitLog -MaxCount 1 | Get-GitCommitFile
```

### Example 4 - Show detailed diff hunks

Emits individual diff hunks with line-level detail for the latest commit.

```powershell
Get-GitCommitFile -Hunk
```

## PARAMETERS

### -Commit

The commit SHA or ref to inspect. Defaults to HEAD when neither this parameter nor `-InputObject` is specified.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
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

### -Hunk

When specified, emits individual GitDiffHunk objects instead of file-level GitDiffEntry objects.

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

### -IgnoreWhitespace

When specified, whitespace-only changes are excluded from the output.

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

### -InputObject

A GitCommitInfo object, typically received from Get-GitLog via pipeline input.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitCommitInfo
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Commit
  Position: Named
  IsRequired: false
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Options

A pre-built GitCommitFileOptions object, allowing full control over the operation.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitCommitFileOptions
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

One or more repository-relative file paths to restrict the output.

```yaml
Type: System.String[]
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### PowerCode.Git.Abstractions.Models.GitCommitInfo

You can pipe GitCommitInfo objects from Get-GitLog to this cmdlet.

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitDiffEntry

By default, the cmdlet outputs one GitDiffEntry per changed file, containing path, status, lines added/deleted, and patch text.

### PowerCode.Git.Abstractions.Models.GitDiffHunk

When the -Hunk switch is specified, the cmdlet outputs individual GitDiffHunk objects with line-level detail.

## NOTES

For the initial commit (which has no parent), all files are shown as Added.

## RELATED LINKS

- [Get-GitLog](Get-GitLog.md)
- [Get-GitDiff](Get-GitDiff.md)
