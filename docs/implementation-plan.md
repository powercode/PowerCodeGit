# PowerGit — Full Workflow Cmdlets Implementation Plan

## Overview

Implement the missing write/network cmdlets to cover the complete git workflow:
**clone → branch → stage → status/diff → commit → push/pull**.

The reference for naming and behavior is [felixfbecker/PowerGit](https://github.com/felixfbecker/PowerGit).

### Currently Implemented

| Cmdlet | Verb-Noun | Operation |
|---|---|---|
| `GetGitBranchCmdlet` | `Get-GitBranch` | List branches |
| `SwitchGitBranchCmdlet` | `Switch-GitBranch` | Checkout branch |
| `GetGitStatusCmdlet` | `Get-GitStatus` | Working tree / index status |
| `GetGitDiffCmdlet` | `Get-GitDiff` | Unstaged / staged diff |
| `GetGitLogCmdlet` | `Get-GitLog` | Commit history |
| `GetGitTagCmdlet` | `Get-GitTag` | List tags |

### Existing Architecture

- **Abstractions layer** (`PowerCode.Git.Abstractions`): Interfaces in `Services/`, POCOs in `Models/`.
- **Core layer** (`PowerCode.Git.Core`): Service implementations using LibGit2Sharp.
- **Cmdlet layer** (`PowerCode.Git`): PSCmdlet subclasses in `Cmdlets/`, tab completers in `Completers/`.
- **Base class**: `GitCmdlet : PSCmdlet` — provides `RepoPath` parameter and `ResolveRepositoryPath()`.
- **DI pattern**: Two constructors per cmdlet — public default calls `ServiceFactory.Create*()`, internal accepts interface (unit tests).
- **Error handling**: Wrap exceptions in `ErrorRecord` with string error ID and `ErrorCategory.InvalidOperation`.
- **Conventions**: C# 14, file-scoped namespaces, XML doc on all public members, `is null` not `== null`, `[ValidateNotNullOrEmpty]` on required parameters.

---

## Step 1: `Copy-GitRepository` (git clone)

### Abstractions

**New model** `GitCloneOptions.cs` in `src/PowerCode.Git.Abstractions/Models/`:
```csharp
namespace PowerCode.Git.Abstractions.Models;

/// <summary>Options for cloning a git repository.</summary>
public sealed class GitCloneOptions
{
    /// <summary>The remote URL to clone from.</summary>
    public required string Url { get; init; }

    /// <summary>The local directory to clone into. If null, derived from the URL.</summary>
    public string? LocalPath { get; init; }

    /// <summary>Optional username for HTTP authentication.</summary>
    public string? CredentialUsername { get; init; }

    /// <summary>Optional password for HTTP authentication.</summary>
    public string? CredentialPassword { get; init; }

    /// <summary>Whether to clone only the default branch (--single-branch).</summary>
    public bool SingleBranch { get; init; }
}
```

**New interface** `IGitCloneService.cs` in `src/PowerCode.Git.Abstractions/Services/`:
```csharp
namespace PowerCode.Git.Abstractions.Services;

/// <summary>Service for cloning git repositories.</summary>
public interface IGitCloneService
{
    /// <summary>Clones a remote repository to a local path.</summary>
    /// <param name="options">Clone options including URL and local path.</param>
    /// <param name="onProgress">Optional callback receiving progress percentage (0–100) and message.</param>
    /// <returns>The resolved absolute path of the cloned repository.</returns>
    string Clone(GitCloneOptions options, Action<int, string>? onProgress = null);
}
```

### Core Implementation

**New file** `GitCloneService.cs` in `src/PowerCode.Git.Core/Services/`:
- Use `LibGit2Sharp.Repository.Clone(url, localPath, cloneOptions)`.
- Derive `localPath` from the URL (last path segment minus `.git`) if `options.LocalPath` is null.
- Wire up `CloneOptions.OnCheckoutProgress` and `CloneOptions.OnTransferProgress` to invoke `onProgress`.
- If credentials supplied, set `CloneOptions.CredentialsProvider` to return `UsernamePasswordCredentials`.
- Return the resolved absolute path.

### Cmdlet

**New file** `CopyGitRepositoryCmdlet.cs` in `src/PowerCode.Git/Cmdlets/`:
```
[Cmdlet(VerbsCommon.Copy, "GitRepository", SupportsShouldProcess = true)]
[OutputType(typeof(string))]
```
- `-Url` (mandatory, position 0, `[ValidateNotNullOrEmpty]`)
- `-LocalPath` (position 1, optional)
- `-Credential` (`PSCredential`, optional)
- `-SingleBranch` (switch)
- Does NOT inherit from `GitCmdlet` (no existing repo context needed).
- In `ProcessRecord`: call `ShouldProcess(Url, "Clone")`, convert `PSCredential` to username/password, call service, `WriteProgress` from callback, `WriteObject(resultPath)`.

### Tests

- **Unit test** `CopyGitRepositoryCmdletTests.cs` in `tests/PowerCode.Git.Tests/Cmdlets/`: Mock `IGitCloneService`, verify parameters forwarded correctly.
- **Core test** `GitCloneServiceTests.cs` in `tests/PowerCode.Git.Core.Tests/Services/`: Integration test that clones a small test repo to a temp directory (or uses `InitializeTestRepo` pattern).
- **System test** `Copy-GitRepository.Tests.ps1` in `tests/PowerCode.Git.SystemTests/`: End-to-end clone of a small public repo, verify directory created and contains `.git`.

---

## Step 2: `New-GitBranch` (git checkout -b)

### Abstractions

**Extend** `IGitBranchService` in `src/PowerCode.Git.Abstractions/Services/IGitBranchService.cs`:
```csharp
/// <summary>Creates a new branch at the current HEAD and checks it out.</summary>
/// <param name="repositoryPath">Path to the repository.</param>
/// <param name="name">Name of the new branch.</param>
/// <returns>Info about the newly created branch.</returns>
GitBranchInfo CreateBranch(string repositoryPath, string name);
```

### Core Implementation

**Extend** `GitBranchService` in `src/PowerCode.Git.Core/Services/GitBranchService.cs`:
- `repo.CreateBranch(name)` then `Commands.Checkout(repo, branch)`.
- Return a `GitBranchInfo` populated from the new branch.

### Cmdlet

**New file** `NewGitBranchCmdlet.cs` in `src/PowerCode.Git/Cmdlets/`:
```
[Cmdlet(VerbsCommon.New, "GitBranch", SupportsShouldProcess = true)]
[OutputType(typeof(GitBranchInfo))]
```
- Inherits `GitCmdlet`.
- `-Name` (mandatory, position 0, `[ValidateNotNullOrEmpty]`).
- In `ProcessRecord`: `ShouldProcess(Name, "Create branch")`, call service, `WriteObject(result)`.
- Output supports pipeline to `Send-GitBranch`.

### Tests

- **Unit test** `NewGitBranchCmdletTests.cs`: Verify `CreateBranch` called with correct args, result written to pipeline.
- **Core test**: `CreateBranch_NewBranch_CreatesAndChecksOut` — init test repo, create branch, verify HEAD points to it.

---

## Step 3: `Remove-GitBranch` (git branch -d / -D)

### Abstractions

**Extend** `IGitBranchService`:
```csharp
/// <summary>Deletes a branch.</summary>
/// <param name="repositoryPath">Path to the repository.</param>
/// <param name="name">Name of the branch to delete.</param>
/// <param name="force">If true, force-delete even if not fully merged.</param>
void DeleteBranch(string repositoryPath, string name, bool force = false);
```

### Core Implementation

- `repo.Branches.Remove(name)` — if not force and not merged, throw descriptive error.

### Cmdlet

**New file** `RemoveGitBranchCmdlet.cs`:
```
[Cmdlet(VerbsCommon.Remove, "GitBranch", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(void))]
```
- `-Name` (mandatory, position 0, `[GitBranchCompleter]`, accepts pipeline by property name).
- `-Force` switch.
- Supports pipeline input from `Get-GitBranch`.

### Tests

- Verify `ShouldProcess` is called.
- Verify pipeline input of multiple branches.

---

## Step 4: `Add-GitItem` (git add)

### Abstractions

**New model** `GitStageOptions.cs` in `Models/`:
```csharp
public sealed class GitStageOptions
{
    public required string RepositoryPath { get; init; }
    public IReadOnlyList<string>? Paths { get; init; }
    public bool All { get; init; }
}
```

**New interface** `IGitStagingService.cs` in `Services/`:
```csharp
public interface IGitStagingService
{
    /// <summary>Stages files for the next commit.</summary>
    void Stage(GitStageOptions options);

    /// <summary>Unstages files (inverse of Stage).</summary>
    void Unstage(string repositoryPath, IReadOnlyList<string>? paths = null);
}
```

### Core Implementation

**New file** `GitStagingService.cs` in `src/PowerCode.Git.Core/Services/`:
- `Stage`: If `All`, call `Commands.Stage(repo, "*")`. Otherwise iterate `Paths` and call `Commands.Stage(repo, path)`.
- `Unstage`: Call `Commands.Unstage(repo, path)` for each path, or all if paths is null.

### Cmdlet

**New file** `AddGitItemCmdlet.cs`:
```
[Cmdlet(VerbsCommon.Add, "GitItem", SupportsShouldProcess = true)]
```
- `-Path` (position 0, `string[]`, `[GitPathCompleter]`, pipeline by value).
- `-All` switch — validates mutually exclusive with `-Path`.
- No output by default.

### Tests

- Unit: Verify correct `StageOptions` constructed.
- Core: Init repo with untracked file, stage it, verify status shows staged.
- System: `Add-GitItem.Tests.ps1` — create file, `Add-GitItem`, check `Get-GitStatus`.

---

## Step 5: `Reset-GitHead` (git reset)

### Abstractions

**New enum** `GitResetMode.cs` in `Models/`:
```csharp
public enum GitResetMode { Mixed, Soft, Hard }
```

**Extend** `IGitStagingService` (or create `IGitResetService` if preferred):
```csharp
public interface IGitResetService
{
    /// <summary>Resets HEAD to the given revision with the specified mode.</summary>
    void Reset(string repositoryPath, string? revision, GitResetMode mode);
}
```

### Core Implementation

- Map `GitResetMode` to `LibGit2Sharp.ResetMode`.
- If `revision` is null, reset to HEAD (unstage all → `Mixed` mode).
- Otherwise, `repo.Reset(mode, repo.Lookup<Commit>(revision))`.

### Cmdlet

**New file** `ResetGitHeadCmdlet.cs`:
```
[Cmdlet(VerbsCommon.Reset, "GitHead", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
```
- `-Revision` (position 0, optional, `[GitBranchCompleter]`).
- `-Hard` switch, `-Soft` switch (parameter sets: `HardSet`, `SoftSet`, default is `Mixed`).
- High confirm impact only when `-Hard` is used.

### Tests

- Unit: Verify reset mode mapping.
- Core: Stage file, `Reset Mixed`, verify unstaged. Commit, `Reset Hard origin/master`, verify HEAD moved.

---

## Step 6: `Save-GitCommit` (git commit)

### Abstractions

**New model** `GitCommitOptions.cs` in `Models/`:
```csharp
public sealed class GitCommitOptions
{
    public required string RepositoryPath { get; init; }
    public string? Message { get; init; }
    public bool Amend { get; init; }
    public bool AllowEmpty { get; init; }
}
```

**New interface** `IGitCommitService.cs` in `Services/`:
```csharp
public interface IGitCommitService
{
    /// <summary>Creates a commit from the current index.</summary>
    /// <returns>Info about the created commit.</returns>
    GitCommitInfo Commit(GitCommitOptions options);
}
```

### Core Implementation

**New file** `GitCommitService.cs`:
- Read `user.name` and `user.email` from repo config to build `Signature`.
- If `Amend`: use `repo.Commit(message ?? existingMessage, author, committer, new CommitOptions { AmendPreviousCommit = true })`.
- If `AllowEmpty`: set `CommitOptions.AllowEmptyCommit = true`.
- If index is empty and not `AllowEmpty` and not `Amend`: throw descriptive error.
- Map the resulting `LibGit2Sharp.Commit` to `GitCommitInfo`.

### Cmdlet

**New file** `SaveGitCommitCmdlet.cs`:
```
[Cmdlet(VerbsData.Save, "GitCommit", SupportsShouldProcess = true)]
[OutputType(typeof(GitCommitInfo))]
[Alias("sgc")]
```
- `-Message` / `-m` (position 0, optional — if omitted and not amending, use `Host.UI.ReadLine` to prompt).
- `-Amend` switch.
- `-AllowEmpty` switch.
- Output: `GitCommitInfo` of the new commit.

### Tests

- Unit: Verify options mapped. Verify prompt happens when message is null.
- Core: Init repo, stage file, commit, verify `GitCommitInfo` has correct message. Test amend. Test empty commit error.
- System: `Save-GitCommit.Tests.ps1`.

---

## Step 7: `Send-GitBranch` (git push)

### Abstractions

**New model** `GitPushOptions.cs` in `Models/`:
```csharp
public sealed class GitPushOptions
{
    public required string RepositoryPath { get; init; }
    public string RemoteName { get; init; } = "origin";
    public string? BranchName { get; init; }
    public bool SetUpstream { get; init; }
    public string? CredentialUsername { get; init; }
    public string? CredentialPassword { get; init; }
}
```

**New interface** `IGitPushService.cs` in `Services/`:
```csharp
public interface IGitPushService
{
    /// <summary>Pushes a branch to a remote.</summary>
    /// <returns>Updated branch info after push.</returns>
    GitBranchInfo Push(GitPushOptions options, Action<int, string>? onProgress = null);
}
```

### Core Implementation

**New file** `GitPushService.cs`:
- Resolve branch: if `BranchName` is null, use `repo.Head`.
- Get remote: `repo.Network.Remotes[options.RemoteName]`.
- Build `LibGit2Sharp.PushOptions` with `CredentialsProvider` and `OnPushTransferProgress`.
- Call `repo.Network.Push(remote, branch.CanonicalName, pushOptions)`.
- If `SetUpstream`: set tracking branch via `repo.Branches.Update(branch, b => b.TrackedBranch = ...)`.
- Return updated `GitBranchInfo`.

### Cmdlet

**New file** `SendGitBranchCmdlet.cs`:
```
[Cmdlet(VerbsCommunications.Send, "GitBranch", SupportsShouldProcess = true)]
[OutputType(typeof(GitBranchInfo))]
```
- `-Remote` (position 0, default `"origin"`).
- `-Name` (position 1, `[GitBranchCompleter]`, pipeline by property name `Name`).
- `-SetUpstream` / `-u` alias switch.
- `-Credential` (`PSCredential`).
- Accepts pipeline from `Get-GitBranch` / `New-GitBranch`.
- Reports progress via `WriteProgress`.

### Tests

- Unit: Verify options mapping, pipeline binding from `GitBranchInfo`.
- Core: Requires a remote — test against a local bare repo created in temp dir.
- System: `Send-GitBranch.Tests.ps1`.

---

## Step 8: `Receive-GitBranch` (git pull)

### Abstractions

**New enum** `GitMergeStrategy.cs` in `Models/`:
```csharp
public enum GitMergeStrategy { Merge, FastForward, Rebase }
```

**New model** `GitPullOptions.cs` in `Models/`:
```csharp
public sealed class GitPullOptions
{
    public required string RepositoryPath { get; init; }
    public string RemoteName { get; init; } = "origin";
    public GitMergeStrategy MergeStrategy { get; init; } = GitMergeStrategy.Merge;
    public bool Prune { get; init; }
    public string? CredentialUsername { get; init; }
    public string? CredentialPassword { get; init; }
}
```

**New interface** `IGitPullService.cs` in `Services/`:
```csharp
public interface IGitPullService
{
    /// <summary>Pulls remote changes into the current branch.</summary>
    /// <returns>The merge result commit info.</returns>
    GitCommitInfo Pull(GitPullOptions options, Action<int, string>? onProgress = null);
}
```

### Core Implementation

**New file** `GitPullService.cs`:
- Read signature from repo config.
- Build `PullOptions` with `MergeOptions.FastForwardStrategy` mapped from `GitMergeStrategy`.
- If `Prune`: set `FetchOptions.Prune = true`.
- Call `Commands.Pull(repo, signature, pullOptions)`.
- Map the resulting merge commit to `GitCommitInfo`.

### Cmdlet

**New file** `ReceiveGitBranchCmdlet.cs`:
```
[Cmdlet(VerbsCommunications.Receive, "GitBranch")]
[OutputType(typeof(GitCommitInfo))]
```
- `-MergeStrategy` (`GitMergeStrategy`, default `Merge`).
- `-Prune` switch.
- `-Credential` (`PSCredential`).
- Reports progress via `WriteProgress`.

### Tests

- Core: Two local repos, push from one, pull from other.
- System: `Receive-GitBranch.Tests.ps1`.

---

## Step 9: Service Registration

**File** `src/PowerCode.Git/DependencyContext.cs` (or `ServiceFactory`):

Register all new services following the existing pattern:
```
IGitCloneService   → GitCloneService
IGitStagingService → GitStagingService
IGitResetService   → GitResetService
IGitCommitService  → GitCommitService
IGitPushService    → GitPushService
IGitPullService    → GitPullService
```

Ensure they are created within the `PowerCodeGitDependencyLoadContext` so LibGit2Sharp assemblies resolve correctly.

---

## Step 10: Module Manifest & Tab Completers

- Update module manifest (if generated from `Update-PowerCodeGitManifest.ps1`) to export the new cmdlets.
- Ensure `[GitBranchCompleter]` works on the new `-Name` / `-Revision` parameters (it already exists).
- Ensure `[GitPathCompleter]` works on `Add-GitItem -Path` (it already exists).

---

## Implementation Order (Recommended)

Execute in this order to build incrementally testable functionality:

1. **`Add-GitItem`** — simplest write operation, immediately testable with existing `Get-GitStatus`
2. **`Save-GitCommit`** — depends on staging; testable with `Get-GitLog`
3. **`New-GitBranch`** — extends existing service; testable with `Get-GitBranch`
4. **`Remove-GitBranch`** — extends existing service; testable with `Get-GitBranch`
5. **`Reset-GitHead`** — testable with `Get-GitStatus` and `Get-GitLog`
6. **`Copy-GitRepository`** — standalone; testable independently
7. **`Send-GitBranch`** — requires remote; test with bare local repo
8. **`Receive-GitBranch`** — requires remote; test with bare local repo

---

## Verification Checklist

- [ ] `dotnet build` succeeds for all projects
- [ ] `dotnet test` passes for `PowerCode.Git.Tests` and `PowerCode.Git.Core.Tests`
- [ ] `Invoke-SystemTests.ps1` passes all system tests
- [ ] Full workflow script runs end-to-end:
  ```powershell
  Copy-GitRepository https://github.com/example/repo ./test-repo
  Set-Location ./test-repo
  New-GitBranch feature/test
  "hello" | Set-Content ./newfile.txt
  Get-GitStatus
  Get-GitDiff
  Add-GitItem ./newfile.txt
  Get-GitStatus   # shows staged
  Save-GitCommit -Message "Add newfile"
  Get-GitLog -MaxCount 1   # shows new commit
  Send-GitBranch -SetUpstream
  ```
- [ ] All new cmdlets appear in `(Get-Module PowerCode.Git).ExportedCommands`
- [ ] Tab completion works on all `[GitBranchCompleter]` and `[GitPathCompleter]` parameters

---

## Out of Scope (Future Work)

- `Receive-GitObject` (git fetch without merge)
- `Merge-GitCommit` (explicit merge)
- `Start-GitRebase` / `Resume-GitRebase` / `Stop-GitRebase`
- `Compare-GitTree` (tree diff between two revisions)
- `Set-GitConfiguration` (git config)
- `Get-GitStatusPrompt` (prompt integration)
- SSH authentication support
