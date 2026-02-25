---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Get-GitPromptStatus.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-25-2026
PlatyPS schema version: 2024-05-01
title: Get-GitPromptStatus
---

# Get-GitPromptStatus

## SYNOPSIS

Gets a Powerline-styled prompt string showing the current git repository status.

## SYNTAX

### Prompt (Default)

```
Get-GitPromptStatus [-HideUpstream] [-HideCounts] [-HideStash] [-NoColor]
 [-RepoPath <string>] [<CommonParameters>]
```

### Options

```
Get-GitPromptStatus -Options <GitPromptStatusOptions> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

The `Get-GitPromptStatus` cmdlet returns a `GitPromptStatus` object whose `ToString()` method produces a compact, Powerline/Nerd-Font-styled string representing the current state of the git repository. This string is suitable for embedding directly in an interactive shell prompt.

The formatted string contains the following segments (each toggleable via switch parameters):

- **Upstream provider icon** — a Nerd Font glyph indicating the hosting provider (GitHub , GitLab , Bitbucket , Azure DevOps , or the generic git  for unknown remotes). Requires a Nerd Font installed as the terminal font.
- **Branch name** — the current branch in bold green (clean) or bold yellow (dirty). Detached HEAD is shown as `(short-sha)`.
- **Ahead/behind counts** — `↑N` (bold cyan) and `↓N` (bold red) when the branch diverges from its upstream.
- **Working-tree counts** — `+staged` (green), `~modified` (red), `?untracked` (dim).
- **Stash count** — `⚑N` (yellow) when stash entries are present.

ANSI color sequences are automatically stripped by PowerShell 7.4+ when output is redirected or `$PSStyle.OutputRendering` is set to `PlainText`.

## EXAMPLES

### Example 1 - Get the git prompt status

Returns the GitPromptStatus object; `FormattedString` is the ready-to-use prompt string.

```powershell
Get-GitPromptStatus
```

### Example 2 - Use in a PowerShell prompt function

Embeds the formatted string directly into the interactive prompt.

```powershell
function prompt { "$(Get-GitPromptStatus) > " }
```

### Example 3 - Plain text output without ANSI colors

Produces the same prompt segment without any color escape sequences.

```powershell
Get-GitPromptStatus -NoColor
```

### Example 4 - Minimal prompt showing only the branch name and upstream icon

Shows just the provider icon and branch name, hiding counts and stash.

```powershell
Get-GitPromptStatus -HideCounts -HideStash
```

## PARAMETERS

### -HideCounts

Omits the staged, modified, and untracked file count segments from the prompt string.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Prompt
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -HideStash

Omits the stash count indicator from the prompt string.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Prompt
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -HideUpstream

Omits the upstream provider icon and ahead/behind counts from the prompt string.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Prompt
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -NoColor

Strips all ANSI color escape sequences from the prompt string. Useful in terminals that do not support VT/ANSI codes.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Prompt
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

A pre-built `GitPromptStatusOptions` object providing full programmatic control over prompt generation.

```yaml
Type: PowerCode.Git.Abstractions.Models.GitPromptStatusOptions
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

None. `Get-GitPromptStatus` does not accept pipeline input.

## OUTPUTS

### PowerCode.Git.Abstractions.Models.GitPromptStatus

A status object with properties for the branch name, upstream provider, ahead/behind counts,
staged/modified/untracked file counts, and stash count. The `FormattedString` property contains
the ready-to-embed Powerline-styled prompt string, and `ToString()` returns that value.

## NOTES

- A **Nerd Font** must be installed and configured as the terminal font for the provider glyphs to render correctly. See [https://nerdfonts.com](https://nerdfonts.com).
- Default segment visibility can be configured permanently via `Set-GitModuleConfiguration -PromptHideUpstream`, `-PromptHideCounts`, `-PromptHideStash`, or `-PromptNoColor`.
- Provider detection is based on the fetch URL of the upstream remote (preferring the remote that matches the tracking branch, falling back to `origin`).

## RELATED LINKS

- [Get-GitStatus](Get-GitStatus.md)
- [Get-GitBranch](Get-GitBranch.md)
- [Set-GitModuleConfiguration](Set-GitModuleConfiguration.md)
