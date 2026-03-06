---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Select-GitCommit.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-25-2026
PlatyPS schema version: 2024-05-01
title: Select-GitCommit
---

# Select-GitCommit

## SYNOPSIS

Searches git commit history for commits whose diff contains a string or matches a regular expression, or that satisfy a PowerShell ScriptBlock predicate.

## SYNTAX

### Contains (Default)

```
Select-GitCommit [-Contains] <string> [-Where <scriptblock>] [-First <int>] [-From <string>]
 [-Path <string[]>] [-RepoPath <string>] [<CommonParameters>]
```

### Match

```
Select-GitCommit [-Match] <string> [-Where <scriptblock>] [-First <int>] [-From <string>]
 [-Path <string[]>] [-RepoPath <string>] [<CommonParameters>]
```

### Where

```
Select-GitCommit -Where <scriptblock> [-First <int>] [-From <string>] [-Path <string[]>]
 [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

This cmdlet has no aliases.


## DESCRIPTION

`Select-GitCommit` walks commit history and emits `GitCommitInfo` objects for every commit that satisfies the active filters. Results are returned in reverse-chronological order (newest first).

Three parameter sets are available, following the same operator convention as `Where-Object`:

- **Contains** (default) — matches commits whose diff against the first parent contains the given plain-text substring (case-sensitive, ordinal). For example `-Contains 'TODO'` finds every commit that added or removed a line containing the word TODO.

- **Match** — matches commits whose diff against the first parent contains a line matching a .NET regular expression (e.g. `-Match 'TODO|FIXME'`). Equivalent to `git log -G <pattern>`.

- **Where** — filters commits using an arbitrary PowerShell ScriptBlock. The block receives the raw `LibGit2Sharp.Commit` as the `$commit` variable, which exposes the full object graph including `Author`, `Committer`, `Tree`, `Parents`, and `Notes`.

`-Where` can be combined with either `-Contains` or `-Match`: the diff filter runs first, and the ScriptBlock predicate is only evaluated on the commits that survive it.

**Performance:** Walking large histories with a per-commit ScriptBlock is slower than native `git log`. Use `-First` to stop early and `-Path` to narrow the candidate set.

## EXAMPLES

### Example 1 — Plain-text search for commits that mention TODO

Searches the entire history of the current repository for commits whose diff contains the string `TODO`. Results are returned newest-first.

```powershell
Select-GitCommit -Contains 'TODO'
```

```output
Sha       ShortSha AuthorName    MessageShort
---       -------- ----------    ------------
abc12345… abc1234  Alice Smith   Add another TODO entry
def67890… def6789  Bob Jones     Add TODO item
```

### Example 2 — Regex search matching multiple patterns

Uses `-Match` with a regular expression to find commits whose diff contains either `TODO` or `FIXME`. This is the `-Match` operator analogue from `Where-Object`.

```powershell
Select-GitCommit -Match 'TODO|FIXME'
```

### Example 3 — Anchored regex to find lines starting with a phrase

Finds commits that added a line starting with `Add TODO` anywhere in the diff. The `\+` matches the unified-diff `+` prefix that marks added lines.

```powershell
Select-GitCommit -Match '^\+Add TODO'
```

### Example 4 — Limit results with -First

Returns only the two most recent commits whose diff contains `FIXME`.

```powershell
Select-GitCommit -Contains 'FIXME' -First 2
```

### Example 5 — Filter by author using -Where

Returns every commit authored by Alice. The `-Where` ScriptBlock receives the raw `LibGit2Sharp.Commit` as the `$commit` variable.

```powershell
Select-GitCommit -Where { $commit.Author.Name -eq 'Alice' }
```

### Example 6 — Combine -Contains and -Where

Finds commits whose diff contains `TODO` **and** that were authored by Alice. The substring filter runs first; `-Where` is only evaluated on the surviving commits.

```powershell
Select-GitCommit -Contains 'TODO' -Where { $commit.Author.Name -eq 'Alice' }
```

### Example 7 — Restrict candidates to a specific file

Only commits that touch `src/handler.ts` are considered, then filtered by the substring.

```powershell
Select-GitCommit -Contains 'FIXME' -Path 'src/handler.ts'
```

### Example 8 — Start the walk from a specific branch

Walks commits reachable from `feature/my-work` rather than `HEAD`. Useful for searching within a feature branch without switching branches.

```powershell
Select-GitCommit -Contains 'TODO' -From 'feature/my-work'
```

## PARAMETERS

### -First

The maximum number of matching commits to return. Once this many results have been emitted, the history walk stops. This provides the same early-exit optimisation as `git log -n` and is particularly useful when combined with content search over large repositories.

Must be a positive integer (≥ 1).

```yaml
Type: System.Int32
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

