# Implementation Instructions: Add Git Parameter Sets to PowerShell Commands

## References

- **Plan**: `docs/plan-gitParameterSets.prompt.md`
- **Git docs**: `file:///C:/Program%20Files/Git/mingw64/share/doc/git-doc/git-<command>.html`
- **Instructions**: `.github/instructions/assembly-load-context.instructions.md`, `csharp.instructions.md`, `powershell.instructions.md`

## Key Conventions (extracted from codebase)

### Model Pattern
Options classes in `src/PowerCode.Git.Abstractions/Models/` use one of two styles:
- **`required`/`init` props** — for options with a required `RepositoryPath`: `public required string RepositoryPath { get; init; }`
- **`get`/`set` props** — for classes like `GitLogOptions`, `GitStatusOptions`, `GitDiffOptions` that use `get; set;` with defaults

New options classes should use `required`/`init` for mandatory fields, `get; set;` for optional filter-style options. Include XML doc on every public member. Include an `override string ToString()` method. Namespace: `PowerCode.Git.Abstractions.Models`.

### Interface Pattern
Interfaces in `src/PowerCode.Git.Abstractions/Services/` use only BCL and Abstractions types. **Never** expose LibGit2Sharp types. Prefer passing an options object over multiple individual parameters.

When migrating from `Method(string repositoryPath, ...)` to `Method(XxxOptions options)`:
- **Keep the old overload** as a default-interface method forwarding to the new one OR
- **Replace** the signature and update all callers (cmdlet + stub in tests + service impl)

### Service Implementation Pattern
In `src/PowerCode.Git.Core/Services/`: public parameterless constructor, validate via `RepositoryGuard`, use LibGit2Sharp freely, map to Abstractions DTOs before returning.

### Cmdlet Pattern
In `src/PowerCode.Git/Cmdlets/`:
```csharp
[Cmdlet(VerbsXxx.Yyy, "GitZzz", SupportsShouldProcess = true, DefaultParameterSetName = "DefaultSetName")]
[OutputType(typeof(SomeDto))]
public sealed class YyyGitZzzCmdlet : GitCmdlet
{
    public YyyGitZzzCmdlet() : this(ServiceFactory.CreateGitXxxService()) { }
    internal YyyGitZzzCmdlet(IGitXxxService service) { this.service = service ?? throw new ArgumentNullException(nameof(service)); }
    private readonly IGitXxxService service;

    // Parameters with [Parameter(ParameterSetName = "...")] attributes
    // SwitchParameters for booleans
    // [ValidateNotNullOrEmpty], completers as appropriate

    protected override void ProcessRecord()
    {
        // Build options from parameters (or use Options parameter directly)
        // ShouldProcess check for mutations
        // try/catch → WriteError with ErrorRecord
        // WriteObject for output
    }
}
```

### Catch-All Options Parameter
Every cmdlet gets an `Options` parameter set:
```csharp
[Parameter(Mandatory = true, ParameterSetName = "Options")]
public GitXxxOptions Options { get; set; } = null!;
```
In `ProcessRecord`, check `ParameterSetName == "Options"` and use `Options` directly, otherwise build `Options` from individual parameters.

### Unit Test Pattern (MSTest, stubs, no Moq)
In `tests/PowerCode.Git.Tests/Cmdlets/`:
- `[TestClass] public sealed class XxxTests`
- Test method naming: `MethodName_Condition_ExpectedResult()`
- No "Arrange/Act/Assert" comments
- Hand-written `private sealed class StubGitXxxService : IGitXxxService` inner classes
- Test `BuildOptions` mapping or individual property defaults
- For cmdlets that build options: add an `internal XxxOptions BuildOptions(string currentFileSystemPath)` method and test it directly

### Integration Test Pattern (MSTest, real repos)
In `tests/PowerCode.Git.Core.Tests/Services/`:
- Create real git repos via LibGit2Sharp in test methods
- Clean up with `DeleteDirectory` in `finally` blocks (handles read-only via retry)
- `[assembly: DoNotParallelize]` in `MSTestSettings.cs`
- `private static string CreateRepositoryWith...()` helper methods

