# PowerCode.Git — ScriptBlock Integration Planning Document

## Executive Summary

PowerShell ScriptBlocks, combined with their ability to convert to .NET delegates (`Func<T,TResult>`, `Action<T>`, predicates), present a unique opportunity to surpass what `git.exe` offers through shell script integration. Where git shells out to bash for `filter-branch`, hooks, custom formats, and merge drivers — all string-based and fragile — PowerCode.Git can provide **typed, object-aware, debuggable, composable** alternatives that operate directly on the LibGit2Sharp object model.

This document captures the design opportunities, architectural patterns, and implementation priorities for leveraging ScriptBlocks throughout the module.

---

## Core Architectural Pattern

### The ScriptBlock-to-Delegate Bridge

All ScriptBlock-accepting cmdlets share a common need: converting PowerShell ScriptBlocks into .NET delegates that LibGit2Sharp callbacks and LINQ operations can consume.

**Implementation: `ScriptBlockExtensions`**

A static utility class that provides generic conversion methods:

```csharp
internal static class ScriptBlockExtensions
{
    public static Func<T, bool> ToPredicate<T>(this ScriptBlock scriptBlock);
    public static Func<T, TResult> ToFunc<T, TResult>(this ScriptBlock scriptBlock);
    public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(this ScriptBlock scriptBlock);
    public static Action<T> ToAction<T>(this ScriptBlock scriptBlock);
}
```

Each method wraps `ScriptBlock.InvokeWithContext()` and uses `LanguagePrimitives.IsTrue()` or `LanguagePrimitives.ConvertTo()` for result coercion. This ensures PowerShell's type conversion semantics are preserved when crossing the boundary into LibGit2Sharp's typed API.

**Design considerations:**

- ScriptBlocks invoked via `InvokeWithContext` run in the caller's session state, giving access to the user's variables, functions, and modules.
- The object passed to the ScriptBlock is available as both `$args[0]` and `$_` (when using pipeline-style `ForEach-Object` patterns within the block).
- Error handling must propagate PowerShell errors (both terminating and non-terminating) correctly through the delegate boundary.
- Performance: each ScriptBlock invocation has overhead (~microseconds). For commit walks of 100K+ commits, consider offering a compiled delegate alternative or a `-Fast` parameter that uses a restricted expression evaluator.

**File location:** `src/PowerCode.Git/ScriptBlockExtensions.cs`

---

## Proposed Cmdlets

### 1. Invoke-GitRepository

**Verb-Noun:** `Invoke-GitRepository`
**Priority:** High — foundational cmdlet that enables all ad-hoc scenarios
**Status:** Not implemented

#### Purpose

Provides direct, scoped access to the `LibGit2Sharp.Repository` object within a ScriptBlock. The repository is opened before the ScriptBlock executes and disposed after it completes, ensuring correct resource management regardless of what the user does inside the block.

This is the "escape hatch" cmdlet — when no purpose-built cmdlet exists for a task, users can drop down to the full LibGit2Sharp API without writing C#.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Action` | `ScriptBlock` | Yes | The ScriptBlock to execute. Receives the `Repository` object as `$args[0]` and via a `$repo` variable. |
| `-Path` | `string` | No | Path to the repository. Defaults to current working directory. Uses `Repository.Discover()` to find the `.git` directory. |

#### Behavior

1. Discover the repository root from `-Path` or `$PWD`.
2. Open the repository with `new Repository(discovered)`.
3. Define a `$repo` variable in the ScriptBlock's scope.
4. Invoke the ScriptBlock with the `Repository` as `$args[0]`.
5. Write each result from the ScriptBlock to the output pipeline, unwrapping `PSObject` where appropriate.
6. Dispose the repository in a `finally` block.

#### Output Type

`System.Object` — output is determined entirely by the user's ScriptBlock.

#### Example Scenarios

```powershell
# List all remote URLs
Invoke-GitRepository {
    $repo.Network.Remotes | ForEach-Object {
        [pscustomobject]@{ Name = $_.Name; Url = $_.Url; PushUrl = $_.PushUrl }
    }
}

# Count objects in the repository database
Invoke-GitRepository {
    $count = 0
    foreach ($ref in $repo.Refs) { $count++ }
    [pscustomobject]@{ RefCount = $count; HeadSha = $repo.Head.Tip.Sha }
}

# Atomic multi-step operation
Invoke-GitRepository {
    $branch = $repo.Branches['feature/work']
    $sig = [LibGit2Sharp.Signature]::new('Bot', 'bot@example.com', [DateTimeOffset]::Now)
    $repo.Merge($branch, $sig)
}
```

