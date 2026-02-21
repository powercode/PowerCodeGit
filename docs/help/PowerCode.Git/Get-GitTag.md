---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-21-2026
PlatyPS schema version: 2024-05-01
title: Get-GitTag
---

# Get-GitTag

## SYNOPSIS

Lists tags in a git repository, equivalent to git tag -l.

## SYNTAX

### Tag (Default)

```
Get-GitTag [-Pattern <string>] [-SortBy <string>] [-ContainsCommit <string>] [-RepoPath <string>]
 [<CommonParameters>]
```

### Options

```
Get-GitTag -Options <GitTagListOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The Get-GitTag cmdlet lists tags in a git repository. Use `-Pattern` to filter tag names with a glob pattern, `-SortBy` to control ordering, and `-ContainsCommit` to find tags that contain a specific commit.

Each tag is returned as a GitTagInfo object.

## EXAMPLES

### Example 1 - List all tags

Lists all tags in the repository.

```powershell
Get-GitTag
```

### Example 2 - Filter tags by pattern

Lists tags matching a glob pattern.

```powershell
Get-GitTag -Pattern 'v1.*'
```

## PARAMETERS

### -ContainsCommit

Filters to only tags that contain the specified commit.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Tag
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

A pre-built GitTagListOptions object for full programmatic control.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitTagListOptions
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

### -Pattern

A glob pattern to filter tag names.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Tag
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

### -SortBy

Controls the sort order of tags. Accepts 'name' or 'version'.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Tag
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

### PowerCode.Git.Abstractions.Models.GitTagInfo

A tag object with properties such as Name, Sha, and IsAnnotated.

## NOTES

Annotated tags include tagger information; lightweight tags point directly to a commit.

## RELATED LINKS

- [Get-GitLog](Get-GitLog.md)
- [Get-GitBranch](Get-GitBranch.md)

