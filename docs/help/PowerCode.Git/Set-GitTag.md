---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Set-GitTag.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-21-2026
PlatyPS schema version: 2024-05-01
title: Set-GitTag
---

# Set-GitTag

## SYNOPSIS

Creates a git tag, equivalent to git tag [-a] [-f] <name> [<target>].

## SYNTAX

### Tag (Default)

```
Set-GitTag [-Name] <string> [[-Target] <string>] [-Message <string>] [-Force] [-RepoPath <string>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Options

```
Set-GitTag -Options <GitTagCreateOptions> [-RepoPath <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The `Set-GitTag` cmdlet creates a tag reference in the repository. By default a
lightweight tag is created, which is simply a name that points directly at a
commit object.

When `-Message` is provided an annotated tag object is created instead. Annotated
tags store the tagger name, email, date, and a message, and are the recommended
form for release tags because commands like `git describe` ignore lightweight
tags by default.

Use `-Target` to tag a specific commit instead of HEAD, and `-Force` to replace
an existing tag with the same name.

## EXAMPLES

### Example 1 - Create a lightweight tag at HEAD

Creates a lightweight tag named `v1.0.0` pointing at the current HEAD commit.

```powershell
Set-GitTag -Name v1.0.0
```

### Example 2 - Create an annotated tag

Creates an annotated tag with a message. Annotated tags store the tagger
identity and date alongside the message.

```powershell
Set-GitTag -Name v2.0.0 -Message 'Release v2.0.0'
```

### Example 3 - Tag a specific commit with -Force

Replaces an existing `v1.0.0` tag so that it points at commit `abc1234`.

```powershell
Set-GitTag -Name v1.0.0 -Target abc1234 -Force
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

### -Force

Replaces an existing tag with the given name instead of failing. Equivalent to
`git tag -f`.

```yaml
Type: System.Management.Automation.SwitchParameter
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

### -Message

The annotation message for the tag. When provided, an annotated tag object is
created (equivalent to `git tag -a -m <message>`). When omitted, a lightweight
tag is created.

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

### -Name

The name of the tag to create. The name must pass the checks defined by
`git check-ref-format`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Tag
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Options

A pre-built `GitTagCreateOptions` object for full programmatic control over tag
creation. Mutually exclusive with the individual parameters in the `Tag`
parameter set.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitTagCreateOptions
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

### -Target

The committish (branch, tag, or SHA) that the new tag will refer to. Defaults to
HEAD when not specified.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Tag
  Position: 1
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

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitTagInfo

A tag object with properties such as `Name`, `Sha`, `ShortSha`, `IsAnnotated`,
`TaggerName`, `TaggerEmail`, `TagDate`, and `Message`.

## NOTES

Annotated tags are recommended for releases because they record who created the
tag and when. Lightweight tags are convenient for private or temporary labels.

When re-tagging after a tag has been pushed, use `-Force` with care — downstream
users who already fetched the old tag will not have it updated automatically.

## RELATED LINKS

- [Get-GitTag](Get-GitTag.md)
- [Get-GitLog](Get-GitLog.md)
- [git-tag documentation](https://git-scm.com/docs/git-tag)