#### Design Notes

- Consider a `-ReadOnly` switch that opens the repository with reduced permissions, protecting against accidental writes in exploratory sessions.
- Consider `-AsTransaction` for operations that should be atomic (though LibGit2Sharp does not support true transactions, the cmdlet could snapshot refs and roll back on error).
- The `$repo` variable approach is more ergonomic than `$args[0]` for multi-line ScriptBlocks.

---

### 2. Select-GitCommit

**Verb-Noun:** `Select-GitCommit`
**Priority:** High — most common ad-hoc git task is searching history
**Status:** Not implemented

#### Purpose

Walks commit history with a PowerShell predicate ScriptBlock for filtering and an optional projection ScriptBlock for output shaping. Replaces complex `git log` format strings and `--grep`/`--author`/`--since` flag combinations with arbitrary PowerShell logic.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Where` | `ScriptBlock` | Yes | Predicate that receives a `LibGit2Sharp.Commit` as `$args[0]`. Return `$true` to include the commit. |
| `-Select` | `ScriptBlock` | No | Projection that receives a `LibGit2Sharp.Commit` as `$args[0]`. Output replaces the raw commit in the pipeline. |
| `-First` | `int` | No | Maximum number of matching commits to return. Defaults to unlimited. |
| `-From` | `string` | No | Starting reference (branch, tag, SHA). Defaults to `HEAD`. |
| `-Path` | `string` | No | Repository path. |

#### Behavior

1. Open the repository.
2. Configure a `CommitFilter` for the starting point (`-From`).
3. Iterate commits via `repo.Commits.QueryBy(filter)`.
4. For each commit, invoke the `-Where` ScriptBlock.
5. If the predicate returns truthy, either invoke `-Select` and emit the result, or emit the raw `Commit`.
6. Stop after `-First` matches.

#### Output Type

`LibGit2Sharp.Commit` (default) or `System.Object` (when `-Select` is provided).

#### Why This Beats git.exe

| git.exe approach | Problem |
|------------------|---------|
| `git log --author='Name' --since='2025-01-01' --grep='fix'` | Only AND logic between built-in filters. Cannot express "author X OR author Y". Cannot filter on commit tree content. |
| `git log --format='%H %an %ae %s' \| awk ...` | String parsing. Breaks on messages containing delimiters. |
| `git rev-list --all \| xargs -I{} git show --stat {} \| ...` | Spawns a process per commit. Extremely slow. |

With `Select-GitCommit`, the user has the full `Commit` object graph: `Author`, `Committer`, `Tree`, `Parents`, `Notes`, `Message`, and can traverse related objects (diff against parents, inspect tree entries) — all in a single process, with PowerShell's full expression language.

#### Example Scenarios

```powershell
# Multi-author search (impossible with single --author flag)
Select-GitCommit -Where {
    $c = $args[0]
    $c.Author.Name -in @('Alice', 'Bob') -and
    $c.Author.When.Year -eq 2025
} -First 50

# Commits that are merge commits with more than 2 parents
Select-GitCommit -Where { $args[0].Parents.Count() -gt 2 }

# Commits where the tree has a specific file
Select-GitCommit -Where { $args[0].Tree['README.md'] -ne $null }

