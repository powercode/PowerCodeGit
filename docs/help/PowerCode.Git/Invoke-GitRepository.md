---
document type: cmdlet
external help file: PowerCode.Git.dll-Help.xml
HelpUri: https://github.com/powercode/PowerCodeGit/blob/{{BranchName}}/docs/help/PowerCode.Git/Invoke-GitRepository.md
Locale: en-US
Module Name: PowerCode.Git
ms.date: 02-25-2026
PlatyPS schema version: 2024-05-01
title: Invoke-GitRepository
---

# Invoke-GitRepository

## SYNOPSIS

Executes a ScriptBlock with direct access to the underlying LibGit2Sharp Repository object.

## SYNTAX

### __AllParameterSets

```
Invoke-GitRepository [-Action] <scriptblock> [-RepoPath <string>] [<CommonParameters>]
```

## ALIASES

None.

## DESCRIPTION

`Invoke-GitRepository` is the escape-hatch cmdlet for PowerCode.Git. When no purpose-built cmdlet
exists for a task, use it to drop down to the full
[LibGit2Sharp](https://github.com/libgit2/libgit2sharp) API without writing C#.

The repository is opened before the ScriptBlock executes and disposed after it completes,
regardless of errors. Inside the ScriptBlock the repository is available as `$repo`
(injected variable).

PowerShell accesses members via reflection, so property and method access such as
`$repo.Head.Tip.Sha` and `$repo.Branches['main']` works as expected. However, type literals
such as `[LibGit2Sharp.Signature]::new(...)` will not resolve because LibGit2Sharp is loaded in
an isolated AssemblyLoadContext, not the default one. Work with the objects you receive
dynamically rather than constructing LibGit2Sharp types by name.

Objects obtained from `$repo` are only valid within the ScriptBlock. Do not capture them in
variables that outlive the block.

## EXAMPLES

### Example 1 — List all remote URLs

```powershell
Invoke-GitRepository {
    $repo.Network.Remotes | ForEach-Object {
        [pscustomobject]@{ Name = $_.Name; Url = $_.Url; PushUrl = $_.PushUrl }
    }
}
```

Enumerates every configured remote and emits a custom object for each one containing its name,
fetch URL, and push URL.

### Example 2 — Inspect HEAD

```powershell
Invoke-GitRepository {
    [pscustomobject]@{
        Branch = $repo.Head.FriendlyName
        Sha    = $repo.Head.Tip.Sha.Substring(0, 7)
        When   = $repo.Head.Tip.Author.When.LocalDateTime
        Message = $repo.Head.Tip.MessageShort
    }
}
```

Returns a single object summarising the current HEAD commit.

### Example 3 — Count refs in a specific repository

```powershell
Invoke-GitRepository -RepoPath C:\src\myrepo {
    $count = 0
    foreach ($ref in $repo.Refs) { $count++ }
    [pscustomobject]@{ RefCount = $count; HeadSha = $repo.Head.Tip.Sha }
}
```

Opens the repository at `C:\src\myrepo`, counts every reference, and returns a summary object.

### Example 4 — Stream commit messages

```powershell
Invoke-GitRepository {
    foreach ($commit in $repo.Commits) {
        $commit.MessageShort
    }
} | Select-Object -First 20
```

Iterates all commits in history and pipes each short message string to `Select-Object`, which
stops after 20. Ctrl+C also stops the pipeline normally.

## PARAMETERS

### -Action

The ScriptBlock to execute. The repository object is injected into the ScriptBlock's scope as
`$repo` and is also available as `$args[0]`. Every object emitted by the ScriptBlock is written
to the output pipeline.

```yaml
Type: System.Management.Automation.ScriptBlock
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RepoPath

Path to the git repository. When omitted the current PowerShell working directory is used.
`Repository.Discover` is not called — the path must point directly to the repository root (the
directory that contains the `.git` folder, or a bare repository directory).

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

None. This cmdlet does not accept pipeline input.

## OUTPUTS

### System.Object

Output type is determined entirely by the ScriptBlock. Each object emitted inside the ScriptBlock
is written individually to the output pipeline. `PSCustomObject` values are passed through
intact; all other values are unwrapped from their `PSObject` shell so callers receive the
underlying .NET type.

## NOTES

**Cancellation:** Pressing Ctrl+C during a long ScriptBlock will stop the pipeline normally.
`PipelineStoppedException` propagates out of the ScriptBlock and the repository is disposed
in the `finally` block before the command exits.

**Performance:** `InvokeWithContext` buffers all ScriptBlock output before returning, so results
do not stream to the pipeline until the ScriptBlock completes. For very large result sets, prefer
using `foreach` loops inside the ScriptBlock rather than collecting everything into an array.

**Type literals:** `[LibGit2Sharp.Signature]`, `[LibGit2Sharp.ObjectId]`, and similar type
literals are not available inside the ScriptBlock because the assembly is loaded in an isolated
context. Use the objects you receive from `$repo` directly rather than constructing types by name.

## RELATED LINKS

- [LibGit2Sharp API](https://github.com/libgit2/libgit2sharp)
- [Get-GitLog](Get-GitLog.md)
- [Get-GitStatus](Get-GitStatus.md)