### -From

The branch name, tag, or commit SHA from which the history walk starts. Only commits reachable from this starting point are considered.

When omitted the walk starts from `HEAD`. This parameter accepts tab-completion for local branches and tags.

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

### -Contains

A plain-text search string matched against the patch text of each changed file in the commit diff. Only commits whose diff against the first parent contains this substring are returned. The comparison is case-sensitive and ordinal.

Mutually exclusive with `-Match`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Contains
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Match

A .NET regular expression matched against the text of each changed hunk in the commit diff. Only commits whose diff against the first parent contains a match for this pattern are returned. Equivalent to `git log -G <pattern>`.

Follows the same `-match` operator semantics as `Where-Object -Match`. The pattern is applied with `RegexOptions.Multiline` so `^` and `$` anchor to line boundaries within the hunk.

Mutually exclusive with `-Contains`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Match
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

One or more repository-relative file paths. When specified, only commits that touch at least one of the given paths are considered as candidates. This is equivalent to appending `-- <path>…` to a `git log` command.

Reducing the candidate set with `-Path` significantly improves performance when searching large repositories. Accepts tab-completion for tracked file paths.

```yaml
Type: System.String[]
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

### -RepoPath

The path to the root of the git repository to search. Defaults to the current directory. Any path inside a working tree is accepted; the cmdlet resolves the repository root automatically.

Alias: `RepositoryPath`

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

### -Where

A PowerShell ScriptBlock used as a per-commit predicate, following the same convention as `Where-Object -FilterScript`. The block receives the raw `LibGit2Sharp.Commit` object as an injected `$commit` variable. Return `$true` (or any truthy value) to include the commit in results.

The `LibGit2Sharp.Commit` object exposes properties such as `Author`, `Committer`, `Message`, `Tree`, `Parents`, and `Notes`, giving access to the full commit object graph.

In the **Contains** and **Match** parameter sets this predicate is applied *after* the diff filter, so it only evaluates commits that already passed the pattern match. In the **Where** parameter set it is the only filter applied.

> **Performance note:** ScriptBlock predicates invoke the PowerShell engine once per candidate commit. Use `-First` and `-Path` to limit the candidate set and improve throughput.

```yaml
Type: System.Management.Automation.ScriptBlock
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Contains
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Match
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: Where
  Position: Named
  IsRequired: true
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

Each returned object represents a matching commit and exposes the following key properties:

| Property | Description |
|---|---|
| `Sha` | Full 40-character commit SHA. |
| `ShortSha` | Abbreviated 7-character SHA. |
| `MessageShort` | First line of the commit message. |
| `Message` | Full commit message including body. |
| `AuthorName` | Display name of the commit author. |
| `AuthorEmail` | Email address of the commit author. |
| `AuthorDate` | Authoring date (`DateTimeOffset`). |
| `CommitterName` | Display name of the committer. |
| `CommitterDate` | Commit date (`DateTimeOffset`). |

## NOTES

**`-Contains` vs `-Match`:** Use `-Contains` for straightforward substring searches — it is the simplest and fastest option. Use `-Match` when you need the full power of .NET regular expressions (alternation, anchors, character classes, etc.). `-Contains` performs a case-sensitive ordinal comparison; `-Match` follows the same conventions as the PowerShell `-match` operator used in `Where-Object`.

**Walk order:** Commits are always returned in reverse-chronological order (newest first), following the default `git log` walk strategy from the starting commit.

**ScriptBlock scope:** The ScriptBlock supplied to `-Where` runs in the calling session's scope, so it has access to variables defined in the caller.

**Patch text format:** Both `-Contains` and `-Match` are applied against the full raw patch text for each file in the diff, which includes `+`/`-` prefix characters and hunk headers. Account for these when writing precise patterns.

## RELATED LINKS

- [Get-GitLog](Get-GitLog.md)
- [Get-GitCommitFile](Get-GitCommitFile.md)
- [Get-GitDiff](Get-GitDiff.md)