# Custom output projection
Select-GitCommit -Where { $true } -Select {
    $c = $args[0]
    [pscustomobject]@{
        Short    = $c.Sha.Substring(0, 8)
        Author   = $c.Author.Name
        When     = $c.Author.When.LocalDateTime
        Message  = $c.MessageShort
        Parents  = $c.Parents.Count()
        IsMerge  = $c.Parents.Count() -gt 1
    }
} -First 20 | Format-Table
```

#### Performance Considerations

- Walking large histories (100K+ commits) with a ScriptBlock per commit will be noticeably slower than `git log`. Document this trade-off.
- Consider a `-Parallel` switch that uses `Parallel.ForEach` with thread-safe ScriptBlock invocation (note: ScriptBlocks are bound to a `Runspace` — this requires `RunspacePool` and `ScriptBlock.Clone()`).
- Consider offering common predicates as compiled parameters (e.g., `-Author`, `-Since`, `-Until`, `-MessagePattern`) that bypass ScriptBlock invocation and filter at the LibGit2Sharp level, with `-Where` as the advanced override.

---

### 3. ForEach-GitBlob

**Verb-Noun:** `ForEach-GitBlob`
**Priority:** High — enables powerful code search and analysis without checkout
**Status:** Not implemented

#### Purpose

Iterates over blobs (file objects) in a tree-ish reference, invoking a ScriptBlock for each. The ScriptBlock receives a rich object with the blob's path, metadata, and lazy content access — without ever writing files to disk.

This replaces `git ls-tree -r | while read mode type sha path; do git cat-file -p $sha | ...; done` with a single, ergonomic command.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Process` | `ScriptBlock` | Yes | ScriptBlock invoked per blob entry. Receives a custom object as `$args[0]` with `Path`, `Blob`, `Size`, `IsBinary` properties and a `GetContent()` script method. |
| `-Treeish` | `string` | No | Tree-ish to walk. Defaults to `HEAD`. Accepts branches, tags, SHAs. |
| `-Filter` | `string` | No | Wildcard filter on file paths. Defaults to `*`. |
| `-Recurse` | `SwitchParameter` | No | Walk subdirectories recursively. Defaults to `$true`. |
| `-Path` | `string` | No | Repository path. |

#### Blob Entry Object Shape

The object passed to the ScriptBlock should be a `PSObject` with:

| Property/Method | Type | Description |
|-----------------|------|-------------|
| `Path` | `string` | Full path relative to repository root (e.g., `src/Program.cs`). |
| `Blob` | `LibGit2Sharp.Blob` | The underlying blob object. |
| `Size` | `long` | Blob size in bytes. |
| `IsBinary` | `bool` | Whether LibGit2Sharp detected the blob as binary. |
| `GetContent()` | `ScriptMethod` → `string` | Lazily reads the blob content as UTF-8 text. Only materializes when called. |
| `GetContentStream()` | `ScriptMethod` → `Stream` | Returns the raw content stream for binary processing. |
| `ObjectId` | `string` | The blob's SHA. |

The lazy `GetContent()` design is critical: iterating 10,000 files to check extensions is fast; only reading content for matching files avoids unnecessary I/O.

#### Example Scenarios

```powershell
# Search for secrets across the entire tree at a specific tag
ForEach-GitBlob -Treeish v2.0 -Filter '*.config' -Process {
    $entry = $args[0]
    if (-not $entry.IsBinary) {
        $content = $entry.GetContent()
        if ($content -match '(?i)(password|secret|apikey)\s*=\s*\S+') {
            [pscustomobject]@{
                Path    = $entry.Path
                Match   = $Matches[0]
                BlobSha = $entry.ObjectId
            }
        }
    }
}

# Language statistics without checkout
ForEach-GitBlob -Treeish HEAD -Process {
    [pscustomobject]@{
        Extension = [IO.Path]::GetExtension($args[0].Path)
        Size      = $args[0].Size
    }
} | Group-Object Extension |
    Select-Object Name, Count, @{N='TotalKB';E={[math]::Round(($_.Group | Measure-Object Size -Sum).Sum / 1KB, 1)}} |
    Sort-Object TotalKB -Descending

# Compare file content between two commits without checkout
$v1Files = ForEach-GitBlob -Treeish v1.0 -Filter '*.cs' -Process {
    [pscustomobject]@{ Path = $args[0].Path; Sha = $args[0].ObjectId }
}
$v2Files = ForEach-GitBlob -Treeish v2.0 -Filter '*.cs' -Process {
    [pscustomobject]@{ Path = $args[0].Path; Sha = $args[0].ObjectId }
}
Compare-Object $v1Files $v2Files -Property Path, Sha
```

#### Implementation Notes

- Tree walking must be recursive. `TreeEntry.TargetType` is either `Blob`, `Tree`, or `GitLink` (submodule). Recurse into `Tree` entries, skip `GitLink`.
- Build the full relative path by accumulating directory names during recursion.
- The `GetContent()` script method captures `$this.Blob` via closure. Ensure the repository remains open for the lifetime of all emitted objects (do not dispose until `EndProcessing`).
- For very large repositories, consider streaming output rather than collecting all results.

---

### 4. Compare-GitTree

**Verb-Noun:** `Compare-GitTree`
**Priority:** Medium — useful but partially covered by existing `Get-GitDiff`
**Status:** Not implemented