### System Test Pattern (Pester 5)
In `tests/PowerCode.Git.SystemTests/`:
```powershell
#Requires -Modules Pester
BeforeAll { . "$PSScriptRoot/SystemTest-Helpers.ps1" }
AfterAll { Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue }

Describe 'Command-Name scenario' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }
    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }
    It 'Does something' {
        $Result = Command-Name -RepoPath $script:RepoPath -Param value
        $Result | Should -Not -BeNullOrEmpty
    }
}
```
Error tests use: `-ErrorVariable GitErrors -ErrorAction SilentlyContinue`

### Build & Test Commands
```powershell
# Build
dotnet build PowerCode.Git.slnx

# Unit + integration tests
dotnet test PowerCode.Git.slnx

# System tests (requires module build first)
dotnet build PowerCode.Git.slnx
Invoke-Pester -Path tests/PowerCode.Git.SystemTests/ -Output Detailed
```

### Commit Convention
```
feat(branch): add parameter sets to Get-GitBranch

- Add GitBranchListOptions model with Remote, All, Pattern, Contains, Merged, NoMerged
- Update IGitBranchService.GetBranches to accept GitBranchListOptions
- Implement filtering in GitBranchService
- Add List and Options parameter sets to GetGitBranchCmdlet
- Add unit tests for option building and parameter defaults
- Add system tests for -Remote, -All, -Pattern flags
```

---

## Command 1: Get-GitBranch

### Step 1.1: Create `GitBranchListOptions` model

**File**: `src/PowerCode.Git.Abstractions/Models/GitBranchListOptions.cs`

```csharp
namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for listing branches in a git repository.
/// </summary>
public sealed class GitBranchListOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to list only remote-tracking branches
    /// (equivalent to <c>git branch -r</c>).
    /// </summary>
    public bool ListRemote { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to list both local and remote-tracking
    /// branches (equivalent to <c>git branch -a</c>).
    /// </summary>
    public bool ListAll { get; set; }

    /// <summary>
    /// Gets or sets a glob pattern to filter branch names (equivalent to
    /// <c>git branch -l &lt;pattern&gt;</c>).
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets a committish to filter branches that contain the specified commit
    /// (equivalent to <c>git branch --contains &lt;commit&gt;</c>).
    /// </summary>
    public string? ContainsCommit { get; set; }

    /// <summary>
    /// Gets or sets a committish to filter only branches whose tips are reachable from
    /// the specified commit (equivalent to <c>git branch --merged [&lt;commit&gt;]</c>).
    /// When set to an empty string or the value <c>"HEAD"</c>, HEAD is used.
    /// </summary>
    public string? MergedInto { get; set; }

    /// <summary>
    /// Gets or sets a committish to filter only branches whose tips are NOT reachable from
    /// the specified commit (equivalent to <c>git branch --no-merged [&lt;commit&gt;]</c>).
    /// </summary>
    public string? NotMergedInto { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (ListRemote) parts.Add("remote");
        if (ListAll) parts.Add("all");
        if (Pattern is not null) parts.Add($"pattern={Pattern}");
        if (ContainsCommit is not null) parts.Add($"contains={ContainsCommit}");
        if (MergedInto is not null) parts.Add($"merged={MergedInto}");
        if (NotMergedInto is not null) parts.Add($"no-merged={NotMergedInto}");
        return parts.Count > 0
            ? $"GitBranchListOptions({string.Join(", ", parts)})"
            : "GitBranchListOptions()";
    }
}
```

### Step 1.2: Update `IGitBranchService`

**File**: `src/PowerCode.Git.Abstractions/Services/IGitBranchService.cs`

Add a new overload while keeping the old one as a default-interface method:
```csharp
/// <summary>
/// Gets branches in the repository filtered by the specified options.
/// </summary>
IReadOnlyList<GitBranchInfo> GetBranches(GitBranchListOptions options);

/// <summary>
/// Gets all branches in the repository.
/// </summary>
IReadOnlyList<GitBranchInfo> GetBranches(string repositoryPath)
    => GetBranches(new GitBranchListOptions { RepositoryPath = repositoryPath });
```

