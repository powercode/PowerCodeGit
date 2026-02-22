````skill
---
name: libgit2sharp-reference
description: 'Look up LibGit2Sharp API usage, find correct patterns, and verify implementations by consulting the LibGit2Sharp source code and test suite cloned in an adjacent directory. Use when implementing or modifying PowerCode.Git.Core services that wrap LibGit2Sharp, debugging unexpected behaviour, or checking correct option types, enum values, and callback signatures.'
---

# LibGit2Sharp Reference

The full source code for [LibGit2Sharp](https://github.com/libgit2/libgit2sharp) is
available in a sibling directory next to this repository (e.g. `../libgit2sharp`).

## Repository Layout

```
../libgit2sharp/
├── LibGit2Sharp/                  ← Public API surface (classes, options, enums)
│   ├── Commands/                  ← Static command helpers (Checkout, Fetch, Pull, …)
│   ├── Core/                      ← Internal/low-level plumbing
│   ├── Repository.cs              ← Main entry point
│   └── …                          ← One file per major type
├── LibGit2Sharp.Tests/            ← *Fixture tests — primary usage reference
│   ├── TestHelpers/               ← Shared test infrastructure
│   └── *Fixture.cs                ← Per-feature test files
└── LibGit2Sharp.sln
```

## Test suite as usage reference

The LibGit2Sharp test suite is an excellent source of information on how the library is
intended to be used. When implementing or modifying `PowerCode.Git.Core` services that
wrap LibGit2Sharp, consult the corresponding tests in the LibGit2Sharp repository to
understand correct API usage, expected behaviour, and edge cases.

Each `*Fixture.cs` file contains tests for the corresponding API area. Use this table to
jump straight to the relevant tests when working on a PowerCode.Git feature.

| PowerCode.Git Feature | LibGit2Sharp Test File | Key APIs |
|------------------------|------------------------|----------|
| Clone / `Copy-GitRepository` | `CloneFixture.cs` | `Repository.Clone`, `CloneOptions` |
| Fetch / `Receive-GitBranch` | `FetchFixture.cs` | `Commands.Fetch`, `FetchOptions` |
| Push / `Send-GitBranch` | `PushFixture.cs`, `NetworkFixture.cs` | `Network.Push`, `PushOptions` |
| Pull / `Receive-GitBranch` | `MergeFixture.cs`, `FetchFixture.cs` | `Commands.Pull`, `PullOptions`, `MergeOptions` |
| Branches / `Get-GitBranch`, `New-GitBranch` | `BranchFixture.cs` | `BranchCollection`, `Branch` |
| Checkout / `Switch-GitBranch` | `CheckoutFixture.cs` | `Commands.Checkout`, `CheckoutOptions` |
| Commit / `Save-GitCommit` | `CommitFixture.cs` | `Repository.Commit`, `CommitOptions` |
| Diff / `Get-GitDiff` | `DiffTreeToTreeFixture.cs`, `DiffBlobToBlobFixture.cs`, `DiffTreeToTargetFixture.cs` | `Diff.Compare`, `CompareOptions` |
| Log / `Get-GitLog` | `CommitFixture.cs`, `LogFixture.cs` | `CommitLog`, `CommitFilter` |
| Status / `Get-GitStatus` | `StatusFixture.cs` | `RepositoryStatus`, `StatusOptions` |
| Stage / `Add-GitItem` | `StageFixture.cs`, `IndexFixture.cs` | `Index.Add`, `StageOptions` |
| Tags / `Get-GitTag`, `Set-GitTag` | `TagFixture.cs` | `TagCollection`, `Tag` |
| Reset / `Reset-GitHead` | `ResetHeadFixture.cs`, `ResetIndexFixture.cs` | `Repository.Reset`, `ResetMode` |
| Stash | `StashFixture.cs` | `StashCollection`, `StashApplyOptions` |
| Worktrees / `Get-GitWorktree`, `New-GitWorktree` | `WorktreeFixture.cs` | `WorktreeCollection`, `Worktree` |
| Blame | `BlameFixture.cs` | `BlameHunkCollection`, `BlameOptions` |
| Cherry-pick | `CherryPickFixture.cs` | `Repository.CherryPick`, `CherryPickOptions` |
| Rebase | `RebaseFixture.cs` | `Repository.Rebase`, `RebaseOptions` |
| Revert | `RevertFixture.cs` | `Repository.Revert`, `RevertOptions` |
| Merge | `MergeFixture.cs` | `Repository.Merge`, `MergeOptions` |
| Remote | `RemoteFixture.cs` | `RemoteCollection`, `Remote` |
| Submodules | `SubmoduleFixture.cs` | `SubmoduleCollection` |
| Config | `ConfigurationFixture.cs` | `Configuration` |
| References | `ReferenceFixture.cs`, `ReflogFixture.cs` | `ReferenceCollection`, `ReflogCollection` |
| Ignore | `IgnoreFixture.cs` | `Ignore` |

## How to Use This Skill

### 1. Find the right test fixture

When wrapping a LibGit2Sharp API in `PowerCode.Git.Core`, locate the matching fixture
from the table above. Read the test file to understand:

- How to construct option objects and set their properties.
- Which callbacks or delegates need to be wired up.
- What preconditions the API expects (e.g. must be non-bare repo).
- What return types and result states to expect.

### 2. Read API source for details

If the test fixture doesn't clarify a question, read the API source directly:

```
../libgit2sharp/LibGit2Sharp/<ClassName>.cs       ← Type definition
../libgit2sharp/LibGit2Sharp/Commands/<Name>.cs   ← Static command helpers
```

### 3. Common patterns from the test suite

**Opening a repository:**
```csharp
using var repo = new Repository(repoPath);
```

**Fetch with credentials:**
```csharp
var options = new FetchOptions
{
    CredentialsProvider = (_url, _user, _cred) =>
        new UsernamePasswordCredentials { Username = "user", Password = "pass" }
};
Commands.Fetch(repo, "origin", repo.Network.Remotes["origin"].FetchRefSpecs.Select(r => r.Specification), options, "log message");
```

**Checkout a branch:**
```csharp
Commands.Checkout(repo, repo.Branches["feature"]);
```

**Comparing trees (diff):**
```csharp
var changes = repo.Diff.Compare<TreeChanges>(oldTree, newTree);
var patch = repo.Diff.Compare<Patch>(oldTree, newTree);
```

**Creating a tag:**
```csharp
repo.Tags.Add("v1.0", repo.Head.Tip);                         // lightweight
repo.Tags.Add("v1.0", repo.Head.Tip, signature, "message");   // annotated
```

**Worktrees:**
```csharp
var worktree = repo.Worktrees.Add("wt-name", "wt-name", worktreePath, false);
```

## When to consult

- **Before** wrapping a new LibGit2Sharp API in a `PowerCode.Git.Core` service.
- **When debugging** unexpected behaviour from a LibGit2Sharp call — compare your
  invocation against the test that exercises the same code path.
- **When unsure** about correct option types, enum values, or callback signatures.
- **When mapping** LibGit2Sharp results to `PowerCode.Git.Abstractions` DTOs — check
  what properties are available on the source objects.
````