#### Purpose

Compares two tree-ish references and provides ScriptBlock-based filtering and transformation of the diff results. Where `Get-GitDiff` focuses on standard diff output, `Compare-GitTree` enables **custom diff logic** — semantic diffs, AST-based comparison, metric extraction.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Base` | `string` | Yes | Base tree-ish reference. |
| `-Compare` | `string` | Yes | Comparison tree-ish reference. |
| `-Where` | `ScriptBlock` | No | Predicate on `TreeEntryChanges`. Receives the change as `$args[0]`. |
| `-Transform` | `ScriptBlock` | No | Projection that receives `TreeEntryChanges` and the `Repository` as `$args[0]` and `$args[1]`. Can look up blobs for content-level diffing. |
| `-Path` | `string` | No | Repository path. |

#### Output Type

`LibGit2Sharp.TreeEntryChanges` (default) or `System.Object` (when `-Transform` is provided).

#### Key Insight: Typed Diff Filtering

`git diff` supports `--diff-filter=ACDMRTUX` to filter by change type, but this is a fixed set of single-character flags. With a ScriptBlock predicate, the user can filter on **any property or combination**:

```powershell
# Files added or copied, larger than 100KB
Compare-GitTree HEAD~10 HEAD -Where {
    $c = $args[0]
    $c.Status -in @('Added', 'Copied') -and $c.Path -notlike '*.min.js'
}

# Custom semantic diff with blob access
Compare-GitTree v1.0 v2.0 -Where { $args[0].Path -like '*.cs' } -Transform {
    $change = $args[0]
    $repo = $args[1]
    $oldBlob = if ($change.OldOid -ne [LibGit2Sharp.ObjectId]::Zero) { $repo.Lookup($change.OldOid) -as [LibGit2Sharp.Blob] }
    $newBlob = if ($change.Oid -ne [LibGit2Sharp.ObjectId]::Zero) { $repo.Lookup($change.Oid) -as [LibGit2Sharp.Blob] }
    [pscustomobject]@{
        Path      = $change.Path
        Status    = $change.Status
        OldLines  = if ($oldBlob -and -not $oldBlob.IsBinary) { ($oldBlob.GetContentText() -split "`n").Count } else { 0 }
        NewLines  = if ($newBlob -and -not $newBlob.IsBinary) { ($newBlob.GetContentText() -split "`n").Count } else { 0 }
    }
}
```

---

### 5. Edit-GitHistory

**Verb-Noun:** `Edit-GitHistory`
**Priority:** Medium — powerful but dangerous, needs careful design
**Status:** Not implemented

#### Purpose

Rewrites repository history using ScriptBlock-based filters, replacing `git filter-branch` and `git filter-repo`. The user provides ScriptBlocks for commit metadata rewriting and/or tree content filtering.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-CommitFilter` | `ScriptBlock` | No | Receives each `Commit`. Returns `$null` to keep unchanged, or a hashtable with overrides for `Author`, `Committer`, `Message`. |
| `-TreeFilter` | `ScriptBlock` | No | Receives each `TreeEntry`. Returns `$true` to keep, `$false` to remove from the rewritten tree. |
| `-Refs` | `string[]` | No | References to rewrite. Defaults to all branches. |
| `-Path` | `string` | No | Repository path. |

#### Safety

- `SupportsShouldProcess = true` with `ConfirmImpact = ConfirmImpact.High`.
- Always creates a backup ref namespace (`refs/original/`) before rewriting.
- `-WhatIf` performs a dry run and reports what would change without modifying anything.
- Consider requiring `-Force` in addition to confirmation for destructive operations.

#### LibGit2Sharp Implementation

LibGit2Sharp provides `repo.Refs.RewriteHistory()` (or equivalent via the `HistoryRewriter` API). The ScriptBlock filters are converted to the appropriate delegate types using `ScriptBlockExtensions`.

#### Example Scenarios