Make the `string` overload a default-interface method that forwards to the options overload. This way existing code (completers, etc.) still compiles. The service implementation only needs to implement the options-based overload.

### Step 1.3: Update `GitBranchService`

**File**: `src/PowerCode.Git.Core/Services/GitBranchService.cs`

Replace the current `GetBranches(string)` with `GetBranches(GitBranchListOptions)`:

```csharp
public IReadOnlyList<GitBranchInfo> GetBranches(GitBranchListOptions options)
{
    RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

    using var repository = new Repository(options.RepositoryPath);

    IEnumerable<Branch> branches = repository.Branches;

    // Filter by local/remote/all
    if (options.ListRemote)
    {
        branches = branches.Where(b => b.IsRemote);
    }
    else if (!options.ListAll)
    {
        branches = branches.Where(b => !b.IsRemote);
    }
    // else: ListAll — include both

    var result = branches.Select(MapBranch).ToList();

    // Pattern filter (glob-like: supports * and ?)
    if (options.Pattern is not null)
    {
        var regex = GlobToRegex(options.Pattern);
        result = result.Where(b => regex.IsMatch(b.Name)).ToList();
    }

    // --contains: only branches whose tip is a descendant of the given commit
    if (options.ContainsCommit is not null)
    {
        var commit = repository.Lookup<Commit>(options.ContainsCommit)
            ?? throw new ArgumentException($"Commit '{options.ContainsCommit}' was not found.");
        result = result.Where(b =>
        {
            var branchTip = repository.Branches[b.Name]?.Tip;
            return branchTip is not null &&
                   (branchTip.Sha == commit.Sha ||
                    repository.ObjectDatabase.FindMergeBase(branchTip, commit)?.Sha == commit.Sha);
        }).ToList();
    }

    // --merged: only branches fully merged into the reference commit
    if (options.MergedInto is not null)
    {
        var reference = ResolveCommitOrHead(repository, options.MergedInto);
        result = result.Where(b =>
        {
            var branchTip = repository.Branches[b.Name]?.Tip;
            if (branchTip is null) return true;
            var mergeBase = repository.ObjectDatabase.FindMergeBase(reference, branchTip);
            return mergeBase?.Sha == branchTip.Sha;
        }).ToList();
    }

    // --no-merged: only branches NOT fully merged
    if (options.NotMergedInto is not null)
    {
        var reference = ResolveCommitOrHead(repository, options.NotMergedInto);
        result = result.Where(b =>
        {
            var branchTip = repository.Branches[b.Name]?.Tip;
            if (branchTip is null) return false;
            var mergeBase = repository.ObjectDatabase.FindMergeBase(reference, branchTip);
            return mergeBase?.Sha != branchTip.Sha;
        }).ToList();
    }

    return result;
}

private static Commit ResolveCommitOrHead(Repository repo, string committish)
{
    if (string.IsNullOrWhiteSpace(committish) || committish == "HEAD")
        return repo.Head.Tip ?? throw new InvalidOperationException("Repository has no commits.");
    return repo.Lookup<Commit>(committish)
        ?? throw new ArgumentException($"Commit '{committish}' was not found.");
}

private static System.Text.RegularExpressions.Regex GlobToRegex(string pattern)
{
    var escaped = System.Text.RegularExpressions.Regex.Escape(pattern)
        .Replace("\\*", ".*")
        .Replace("\\?", ".");
    return new System.Text.RegularExpressions.Regex(
        $"^{escaped}$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
```

### Step 1.4: Update `GetGitBranchCmdlet`

**File**: `src/PowerCode.Git/Cmdlets/GetGitBranchCmdlet.cs`

