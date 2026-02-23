# PowerCode.Git

[![CI](https://github.com/powercode/PowerCodeGit/actions/workflows/ci.yml/badge.svg)](https://github.com/powercode/PowerCodeGit/actions/workflows/ci.yml)

A PowerShell binary module that brings native, idiomatic git commands to your terminal. Built in C# on top of [LibGit2Sharp](https://github.com/libgit2/libgit2sharp), **PowerCode.Git** provides `ShouldProcess` support, intelligent tab completion, and ANSI-colored output that mirrors familiar git styles.

## Features

- **27 cmdlets** covering everyday git workflows — commits, branches, tags, diffs, status, worktrees, rebase, configuration, clone, push, pull, and more
- **Tab completion** for branches, commits, tracked paths, remotes, and worktrees
- **Rich formatted output** with ANSI colors matching native git (yellow SHAs, green branches, red remotes, cyan tracking info)
- **Dual parameter sets** — friendly individual parameters *or* a single `-Options` object for full control
- **`ShouldProcess` / `-WhatIf`** on every mutating cmdlet
- **Assembly Load Context isolation** — LibGit2Sharp and its native libraries load in an isolated ALC, avoiding dependency conflicts
- **Cross-platform** — tested on Ubuntu and Windows via GitHub Actions

## Requirements

| Requirement | Version |
|---|---|
| PowerShell | 7.4+ (Core only) |
| .NET SDK (build only) | 10.0+ |

## Installation

### From PSGallery (recommended)

```powershell
Install-PSResource -Name PowerCode.Git -Repository PSGallery
```

### From source

```powershell
git clone https://github.com/powercode/PowerCodeGit.git
cd PowerCodeGit
dotnet build --configuration Release
```

The module is output to `artifacts/module/PowerCode.Git/<version>/`. Import it with:

```powershell
Import-Module ./artifacts/module/PowerCode.Git/<version>/PowerCode.Git.psd1
```

## Cmdlets

### History & Commits

| Cmdlet | Git equivalent | Description |
|---|---|---|
| `Get-GitLog` | `git log` | Retrieve commit history with filtering by path, branch, author, date range, or message pattern |
| `Get-GitCommitFile` | `git diff-tree -r` | List files changed by a specific commit, optionally as diff hunks |
| `Save-GitCommit` | `git commit` | Create a commit (supports `-Amend`, `-All`, `-AllowEmpty`) |

### Working Tree & Index

| Cmdlet | Git equivalent | Description |
|---|---|---|
| `Get-GitStatus` | `git status` | Retrieve working tree and index status |
| `Get-GitDiff` | `git diff` | Retrieve diff entries for working tree, staged, or commit ranges |
| `Add-GitItem` | `git add` | Stage files by path, `-All`, or `-Update` |
| `Restore-GitItem` | `git restore` | Discard working-tree changes or unstage index changes |
| `Reset-GitHead` | `git reset` | Reset HEAD with Mixed / Soft / Hard modes, or unstage by path |

### Branches

| Cmdlet | Git equivalent | Description |
|---|---|---|
| `Get-GitBranch` | `git branch` | List branches (local, remote, or all) with filtering |
| `New-GitBranch` | `git branch` + `git switch` | Create a new branch with optional start point and tracking |
| `Switch-GitBranch` | `git switch` | Switch branches, create-and-switch, or detach HEAD |
| `Remove-GitBranch` | `git branch -d` | Delete a branch (with `-Force` for unmerged) |

### Tags

| Cmdlet | Git equivalent | Description |
|---|---|---|
| `Get-GitTag` | `git tag -l` | List tags with pattern filtering and sorting |
| `Set-GitTag` | `git tag` | Create lightweight or annotated tags |

### Rebase

| Cmdlet | Git equivalent | Description |
|---|---|---|
| `Start-GitRebase` | `git rebase` | Start a rebase, replaying commits on top of an upstream branch (supports `-Interactive`, `-AutoStash`, `-Onto`) |
| `Resume-GitRebase` | `git rebase --continue / --skip` | Resume a paused rebase after resolving conflicts, or skip the conflicting commit |
| `Stop-GitRebase` | `git rebase --abort` | Abort the current rebase and restore the branch to its original state |

### Configuration

| Cmdlet | Git equivalent | Description |
|---|---|---|
| `Get-GitConfiguration` | `git config` | Read configuration values from repository, user, or system scopes |
| `Set-GitConfiguration` | `git config` | Write a configuration value to a specific scope |

### Remotes

| Cmdlet | Git equivalent | Description |
|---|---|---|
| `Copy-GitRepository` | `git clone` | Clone a remote repository (supports credentials, bare, single-branch, submodules) |
| `Send-GitBranch` | `git push` | Push to remote (supports `-Force`, `-ForceWithLease`, `-Delete`, `-Tags`, `-SetUpstream`, `-DryRun`) |
| `Receive-GitBranch` | `git pull` | Pull remote changes (merge strategies: Merge / FastForward / Rebase, `-Prune`, `-AutoStash`) |

### Worktrees

| Cmdlet | Git equivalent | Description |
|---|---|---|
| `Get-GitWorktree` | `git worktree list` | List worktrees |
| `New-GitWorktree` | `git worktree add` | Create a new worktree |
| `Remove-GitWorktree` | `git worktree remove` | Remove a worktree |
| `Lock-GitWorktree` | `git worktree lock` | Lock a worktree with optional reason |
| `Unlock-GitWorktree` | `git worktree unlock` | Unlock a previously locked worktree |

## Examples

### View commit log with decorations

```powershell
Get-GitLog -MaxCount 20
```

Output mirrors `git log --oneline --decorate` with colored SHAs, branch names, and tags.

### Stage and commit changes

```powershell
Add-GitItem -All
Save-GitCommit -Message "feat: add new feature"
```

### Create a feature branch and switch to it

```powershell
New-GitBranch -Name feature/my-feature
```

### Check working tree status

```powershell
Get-GitStatus
```

### Push with upstream tracking

```powershell
Send-GitBranch -SetUpstream
```

### Pull with rebase

```powershell
Receive-GitBranch -Strategy Rebase -AutoStash
```

### Clone a repository

```powershell
Copy-GitRepository -Url https://github.com/owner/repo.git -Path ./repo
```

### Create an annotated tag

```powershell
Set-GitTag -Name v1.0.0 -Message "Release 1.0.0"
```

### Show files changed in the latest commit

```powershell
Get-GitCommitFile
```

### Discard working-tree changes for a file

```powershell
Restore-GitItem -Path ./file.txt
```

### Rebase the current branch onto main

```powershell
Start-GitRebase -Upstream main -AutoStash
```

### Read git configuration

```powershell
Get-GitConfiguration -Name user.name
```

### Work with worktrees

```powershell
New-GitWorktree -Name hotfix -Path ../hotfix-worktree
Get-GitWorktree
Remove-GitWorktree -Name hotfix
```

## Tab Completion

PowerCode.Git provides intelligent tab completion for common git entities:

| Completer | Completes | Used by |
|---|---|---|
| **Branch** | Local and remote branch names | `Switch-GitBranch`, `Get-GitLog`, `Send-GitBranch`, `Remove-GitBranch`, `New-GitWorktree`, `Start-GitRebase` |
| **Committish** | Commit SHAs and messages | `Get-GitDiff`, `Get-GitCommitFile`, `Switch-GitBranch`, `Set-GitTag`, `Reset-GitHead`, `New-GitBranch` |
| **Path** | Tracked file paths | `Get-GitLog`, `Get-GitDiff`, `Get-GitCommitFile`, `Add-GitItem`, `Restore-GitItem`, `Reset-GitHead` |
| **Remote** | Remote names (with URL tooltips) | `Send-GitBranch` |
| **Worktree** | Worktree names (with path tooltips) | `Remove-GitWorktree`, `Lock-GitWorktree`, `Unlock-GitWorktree` |

## Output Formatting

All output types have custom ANSI-colored format views:

- **`GitCommitInfo`** — Yellow SHA, bold green local branches, bold red remote branches, bold yellow tags (matches `git log --oneline --decorate`)
- **`GitStatusResult`** — Bold green branch name, green staged count, red modified count
- **`GitStatusEntry`** — Colored status codes matching `git status --short`
- **`GitBranchInfo`** — Current branch in bold green with `*`, remotes in red, tracking info in cyan
- **`GitDiffEntry`** / **`GitTagInfo`** / **`GitWorktreeInfo`** — Table and list views

Colors are automatically stripped when output is redirected (PowerShell 7.4+ behaviour).

## Architecture

```
PowerCode.Git (Cmdlets, Completers, Formatters)
    │
    ├── PowerCode.Git.Abstractions (Shared interfaces & DTOs)
    │
    └── PowerCode.Git.Core (LibGit2Sharp service implementations)
            └── [Isolated AssemblyLoadContext]
```

The project uses a **custom AssemblyLoadContext** to isolate LibGit2Sharp and its native libraries from the PowerShell host. The shared `PowerCode.Git.Abstractions` assembly provides interfaces and DTOs that are identical across both contexts. Services are resolved via `DependencyContext` and consumed through `ServiceFactory`.

| Project | Purpose |
|---|---|
| **PowerCode.Git** | Cmdlets, completers, formatters, module initializer |
| **PowerCode.Git.Abstractions** | Shared `I*Service` interfaces and model/option DTOs (no external dependencies) |
| **PowerCode.Git.Core** | `LibGit2Sharp`-based service implementations, runs inside isolated ALC |

### Module Layout (after build)

```
artifacts/module/PowerCode.Git/<version>/
├── PowerCode.Git.dll                 # Cmdlet assembly
├── PowerCode.Git.Abstractions.dll    # Shared interfaces & DTOs
├── PowerCode.Git.psd1               # Module manifest
├── PowerCode.Git.Format.ps1xml      # ANSI format definitions
├── en-US/                            # MAML help
└── dependencies/
    ├── PowerCode.Git.Core.dll        # Service implementations
    ├── LibGit2Sharp.dll
    └── runtimes/<rid>/native/…       # Native libgit2 libraries
```

## Development

### Prerequisites

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download) (see `global.json`)
- [PowerShell 7.4+](https://github.com/PowerShell/PowerShell)
- [Pester 5+](https://pester.dev/) (for system tests)

### Build

```powershell
dotnet build
```

### Run unit tests

```powershell
dotnet test
```

### Run system tests

System tests are Pester-based end-to-end tests that exercise the module against real git repositories:

```powershell
./scripts/Invoke-SystemTests.ps1
```

To run tests for a specific cmdlet:

```powershell
./scripts/Invoke-SystemTests.ps1 -CommandName Get-GitLog
```

### Build MAML help

Generates MAML XML help from the PlatyPS v2 markdown docs in `docs/help/`:

```powershell
./scripts/Build-MamlHelp.ps1
```

### Clean

```powershell
./scripts/clean.ps1
```

## CI/CD

The project uses GitHub Actions for continuous integration and publishing:

- **CI** (`ci.yml`) — Runs on every push/PR to `main` and `preview`. Builds, runs unit tests and system tests on both Ubuntu and Windows, generates MAML help, and uploads the module artifact.
- **Publish** (`publish.yml`) — Triggered by `v*` tags. Builds, tests, patches the module manifest with the tag version, and publishes to [PSGallery](https://www.powershellgallery.com/).

## Contributing

1. Fork the repository
2. Create a feature branch from `preview`
3. Make your changes with tests
4. Run `dotnet test` and `./scripts/Invoke-SystemTests.ps1` to verify
5. Submit a pull request to `preview`

### Conventions

- Cmdlets follow PowerShell [approved verb](https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/approved-verbs-for-windows-powershell-commands) naming
- Every cmdlet supports a dual parameter set pattern (individual parameters + `-Options` object)
- Mutating cmdlets implement `ShouldProcess`
- Each cmdlet has unit tests (MSTest) and a Pester system test
- Microsoft.PowerShell.PlatyPS v2 help documentation in `docs/help/`
- Help examples must have corresponding system tests

## License

Copyright (c) Staffan Gustafsson. All rights reserved.