```powershell
# Fix email address across all history
Edit-GitHistory -CommitFilter {
    $c = $args[0]
    if ($c.Author.Email -eq 'wrong@old.com') {
        @{
            Author = [LibGit2Sharp.Signature]::new(
                $c.Author.Name, 'correct@new.com', $c.Author.When)
        }
    }
} -WhatIf

# Remove accidentally committed large files from all history
Edit-GitHistory -TreeFilter {
    $entry = $args[0]
    -not ($entry.Path -like '*.zip' -or $entry.Path -like '*.exe')
}

# Rewrite commit messages to add ticket numbers
Edit-GitHistory -CommitFilter {
    $c = $args[0]
    if ($c.Message -match 'PROJ-\d+') {
        $null  # keep unchanged
    } else {
        @{ Message = "[NO-TICKET] $($c.Message)" }
    }
} -Refs @('refs/heads/main')
```

---

### 6. Merge-GitBranch

**Verb-Noun:** `Merge-GitBranch`
**Priority:** Medium — conflict resolution is a major pain point
**Status:** Not implemented

#### Purpose

Performs a merge with an optional ScriptBlock-based conflict resolver. Where git normally drops to manual conflict resolution (editing marker files), this cmdlet enables **programmatic, content-aware conflict resolution**.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Branch` | `string` | Yes | Branch to merge into the current branch. |
| `-ConflictResolver` | `ScriptBlock` | No | Receives `$ancestor`, `$ours`, `$theirs` (content strings), and `$path` (file path). Returns resolved content as a string, or `$null` to leave the conflict unresolved. |
| `-Strategy` | `string` | No | Merge strategy hint (e.g., `Recursive`, `Ours`, `Theirs`). |
| `-NoCommit` | `SwitchParameter` | No | Perform merge but don't commit. |
| `-Path` | `string` | No | Repository path. |

#### Conflict Resolver Design

The ScriptBlock receives rich context per conflicted file:

```powershell
# ConflictResolver signature
{
    param(
        [string]$Ancestor,    # Common ancestor content (null for add/add conflicts)
        [string]$Ours,        # Our side's content
        [string]$Theirs,      # Their side's content
        [string]$Path         # File path
    )

    # Return resolved content, or $null to skip
    return $resolvedContent
}
```

#### Example: Structured File Merging

This is where ScriptBlocks truly shine over shell scripts — structured data merging:

```powershell
Merge-GitBranch feature/deps -ConflictResolver {
    param($ancestor, $ours, $theirs, $path)

    switch -Wildcard ($path) {
        '*.csproj' {
            # Parse as XML, merge PackageReference elements
            $oursXml = [xml]$ours
            $theirsXml = [xml]$theirs
            $oursRefs = $oursXml.SelectNodes('//PackageReference')
            $theirsRefs = $theirsXml.SelectNodes('//PackageReference')
            foreach ($ref in $theirsRefs) {
                $existing = $oursRefs | Where-Object { $_.Include -eq $ref.Include }
                if (-not $existing) {
                    $imported = $oursXml.ImportNode($ref, $true)
                    $ref.ParentNode.AppendChild($imported)
                }
            }
            $oursXml.OuterXml
        }
        'package.json' {
            # Merge JSON dependencies, take higher semver
            $oursJson = $ours | ConvertFrom-Json
            $theirsJson = $theirs | ConvertFrom-Json
            foreach ($prop in $theirsJson.dependencies.PSObject.Properties) {
                $ourVersion = $oursJson.dependencies.$($prop.Name)
                if (-not $ourVersion -or [version]($prop.Value -replace '[^0-9.]') -gt [version]($ourVersion -replace '[^0-9.]')) {
                    $oursJson.dependencies | Add-Member -NotePropertyName $prop.Name -NotePropertyValue $prop.Value -Force
                }
            }
            $oursJson | ConvertTo-Json -Depth 10
        }
        default { $null }  # Leave unresolved
    }
}
```

---

### 7. Register-GitHook

**Verb-Noun:** `Register-GitHook`
**Priority:** Low-Medium — high value but complex lifecycle management
**Status:** Not implemented

#### Purpose

Registers PowerShell ScriptBlocks as git hook equivalents. Unlike traditional `.git/hooks/` scripts (which must be bash/batch files), these hooks are:

- **Debuggable:** Set breakpoints, use `Wait-Debugger`.
- **Module-aware:** Access any installed PowerShell module.
- **Typed:** Receive structured objects, not raw stdin text.
- **Composable:** Multiple hooks can be registered and chained.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `-Hook` | `string` | Yes | Hook name. Validated set: `PreCommit`, `CommitMsg`, `PrePush`, `PostCheckout`, `PostMerge`, `PreRebase`. |
| `-Action` | `ScriptBlock` | Yes | ScriptBlock to execute. Return `$false` or throw to abort the operation (for pre-* hooks). |
| `-Name` | `string` | No | Display name for the hook registration. Allows multiple hooks per event. |
| `-Repository` | `string` | No | Scope to a specific repository. Default is global (all repos opened by the module). |

#### Architecture

Hooks are stored in the module's configuration system (via `Get-GitModuleConfiguration` / `Set-GitModuleConfiguration`). The companion cmdlets (`Save-GitCommit`, `Send-GitBranch`, `Start-GitRebase`, etc.) check for registered hooks and invoke them at the appropriate lifecycle points.

```
Save-GitCommit
  → Check registered PreCommit hooks
    → Invoke each ScriptBlock with staged file info
    → If any returns $false or throws → abort commit
  → Perform commit via LibGit2Sharp
  → Check registered PostCommit hooks
    → Invoke each ScriptBlock with the new Commit object