```csharp
[Cmdlet(VerbsCommon.Get, "GitBranch", DefaultParameterSetName = "List")]
[OutputType(typeof(GitBranchInfo))]
public sealed class GetGitBranchCmdlet : GitCmdlet
{
    // ... constructors unchanged ...

    /// <summary>
    /// Gets or sets a value indicating whether to list only remote-tracking branches.
    /// </summary>
    [Parameter(ParameterSetName = "List")]
    public SwitchParameter Remote { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to list both local and remote branches.
    /// </summary>
    [Parameter(ParameterSetName = "List")]
    [Alias("a")]
    public SwitchParameter All { get; set; }

    /// <summary>
    /// Gets or sets a glob pattern to filter branch names.
    /// </summary>
    [Parameter(ParameterSetName = "List")]
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets a committish; only branches containing this commit are shown.
    /// </summary>
    [Parameter(ParameterSetName = "List")]
    [GitCommittishCompleter]
    public string? Contains { get; set; }

    /// <summary>
    /// Gets or sets a committish; only branches merged into this commit are shown.
    /// </summary>
    [Parameter(ParameterSetName = "List")]
    [GitCommittishCompleter]
    public string? Merged { get; set; }

    /// <summary>
    /// Gets or sets a committish; only branches NOT merged into this commit are shown.
    /// </summary>
    [Parameter(ParameterSetName = "List")]
    [GitCommittishCompleter]
    public string? NoMerged { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object for full control.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitBranchListOptions Options { get; set; } = null!;

    protected override void ProcessRecord()
    {
        try
        {
            var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);
            var branches = branchService.GetBranches(options);
            foreach (var branch in branches)
            {
                WriteObject(branch);
            }
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(exception, "GetGitBranchFailed",
                ErrorCategory.InvalidOperation, RepoPath));
        }
    }

    internal GitBranchListOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == "Options")
        {
            return Options;
        }

        return new GitBranchListOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            ListRemote = Remote.IsPresent,
            ListAll = All.IsPresent,
            Pattern = Pattern,
            ContainsCommit = Contains,
            MergedInto = Merged,
            NotMergedInto = NoMerged,
        };
    }
}
```

### Step 1.5: Update unit tests

**File**: `tests/PowerCode.Git.Tests/Cmdlets/GetGitBranchCmdletTests.cs`

Add tests for `BuildOptions`:
- `BuildOptions_NoParameters_DefaultListOptions()` — verify all filter properties are null/false
- `BuildOptions_RemoteSet_ListRemoteIsTrue()`
- `BuildOptions_AllSet_ListAllIsTrue()`
- `BuildOptions_PatternSet_PatternMapped()`
- `BuildOptions_ContainsSet_ContainsCommitMapped()`
- `BuildOptions_MergedSet_MergedIntoMapped()`
- `BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()`

Update `StubGitBranchService` to implement the new `GetBranches(GitBranchListOptions)` overload.

### Step 1.6: Update integration tests

**File**: `tests/PowerCode.Git.Core.Tests/Services/GitBranchServiceTests.cs`

Add tests:
- `GetBranches_ListRemoteOnly_ReturnsNoLocalBranches()`
- `GetBranches_ListAll_ReturnsLocalAndRemoteBranches()`
- `GetBranches_PatternFilter_MatchesGlob()`
- `GetBranches_MergedFilter_ReturnsOnlyMergedBranches()`
- `GetBranches_OptionsBasedOverload_WorksCorrectly()`

### Step 1.7: Update system tests

**File**: `tests/PowerCode.Git.SystemTests/Get-GitBranch.Tests.ps1`

Add `Describe` blocks:
```powershell
Describe 'Get-GitBranch -All flag' {
    # Setup repo with remote, test -All returns remote branches too
}

Describe 'Get-GitBranch -Pattern filter' {
    # Setup repo with feature/* and bugfix/* branches, test -Pattern 'feature/*'
}

Describe 'Get-GitBranch -Merged filter' {
    # Create branch, merge it, verify -Merged shows it, -NoMerged excludes it
}

Describe 'Get-GitBranch -Options catch-all' {
    # Create GitBranchListOptions via [PowerCode.Git.Abstractions.Models.GitBranchListOptions]::new()
    # Pass it via -Options parameter
}
```

### Step 1.8: Build, test, commit

```powershell
dotnet build PowerCode.Git.slnx
dotnet test PowerCode.Git.slnx
Invoke-Pester -Path tests/PowerCode.Git.SystemTests/Get-GitBranch.Tests.ps1 -Output Detailed
```

