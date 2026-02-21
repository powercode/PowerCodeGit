# Plan: Add Git Parameter Sets to PowerShell Commands

## Overview

Each PowerCode.Git cmdlet needs parameter sets matching the most common `git.exe` usage patterns. A catch-all "Options" parameter set accepts a pre-built parameter object for full control.

**Workflow per command**: Implement parameter sets → update models/services → add unit tests → add system tests → verify all pass → commit → next command.

**Reference for git options**: `file:///C:/Program%20Files/Git/mingw64/share/doc/git-doc/git-<command>.html`

---

## Architecture Pattern

For each command, follow this layered approach:

1. **Abstractions/Models/** — Add/extend the options class with new properties
2. **Abstractions/Services/** — Update interface signature if needed (prefer options object over individual args)
3. **Core/Services/** — Implement new options in the LibGit2Sharp layer
4. **PowerCode.Git/Cmdlets/** — Add parameter sets to the cmdlet, mapping to the options object
5. **Tests/Cmdlets/** — Unit test parameter mapping and parameter set resolution
6. **Tests/Core.Tests/** — Unit test the service implementation with new options
7. **Tests/SystemTests/** — End-to-end Pester tests against real git repos

### Catch-All Pattern

Every cmdlet gets an `Options` parameter set where the user passes a fully-constructed options object:

```csharp
[Parameter(Mandatory = true, ParameterSetName = "Options")]
public GitXxxOptions Options { get; set; }
```

### Parameter Set Naming Convention

- Use descriptive names matching git's mental model: `"List"`, `"Create"`, `"Delete"`, `"Switch"`, `"Detach"`
- The default parameter set should be the most common usage
- `"Options"` is always the catch-all parameter set name

---

## Command 1: Get-GitBranch

**Git reference**: `git-branch.html` (list mode)

### Current State
- No parameters beyond `RepoPath`
- Calls `IGitBranchService.GetBranches(string repositoryPath)` → returns all local branches

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `List` (default) | `-Remote`, `-All`, `-Pattern`, `-Contains`, `-Merged`, `-NoMerged` | `git branch [-r\|-a] [-l <pattern>] [--contains] [--merged\|--no-merged]` |
| `Options` | `-Options <GitBranchListOptions>` | Catch-all |

### Changes Required

1. **New model**: `GitBranchListOptions` in Abstractions/Models/
   - `RepositoryPath` (required string)
   - `ListRemote` (bool) — `-r`
   - `ListAll` (bool) — `-a`
   - `Pattern` (string?) — `-l <pattern>`
   - `ContainsCommit` (string?) — `--contains <commit>`
   - `MergedInto` (string?) — `--merged [<commit>]`
   - `NotMergedInto` (string?) — `--no-merged [<commit>]`

2. **Update interface**: `IGitBranchService.GetBranches(GitBranchListOptions options)` (keep old overload or replace)

3. **Update service**: `GitBranchService` — filter branches based on options using LibGit2Sharp

4. **Update cmdlet**: `GetGitBranchCmdlet` — add parameter sets, build options object

5. **Tests**: Unit tests for parameter mapping, service filtering; system tests for `-Remote`, `-All`, `-Pattern`

---

## Command 2: New-GitBranch

**Git reference**: `git-branch.html` (create mode)

### Current State
- `Name` (mandatory) — calls `CreateBranch(repositoryPath, name)`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Create` (default) | `-Name`, `-StartPoint`, `-Track`, `-Force` | `git branch [-f] [--track] <name> [<start-point>]` |
| `Options` | `-Options <GitBranchCreateOptions>` | Catch-all |

### Changes Required

1. **New model**: `GitBranchCreateOptions` in Abstractions/Models/
   - `RepositoryPath` (required string)
   - `Name` (required string)
   - `StartPoint` (string?) — defaults to HEAD
   - `Track` (bool) — `--track`
   - `Force` (bool) — `-f` (reset if branch exists)

2. **Update interface**: `IGitBranchService.CreateBranch(GitBranchCreateOptions options)`

3. **Update service**: Support `StartPoint` (resolve committish), `Track`, `Force`

4. **Update cmdlet**: Add `-StartPoint`, `-Track`, `-Force` parameters

5. **Tests**: Create from specific commit, force-reset existing branch

---

## Command 3: Remove-GitBranch

**Git reference**: `git-branch.html` (delete mode)

### Current State
- `Name` (mandatory), `Force` (switch) — calls `DeleteBranch(repositoryPath, name, force)`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Delete` (default) | `-Name`, `-Force` | `git branch -d [-f] <name>` |
| `Options` | `-Options <GitBranchDeleteOptions>` | Catch-all |

### Changes Required

1. **New model**: `GitBranchDeleteOptions` in Abstractions/Models/
   - `RepositoryPath` (required string)
   - `Name` (required string)
   - `Force` (bool) — `-D`

2. **Update interface**: `IGitBranchService.DeleteBranch(GitBranchDeleteOptions options)`

3. **Update cmdlet**: Add `Options` parameter set (main set already sufficient)

4. **Tests**: Verify catch-all Options parameter set works

---

## Command 4: Switch-GitBranch

**Git reference**: `git-switch.html`

### Current State
- `Name` (mandatory) — calls `SwitchBranch(repositoryPath, branchName)`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Switch` (default) | `-Name`, `-Force` | `git switch <branch>` |
| `Create` | `-Create`, `-Name`, `-StartPoint`, `-Force` | `git switch -c <branch> [<start-point>]` |
| `Detach` | `-Detach`, `-Committish` | `git switch --detach [<commit>]` |
| `Options` | `-Options <GitSwitchOptions>` | Catch-all |

### Changes Required

1. **New model**: `GitSwitchOptions` in Abstractions/Models/
   - `RepositoryPath` (required string)
   - `BranchName` (string?) — target branch
   - `Create` (bool) — `-c`
   - `StartPoint` (string?) — for create mode
   - `Detach` (bool) — `--detach`
   - `Committish` (string?) — for detach mode
   - `Force` (bool) — `-f` (discard local changes)

2. **Update interface**: `IGitBranchService.SwitchBranch(GitSwitchOptions options)` → returns `GitBranchInfo`

3. **Update service**: Support create-and-switch, detach, force

4. **Update cmdlet**: Restructure with parameter sets

5. **Tests**: Switch, create-and-switch, detach HEAD, force switch

---

## Command 5: Get-GitStatus

**Git reference**: `git-status.html`

### Current State
- `IncludeIgnored` (switch) — builds `GitStatusOptions`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Default` (default) | `-IncludeIgnored`, `-UntrackedFiles`, `-Path` | `git status [--ignored] [-u<mode>] [-- <path>...]` |
| `Short` | `-Short`, `-Path` | `git status --short` |
| `Options` | `-Options <GitStatusOptions>` | Catch-all |

### Changes Required

1. **New enum**: `GitUntrackedFilesMode` — `No`, `Normal`, `All`

2. **Extend model** `GitStatusOptions`:
   - Add `Paths` (IReadOnlyList\<string\>?)
   - Add `UntrackedFilesMode` (GitUntrackedFilesMode?)
   - Add `Short` (bool) — for short format output

3. **Update service**: Filter by paths, respect untracked mode

4. **Update cmdlet**: Add parameters

5. **Tests**: Path filtering, untracked modes

---

## Command 6: Get-GitDiff

**Git reference**: `git-diff.html`

### Current State
- `Staged` (switch), `Path` (string[]) — builds `GitDiffOptions`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `WorkingTree` (default) | `-Path`, `-IgnoreWhitespace` | `git diff [-- <path>...]` |
| `Staged` | `-Staged`, `-Path`, `-IgnoreWhitespace` | `git diff --staged` |
| `Commit` | `-Commit`, `-Path`, `-IgnoreWhitespace` | `git diff <commit> [-- <path>...]` |
| `Range` | `-FromCommit`, `-ToCommit`, `-Path`, `-IgnoreWhitespace` | `git diff <commit>..<commit>` |
| `Options` | `-Options <GitDiffOptions>` | Catch-all |

### Changes Required

1. **Extend model** `GitDiffOptions`:
   - Add `Commit` (string?) — diff working tree against specific commit
   - Add `FromCommit` (string?) — range start
   - Add `ToCommit` (string?) — range end
   - Add `IgnoreWhitespace` (bool) — `-w`
   - Add `NameOnly` (bool) — `--name-only`
   - Add `Stat` (bool) — `--stat`

2. **Update service**: Support commit-based diffs and ignore-whitespace

3. **Update cmdlet**: Add parameter sets

4. **Tests**: Diff against commit, commit ranges, ignore whitespace

---

## Command 7: Add-GitItem

**Git reference**: `git-add.html`

### Current State
- `Path` (string[], set "Path"), `All` (switch, set "All") — builds `GitStageOptions`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Path` (default) | `-Path`, `-Force` | `git add [--force] <path>...` |
| `All` | `-All`, `-Force` | `git add --all` |
| `Update` | `-Update` | `git add --update` |
| `Options` | `-Options <GitStageOptions>` | Catch-all |

### Changes Required

1. **Extend model** `GitStageOptions`:
   - Add `Update` (bool) — `-u` (only already-tracked files)
   - Add `Force` (bool) — `-f` (add ignored files)
   - Add `DryRun` (bool) — `-n`
   - Add `IntentToAdd` (bool) — `-N`

2. **Update service**: Support update-only staging, force, dry-run

3. **Update cmdlet**: Add `Update`, `Force` parameter sets

4. **Tests**: Update-only staging, force-add ignored files

---

## Command 8: Reset-GitHead

**Git reference**: `git-reset.html`

### Current State
- `Revision` (string?), `Hard`/`Soft` switches, 3 parameter sets — calls `Reset(repo, revision, mode)`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Mixed` (default) | `-Revision` | `git reset [<commit>]` |
| `Soft` | `-Soft`, `-Revision` | `git reset --soft [<commit>]` |
| `Hard` | `-Hard`, `-Revision` | `git reset --hard [<commit>]` |
| `Paths` | `-Revision`, `-Path` | `git reset [<commit>] -- <path>...` |
| `Options` | `-Options <GitResetOptions>` | Catch-all |

### Changes Required

1. **New model**: `GitResetOptions` in Abstractions/Models/
   - `RepositoryPath` (required string)
   - `Revision` (string?)
   - `Mode` (GitResetMode)
   - `Paths` (IReadOnlyList\<string\>?) — for path-based reset (unstage)

2. **Update interface**: `IGitWorkingTreeService.Reset(GitResetOptions options)`

3. **Update service**: Support path-based reset (selective unstage)

4. **Update cmdlet**: Add `Path` parameter and `Paths` set

5. **Tests**: Path-based reset (unstage specific files)

---

## Command 9: Get-GitLog

**Git reference**: `git-log.html`

### Current State
- Rich parameter set: `Path`, `Branch`, `MaxCount`, `Author`, `Since`, `Until`, `MessagePattern`
- `GitLogOptions` already has `AllBranches` but it's NOT exposed on the cmdlet

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Default` (default) | `-Path`, `-Branch`, `-MaxCount`, `-Author`, `-Since`, `-Until`, `-MessagePattern`, `-AllBranches`, `-FirstParent`, `-NoMerges` | `git log [options] [-- <path>...]` |
| `Options` | `-Options <GitLogOptions>` | Catch-all |

### Changes Required

1. **Extend model** `GitLogOptions`:
   - Add `FirstParent` (bool) — `--first-parent`
   - Add `NoMerges` (bool) — `--no-merges`
   - Add `Follow` (bool) — `--follow` (single file rename tracking)

2. **Update service**: Support `FirstParent`, `NoMerges` in CommitFilter

3. **Update cmdlet**: Expose `AllBranches` (already in model!), add `FirstParent`, `NoMerges`

4. **Tests**: All-branches, first-parent, no-merges filtering

---

## Command 10: Save-GitCommit

**Git reference**: `git-commit.html`

### Current State
- `Message`, `Amend`, `AllowEmpty` — builds `GitCommitOptions`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Default` (default) | `-Message`, `-All`, `-Amend`, `-AllowEmpty` | `git commit [-a] [-m <msg>] [--amend] [--allow-empty]` |
| `Options` | `-Options <GitCommitOptions>` | Catch-all |

### Changes Required

1. **Extend model** `GitCommitOptions`:
   - Add `All` (bool) — `-a` (auto-stage tracked modified files before commit)
   - Add `Author` (string?) — `--author="Name <email>"`
   - Add `Date` (DateTimeOffset?) — `--date=<date>`

2. **Update service**: Stage tracked-modified files when `All` is set, pass author/date overrides

3. **Update cmdlet**: Add `-All`, `-Author`, `-Date` parameters

4. **Tests**: Commit with auto-stage, author override

---

## Command 11: Get-GitTag

**Git reference**: `git-tag.html` (list mode)

### Current State
- No parameters beyond `RepoPath` — calls `GetTags(repositoryPath)`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `List` (default) | `-Pattern`, `-Sort`, `-Contains` | `git tag [-l <pattern>] [--sort=<key>] [--contains <commit>]` |
| `Options` | `-Options <GitTagListOptions>` | Catch-all |

### Changes Required

1. **New model**: `GitTagListOptions` in Abstractions/Models/
   - `RepositoryPath` (required string)
   - `Pattern` (string?) — glob pattern filter
   - `SortBy` (string?) — sort key (e.g., `version:refname`)
   - `ContainsCommit` (string?) — `--contains`

2. **Update interface**: `IGitTagService.GetTags(GitTagListOptions options)`

3. **Update service**: Filter and sort tags

4. **Update cmdlet**: Add filter parameters

5. **Tests**: Pattern matching, sort, contains filtering

---

## Command 12: Copy-GitRepository

**Git reference**: `git-clone.html`

### Current State
- `Url`, `LocalPath`, `Credential`, `SingleBranch` — builds `GitCloneOptions`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Default` (default) | `-Url`, `-LocalPath`, `-Credential`, `-SingleBranch`, `-Depth`, `-Branch`, `-Bare`, `-RecurseSubmodules` | `git clone [options] <url> [<dir>]` |
| `Options` | `-Options <GitCloneOptions>` | Catch-all |

### Changes Required

1. **Extend model** `GitCloneOptions`:
   - Add `Depth` (int?) — `--depth <n>` (shallow clone)
   - Add `BranchName` (string?) — `--branch <name>`
   - Add `Bare` (bool) — `--bare`
   - Add `RecurseSubmodules` (bool) — `--recurse-submodules`

2. **Update service**: Pass depth, branch, bare, recurse options to LibGit2Sharp `CloneOptions`

3. **Update cmdlet**: Add new parameters

4. **Tests**: Shallow clone, specific branch clone, bare clone

---

## Command 13: Send-GitBranch

**Git reference**: `git-push.html`

### Current State
- `Remote`, `Name`, `SetUpstream`, `Credential` — builds `GitPushOptions`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Default` (default) | `-Remote`, `-Name`, `-SetUpstream`, `-Credential`, `-Force`, `-ForceWithLease`, `-DryRun` | `git push [-f\|--force-with-lease] [-n] [-u] [<remote> [<branch>]]` |
| `Delete` | `-Remote`, `-Name`, `-Credential` | `git push --delete <remote> <branch>` |
| `Tags` | `-Remote`, `-Credential`, `-Tags` | `git push --tags` |
| `All` | `-Remote`, `-Credential`, `-All` | `git push --all` |
| `Options` | `-Options <GitPushOptions>` | Catch-all |

### Changes Required

1. **Extend model** `GitPushOptions`:
   - Add `Force` (bool) — `-f`
   - Add `ForceWithLease` (bool) — `--force-with-lease`
   - Add `Delete` (bool) — `--delete`
   - Add `Tags` (bool) — `--tags`
   - Add `All` (bool) — `--all`
   - Add `DryRun` (bool) — `-n`

2. **Update service**: Support force push, delete remote branch, push tags

3. **Update cmdlet**: Add parameter sets

4. **Tests**: Force push, delete remote branch, push tags

---

## Command 14: Receive-GitBranch

**Git reference**: `git-pull.html`

### Current State
- `MergeStrategy`, `Prune`, `Credential` — builds `GitPullOptions`

### Proposed Parameter Sets

| Set Name | Parameters | Git Equivalent |
|----------|-----------|---------------|
| `Default` (default) | `-Remote`, `-MergeStrategy`, `-Prune`, `-Credential`, `-AutoStash`, `-Tags`, `-Depth` | `git pull [options] [<remote>]` |
| `Options` | `-Options <GitPullOptions>` | Catch-all |

### Changes Required

1. **Extend model** `GitPullOptions`:
   - Add `AutoStash` (bool) — `--autostash`
   - Add `Tags` (bool?) — `--tags` / `--no-tags`
   - Add `Depth` (int?) — `--depth <n>`

2. **Update service**: Pass autostash, tags, depth to LibGit2Sharp `PullOptions`

3. **Update cmdlet**: Add `-Remote` (already exists but defaulted), `-AutoStash`, `-Tags`, `-Depth`

4. **Tests**: Pull with autostash, tags control

---

## Execution Order

Work one command at a time, in dependency order:

| # | Command | Rationale |
|---|---------|-----------|
| 1 | `Get-GitBranch` | Foundation — read-only, simplest to verify |
| 2 | `New-GitBranch` | Depends on branch listing for verification |
| 3 | `Remove-GitBranch` | Depends on branch creation for setup |
| 4 | `Switch-GitBranch` | Complex parameter sets, uses branch operations |
| 5 | `Get-GitStatus` | Working tree read, independent |
| 6 | `Get-GitDiff` | Working tree read, independent |
| 7 | `Add-GitItem` | Working tree write, needs status for verification |
| 8 | `Reset-GitHead` | Working tree write, needs staging for setup |
| 9 | `Get-GitLog` | History read, mostly extending existing |
| 10 | `Save-GitCommit` | History write, needs staging for setup |
| 11 | `Get-GitTag` | Tag read, independent |
| 12 | `Copy-GitRepository` | Remote, standalone |
| 13 | `Send-GitBranch` | Remote push, needs repo with remote |
| 14 | `Receive-GitBranch` | Remote pull, needs repo with remote |

## Per-Command Checklist

For each command above, execute these steps in order:

- [ ] Read git-doc HTML for the command to verify parameter coverage
- [ ] Create/extend the options model in `Abstractions/Models/`
- [ ] Update the service interface in `Abstractions/Services/`
- [ ] Update the service implementation in `Core/Services/`
- [ ] Add unit tests for the service in `Core.Tests/Services/`
- [ ] Update the cmdlet with parameter sets in `PowerCode.Git/Cmdlets/`
- [ ] Add unit tests for the cmdlet in `Tests/Cmdlets/`
- [ ] Add system tests in `Tests/SystemTests/`
- [ ] Run all tests: `dotnet test` and `Invoke-Pester`
- [ ] Verify no regressions in existing tests
- [ ] Commit with conventional commit message describing changes