```

#### Hook Input Objects

Each hook type receives a purpose-built input object:

| Hook | Input Object |
|------|-------------|
| `PreCommit` | Array of staged file entries: `@{Path; Status; IsNew; IsModified; IsDeleted}` |
| `CommitMsg` | The proposed commit message (string). Return modified message or `$null` to keep. |
| `PrePush` | Remote name, URL, and array of refs being pushed. |
| `PostCheckout` | Previous HEAD, new HEAD, branch flag. |
| `PostMerge` | Squash flag, merge result. |

#### Example Scenarios

```powershell
# Enforce conventional commits
Register-GitHook CommitMsg -Name 'ConventionalCommits' -Action {
    $msg = $args[0]
    if ($msg -notmatch '^(feat|fix|docs|style|refactor|perf|test|chore)(\(.+\))?: .+') {
        Write-Error "Commit message must follow Conventional Commits format."
        return $false
    }
}

# Pre-commit: run PSScriptAnalyzer on staged .ps1 files
Register-GitHook PreCommit -Name 'PSScriptAnalyzer' -Action {
    $staged = $args[0]
    $psFiles = $staged | Where-Object { $_.Path -like '*.ps1' -and -not $_.IsDeleted }
    if ($psFiles) {
        $results = $psFiles | ForEach-Object {
            Invoke-ScriptAnalyzer -Path $_.FullPath -ExcludeRule PSAvoidUsingWriteHost
        }
        if ($results | Where-Object Severity -eq 'Error') {
            $results | Format-Table -AutoSize | Out-String | Write-Error
            return $false
        }
    }
}

# Post-checkout: auto-restore .NET tools
Register-GitHook PostCheckout -Name 'DotnetRestore' -Action {
    if (Test-Path 'dotnet-tools.json') {
        dotnet tool restore --verbosity quiet
    }
}
```

#### Companion Cmdlets

- `Get-GitHook` — List registered hooks.
- `Unregister-GitHook` — Remove a registered hook.
- `Test-GitHook` — Invoke a hook manually for testing/debugging.

---

## Non-Cmdlet Opportunities

### A. ArgumentCompleter ScriptBlocks with Repository Context

Tab completion for git parameters can use ScriptBlocks that have access to the repository:

```csharp
// Register argument completer that queries the live repository
[ArgumentCompleter(typeof(BranchCompleter))]
public string Branch { get; set; }
```

The completer opens the repository and enumerates branches/tags/remotes. This already likely exists in the module, but the pattern can be extended:

```powershell
# User-defined completers for custom parameters
Register-ArgumentCompleter -CommandName Select-GitCommit -ParameterName From -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete)
    Invoke-GitRepository {
        $repo.Refs |
            Where-Object { $_.CanonicalName -like "*$wordToComplete*" } |
            ForEach-Object {
                [System.Management.Automation.CompletionResult]::new(
                    $_.FriendlyName, $_.FriendlyName, 'ParameterValue', $_.CanonicalName)
            }
    }
}
```

### B. Format ScriptBlocks for Get-GitLog

Allow users to define custom output formatters as ScriptBlocks, similar to `git log --format` but with full object access:

```powershell
Get-GitLog -First 10 -Format {
    $c = $args[0]
    $color = if ($c.Parents.Count() -gt 1) { "`e[33m" } else { "`e[32m" }
    "$color$($c.Sha.Substring(0,8))`e[0m $($c.Author.Name.PadRight(20)) $($c.MessageShort)"
}
```

This could be implemented by adding a `-Format` parameter to `Get-GitLog` that accepts `ScriptBlock`.

### C. Pipeline-Aware Repository Context

Consider a pattern where the repository context flows through the pipeline via a hidden property:

```csharp
// Attach repository context to output objects
var output = new PSObject(commit);
output.Properties.Add(new PSNoteProperty("__GitRepository", repoPath));
```

This would allow downstream cmdlets to automatically discover which repository an object came from, enabling true pipeline composition:

```powershell
# Repository context flows automatically
Get-GitBranch | Where-Object { $_.IsRemote } |
    ForEach-GitLog -First 5 |
    Select-GitCommit -Where { $_.Author.Name -eq 'Alice' }