Commit: `feat(branch): add parameter sets to Get-GitBranch`

---

## Command 2: New-GitBranch

### Step 2.1: Create `GitBranchCreateOptions` model

**File**: `src/PowerCode.Git.Abstractions/Models/GitBranchCreateOptions.cs`

Properties:
- `required string RepositoryPath { get; init; }`
- `required string Name { get; init; }`
- `string? StartPoint { get; init; }` — committish, defaults to HEAD if null
- `bool Track { get; init; }` — set upstream tracking
- `bool Force { get; init; }` — overwrite existing branch

### Step 2.2: Update `IGitBranchService`

Add overload: `GitBranchInfo CreateBranch(GitBranchCreateOptions options);`
Make old `CreateBranch(string, string)` a default-interface method forwarding to new one.

### Step 2.3: Update `GitBranchService`

Implement `CreateBranch(GitBranchCreateOptions)`:
- Resolve `StartPoint` → `repository.Lookup<Commit>(startPoint)` or HEAD
- If `Force` and branch exists: `repository.Branches.Remove(existing)` first, or use the force overload
- `repository.CreateBranch(name, target)`
- If `Track`: set tracking reference
- `Commands.Checkout` + return MapBranch

### Step 2.4: Update `NewGitBranchCmdlet`

Add parameters: `-StartPoint` (Position=1, `[GitCommittishCompleter]`), `-Track` (SwitchParameter), `-Force` (SwitchParameter)
Add `Options` parameter set with `GitBranchCreateOptions Options`.
Add `DefaultParameterSetName = "Create"`.
Add `internal GitBranchCreateOptions BuildOptions(string currentFsPath)`.

### Step 2.5–2.8: Tests + build + commit

Follow same pattern as Command 1. Commit: `feat(branch): add parameter sets to New-GitBranch`

---

## Command 3: Remove-GitBranch

### Step 3.1: Create `GitBranchDeleteOptions` model

Properties: `required string RepositoryPath { get; init; }`, `required string Name { get; init; }`, `bool Force { get; init; }`

### Step 3.2: Update `IGitBranchService`

Add: `void DeleteBranch(GitBranchDeleteOptions options);`
Make old `DeleteBranch(string, string, bool)` a default-interface method.

### Step 3.3–3.7: Service, cmdlet, tests

Add `DefaultParameterSetName = "Delete"`, add `Options` parameter set. Commit: `feat(branch): add parameter sets to Remove-GitBranch`

---

## Command 4: Switch-GitBranch

### Step 4.1: Create `GitSwitchOptions` model

Properties: `RepositoryPath` (required), `BranchName` (string?), `Create` (bool), `StartPoint` (string?), `Detach` (bool), `Committish` (string?), `Force` (bool)

### Step 4.2: Update `IGitBranchService`

Add: `GitBranchInfo SwitchBranch(GitSwitchOptions options);`
Make old `SwitchBranch(string, string)` a default-interface method.

### Step 4.3: Update `GitBranchService`

Implement:
- If `options.Create`: create branch at `StartPoint` then checkout
- If `options.Detach`: `Commands.Checkout(repository, commit)` for detached HEAD
- If `options.Force`: pass `CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force }`
- Default: standard branch checkout

### Step 4.4: Update `SwitchGitBranchCmdlet`

Parameter sets: `Switch` (default), `Create`, `Detach`, `Options`
- `Switch`: `-Name` (mandatory, pos 0), `-Force`
- `Create`: `-Create` (mandatory switch), `-Name` (mandatory, pos 0), `-StartPoint`, `-Force`
- `Detach`: `-Detach` (mandatory switch), `-Committish` (pos 0, `[GitCommittishCompleter]`)

Commit: `feat(branch): add parameter sets to Switch-GitBranch`

---

## Command 5: Get-GitStatus

### Step 5.1: Extend `GitStatusOptions`

Add: `string[]? Paths`, `GitUntrackedFilesMode? UntrackedFilesMode`

Create enum `GitUntrackedFilesMode` { No, Normal, All } in `Models/`.

### Step 5.2: Update `GitWorkingTreeService.GetStatus`

Filter entries by `options.Paths` (if specified).
Respect `UntrackedFilesMode`: `No` → exclude untracked; `Normal`/`All` → pass through.

### Step 5.3: Update `GetGitStatusCmdlet`

Add `-Path` (string[], `[GitPathCompleter]`), `-UntrackedFiles` (GitUntrackedFilesMode).
Add `Options` parameter set. Add `BuildOptions` method.

Commit: `feat(status): add parameter sets to Get-GitStatus`

---

## Command 6: Get-GitDiff

### Step 6.1: Extend `GitDiffOptions`

Add: `string? Commit`, `string? FromCommit`, `string? ToCommit`, `bool IgnoreWhitespace`

### Step 6.2: Update `GitWorkingTreeService.GetDiff`

Implement commit-based diff: `repository.Diff.Compare<Patch>(commitTree, DiffTargets.WorkingDirectory)`
Implement range diff: `repository.Diff.Compare<Patch>(fromTree, toTree)`
Apply `IgnoreWhitespace` via `CompareOptions { IgnoreWhitespace = true }`

### Step 6.3: Update `GetGitDiffCmdlet`

Parameter sets: `WorkingTree` (default), `Staged`, `Commit`, `Range`, `Options`
Update `BuildOptions` for each set.

Commit: `feat(diff): add parameter sets to Get-GitDiff`

---

## Command 7: Add-GitItem

### Step 7.1: Extend `GitStageOptions`

Add: `bool Update`, `bool Force`

### Step 7.2: Update `GitWorkingTreeService.Stage`

Handle `Update`: `Commands.Stage(repository, "*")` with `StageOptions { IncludeIgnored = false }` (only tracked)
Handle `Force`: staged with `Commands.Stage` allowing ignored files

### Step 7.3: Update `AddGitItemCmdlet`

Add `Update` parameter set, `Force` parameter (shared), `Options` parameter set.

Commit: `feat(stage): add parameter sets to Add-GitItem`

---

## Command 8: Reset-GitHead

### Step 8.1: Create `GitResetOptions` model

Properties: `required string RepositoryPath { get; init; }`, `string? Revision`, `GitResetMode Mode`, `IReadOnlyList<string>? Paths`

### Step 8.2: Update `IGitWorkingTreeService`

Add: `void Reset(GitResetOptions options);`
Make old `Reset(string, string?, GitResetMode)` a default-interface method.

### Step 8.3: Update service

Path-based reset: `Commands.Unstage(repository, path)` for each path (essentially `git reset -- <path>`)

### Step 8.4: Update cmdlet

Add `Paths` parameter set with `-Path` (string[], `[GitPathCompleter]`), add `Options` parameter set.

Commit: `feat(reset): add parameter sets to Reset-GitHead`

---

## Command 9: Get-GitLog

### Step 9.1: Extend `GitLogOptions`

Add: `bool FirstParent`, `bool NoMerges`

### Step 9.2: Update `GitHistoryService.GetLog`

Apply `FirstParent`: `filter.FirstParentOnly = true`
Apply `NoMerges`: `commits = commits.Where(c => c.Parents.Count() <= 1)`

### Step 9.3: Update `GetGitLogCmdlet`

Expose existing `AllBranches` (from model, not currently on cmdlet). Add `-FirstParent`, `-NoMerges`. Add `Options` parameter set. Update `BuildOptions`.

Commit: `feat(log): add parameter sets to Get-GitLog`

---

## Command 10: Save-GitCommit

### Step 10.1: Extend `GitCommitOptions`

Add: `bool All`, `string? Author`, `DateTimeOffset? Date`

### Step 10.2: Update `GitHistoryService.Commit`

When `options.All`: `Commands.Stage(repository, "*")` before committing (tracked files only)
When `options.Author`: parse "Name <email>" format, create custom `Signature`
When `options.Date`: use provided date in `Signature`

### Step 10.3: Update `SaveGitCommitCmdlet`

Add `-All` (alias "a"), `-Author`, `-Date` parameters. Add `Options` parameter set.

Commit: `feat(commit): add parameter sets to Save-GitCommit`