```

### D. ScriptBlock-Based Diff Drivers

For `Get-GitDiff`, allow custom diff rendering via ScriptBlock:

```powershell
Get-GitDiff HEAD~1 -DiffRenderer {
    param($oldContent, $newContent, $path)
    # Use a .NET diff library for word-level diff
    # Or generate HTML diff
    # Or compare ASTs for PowerShell/C# files
}
```

---

## Implementation Priority and Dependencies

### Phase 1: Foundation (High Priority)

| Cmdlet | Rationale |
|--------|-----------|
| `ScriptBlockExtensions` | Required by all other cmdlets. Implement first. |
| `Invoke-GitRepository` | Escape hatch. Immediately useful. Lowest complexity. |
| `Select-GitCommit` | Most frequently needed. Demonstrates the paradigm. |

### Phase 2: Tree & Diff (Medium Priority)

| Cmdlet | Rationale |
|--------|-----------|
| `ForEach-GitBlob` | Unique capability — no git.exe equivalent for lazy blob enumeration. |
| `Compare-GitTree` | Builds on existing diff infrastructure. |
| `Get-GitLog -Format` parameter | Enhancement to existing cmdlet. |

### Phase 3: History Rewriting & Merge (Medium Priority)

| Cmdlet | Rationale |
|--------|-----------|
| `Edit-GitHistory` | High value but requires careful safety design. |
| `Merge-GitBranch` | Conflict resolution is a differentiating feature. |

### Phase 4: Hooks & Integration (Lower Priority)

| Cmdlet | Rationale |
|--------|-----------|
| `Register-GitHook` | Requires changes to existing cmdlets (Save-GitCommit, etc.) to invoke hooks. |
| `Get-GitHook` / `Unregister-GitHook` / `Test-GitHook` | Supporting cmdlets. |

---

## Comparison: git.exe + Shell vs PowerCode.Git + ScriptBlock

| Capability | git.exe + Shell | PowerCode.Git + ScriptBlock |
|------------|-----------------|----------------------------|
| **Commit filtering** | `git log --author --since --grep` — fixed set of AND-only filters | `Select-GitCommit -Where { <arbitrary predicate> }` — any logic, OR/AND/NOT, access to tree |
| **Output formatting** | `git log --format='%H %an'` — string interpolation, limited to format codes | `-Select { <projection> }` — full object construction, type-safe |
| **History rewriting** | `git filter-branch --env-filter '...'` — bash-only, string escaping issues, single script | `Edit-GitHistory -CommitFilter { ... } -TreeFilter { ... }` — typed objects, PowerShell closures |
| **File search across refs** | `git ls-tree -r REF \| while read ...; git cat-file -p SHA` — process-per-file | `ForEach-GitBlob -Treeish REF { <access blob content lazily> }` — single process, lazy loading |
| **Hooks** | `.git/hooks/pre-commit` — bash file, no debugging, no module access | `Register-GitHook PreCommit { ... }` — debuggable, access to all PS modules |
| **Merge conflict resolution** | Manual editing of `<<<<<<<` marker files | `Merge-GitBranch -ConflictResolver { ... }` — programmatic, content-aware, structured-data merging |
| **Diff filtering** | `git diff --diff-filter=M` — single-character type codes | `Compare-GitTree -Where { <any predicate on TreeEntryChanges> }` — arbitrary filtering |
| **Custom diff rendering** | `git diff --word-diff-regex=<regex>` — regex-based word splitting | `-Transform { <access both blobs, apply AST diff, semantic diff> }` — full .NET |
| **Cross-commit analysis** | Complex pipelines: `git rev-list \| xargs \| sort \| uniq -c` | PowerShell pipeline: `Select-GitCommit \| Group-Object \| Measure-Object` — native objects throughout |
| **Type safety** | None — everything is strings | Full — `Commit`, `Blob`, `Tree`, `Branch`, `Tag`, `Signature`, `ObjectId` |
| **Error handling** | Exit codes, stderr parsing | PowerShell error records, try/catch, `-ErrorAction` |
| **Composability** | Pipe text between processes | Pipe objects between cmdlets within a single process |

---

## Technical Risks and Mitigations

### Risk: ScriptBlock Performance on Large Histories

**Problem:** Invoking a ScriptBlock per commit for repositories with 100K+ commits may be unacceptably slow (~50-100μs per invocation = 5-10 seconds for 100K commits).

**Mitigations:**
1. Offer built-in compiled parameters (e.g., `-Author`, `-Since`) that filter before the ScriptBlock.
2. Support `-First` to enable early termination.
3. Document the performance trade-off clearly.
4. Future: investigate `ScriptBlock.Compile()` or expression-tree compilation for simple predicates.

### Risk: Repository Lifetime Management

**Problem:** ScriptBlocks may capture references to LibGit2Sharp objects (`Commit`, `Blob`) that become invalid after the repository is disposed.

**Mitigations:**
1. For cmdlets that emit LibGit2Sharp objects, convert them to `PSObject` copies with the relevant data extracted.
2. For `Invoke-GitRepository`, document that objects are only valid within the ScriptBlock scope.
3. Consider a `ConvertTo-PSObject` pattern that eagerly snapshots relevant properties.

### Risk: Thread Safety for Parallel Execution

**Problem:** LibGit2Sharp's `Repository` object is not thread-safe. `ScriptBlock` objects are bound to a single `Runspace`.

**Mitigations:**
1. Do not offer `-Parallel` in initial implementation.
2. If parallel execution is added later, open separate `Repository` instances per thread and use `ScriptBlock.Clone()` with a `RunspacePool`.

### Risk: Error Propagation Across ScriptBlock Boundary

**Problem:** Exceptions thrown in a ScriptBlock invoked via `InvokeWithContext` are wrapped in `ActionPreferenceStopException` or swallowed depending on `$ErrorActionPreference`.

**Mitigations:**
1. Check `ScriptBlock.InvokeWithContext` return for errors.
2. Check `$PSCmdlet.HasErrors()` or inspect the error stream after each invocation.
3. For predicate ScriptBlocks, treat errors as `$false` with a warning.
4. For action/projection ScriptBlocks, propagate errors to the cmdlet's error stream.

---

## Open Questions

1. **Should `Invoke-GitRepository` expose additional LibGit2Sharp types?** For example, pre-importing `LibGit2Sharp.Signature`, `LibGit2Sharp.ObjectId`, etc. into the ScriptBlock's scope, or adding `using namespace LibGit2Sharp` equivalent.

2. **Should ScriptBlock parameters accept `[Func[Commit, bool]]` directly?** PowerShell can convert ScriptBlocks to delegate types automatically. Declaring the parameter as `Func<Commit, bool>` rather than `ScriptBlock` would enable both ScriptBlock and compiled delegate usage. Trade-off: less flexibility in the ScriptBlock (fixed signature) but better performance for compiled delegates.

3. **Should we provide a DSL for common predicates?** For example, a hashtable-based query syntax:
   ```powershell
   Select-GitCommit -Query @{ Author = 'Alice'; Since = '2025-01-01'; MessageMatch = 'fix' }
   ```
   This could be compiled to a single predicate without per-commit ScriptBlock invocation.

4. **How should the module handle large binary blobs in `ForEach-GitBlob`?** Should `GetContent()` have a size guard? Should there be a `-MaxSize` parameter?

5. **Should `Register-GitHook` persist hooks across sessions?** If so, where? Options: module configuration file, repository-local `.powergit/hooks.json`, or PowerShell profile.

---

## References

- [LibGit2Sharp API Documentation](https://github.com/libgit2/libgit2sharp)
- [PowerShell ScriptBlock Documentation](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_script_blocks)
- [PowerShell Language Primitives](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.languageprimitives)
- [git filter-branch](https://git-scm.com/docs/git-filter-branch) — the shell-based predecessor
- [git-filter-repo](https://github.com/newren/git-filter-repo) — Python-based alternative, similar philosophy