---

## Command 11: Get-GitTag

### Step 11.1: Create `GitTagListOptions` model

Properties: `string RepositoryPath`, `string? Pattern`, `string? SortBy`, `string? ContainsCommit`

### Step 11.2: Update `IGitTagService`

Add: `IReadOnlyList<GitTagInfo> GetTags(GitTagListOptions options);`
Make old `GetTags(string)` a default-interface method.

### Step 11.3–11.7: Service, cmdlet, tests

Pattern: filter by glob. Sort: by name or version. Contains: check commit ancestry.

Commit: `feat(tag): add parameter sets to Get-GitTag`

---

## Command 12: Copy-GitRepository

### Step 12.1: Extend `GitCloneOptions`

Add: `int? Depth`, `string? BranchName`, `bool Bare`, `bool RecurseSubmodules`

### Step 12.2: Update `GitRemoteService.Clone`

Pass to `CloneOptions`: `CloneOptions.IsBare = options.Bare`, branch checkout, depth (note: LibGit2Sharp depth support is limited — may need validation)

### Step 12.3: Update `CopyGitRepositoryCmdlet`

Add `-Depth`, `-Branch`, `-Bare`, `-RecurseSubmodules` parameters. Add `Options` parameter set.

Commit: `feat(clone): add parameter sets to Copy-GitRepository`

---

## Command 13: Send-GitBranch

### Step 13.1: Extend `GitPushOptions`

Add: `bool Force`, `bool ForceWithLease`, `bool Delete`, `bool Tags`, `bool All`, `bool DryRun`

### Step 13.2: Update `GitRemoteService.Push`

Handle `Force`: push with force refspec (`+refs/heads/...`)
Handle `Delete`: push with delete refspec (`:refs/heads/...`)
Handle `Tags`: push `refs/tags/*`
Handle `All`: push all branches

### Step 13.3: Update `SendGitBranchCmdlet`

Parameter sets: `Default`, `Delete`, `Tags`, `All`, `Options`.
Add `-Force`, `-ForceWithLease`, `-DryRun` in Default set.

Commit: `feat(push): add parameter sets to Send-GitBranch`

---

## Command 14: Receive-GitBranch

### Step 14.1: Extend `GitPullOptions`

Add: `bool AutoStash`, `bool? Tags`, `int? Depth`

### Step 14.2: Update `GitRemoteService.Pull`

Pass options through to LibGit2Sharp `PullOptions`/`FetchOptions`.

### Step 14.3: Update `ReceiveGitBranchCmdlet`

Add `-AutoStash`, `-Tags`, `-Depth` parameters. Add `Options` parameter set.

Commit: `feat(pull): add parameter sets to Receive-GitBranch`

---

## Important Notes

### Interface Migration Strategy
When changing `GetBranches(string)` → `GetBranches(GitBranchListOptions)`, use default-interface methods to maintain backward compatibility:
```csharp
IReadOnlyList<GitBranchInfo> GetBranches(GitBranchListOptions options);
IReadOnlyList<GitBranchInfo> GetBranches(string repositoryPath)
    => GetBranches(new GitBranchListOptions { RepositoryPath = repositoryPath });
```
This ensures completers (`GitBranchCompleterAttribute`) and other callers of the old signature keep working.

### Stub Updates in Tests
When a service interface changes, ALL stub classes in test files must be updated. Search for `StubGitXxxService` in test files and update them to implement new methods.

### The `ParameterSetName` Property at Runtime
In `ProcessRecord`, use `ParameterSetName` (inherited from `PSCmdlet`) to determine which parameter set was resolved. For the `Options` set, use the `Options` property directly. For other sets, build the options from individual parameters.

### No Breaking Changes to Existing Parameters
All existing cmdlet parameters must continue to work as before. New parameters are additive. The default parameter set should match the current behavior.

### ALC Boundary
Remember: new model types go in `Abstractions/Models/` (shared ALC). Service interface changes go in `Abstractions/Services/`. Implementation changes go in `Core/Services/` (isolated ALC). The cmdlet layer in `PowerCode.Git/` only references Abstractions types.
