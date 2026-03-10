using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Lists and switches git branches using LibGit2Sharp.
/// </summary>
public sealed class GitBranchService : IGitBranchService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitBranchInfo> GetBranches(GitBranchListOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        // Build a lookup of branch name → worktree path so each branch
        // can report which worktree (if any) it is currently checked out in.
        var worktreePaths = BuildWorktreeLookup(repository);

        // Build a lookup of branch name → description when requested.
        var descriptions = options.IncludeDescription
            ? BuildDescriptionLookup(repository)
            : null;

        // Resolve the reference branch tip once so each branch can be compared against it.
        string? referenceBranchName = options.ReferenceBranch;
        Commit? referenceTip = null;
        if (referenceBranchName is not null)
        {
            var refBranch = repository.Branches[referenceBranchName]
                ?? throw new ArgumentException(
                    $"Reference branch '{referenceBranchName}' was not found in the repository.",
                    nameof(options));
            referenceTip = refBranch.Tip
                ?? throw new ArgumentException(
                    $"Reference branch '{referenceBranchName}' has no commits.",
                    nameof(options));
        }

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
        // else: ListAll — include both local and remote

        var result = branches.Select(b =>
        {
            GitBranchComparisonInfo? comparison = null;
            if (referenceTip is not null && b.Tip is not null)
            {
                var divergence = repository.ObjectDatabase.CalculateHistoryDivergence(b.Tip, referenceTip);
                comparison = new GitBranchComparisonInfo(
                    referenceBranchName!,
                    divergence.AheadBy ?? 0,
                    divergence.BehindBy ?? 0);
            }

            return MapBranch(b, worktreePaths, comparison, descriptions);
        }).ToList();

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
                ?? throw new ArgumentException($"Commit '{options.ContainsCommit}' was not found.", nameof(options));
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
                if (branchTip is null)
                {
                    return true;
                }

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
                if (branchTip is null)
                {
                    return false;
                }

                var mergeBase = repository.ObjectDatabase.FindMergeBase(reference, branchTip);
                return mergeBase?.Sha != branchTip.Sha;
            }).ToList();
        }

        // Include filter: keep only branches matching at least one include pattern
        if (options.Include is { Length: > 0 })
        {
            var includeRegexes = options.Include.Select(GlobToRegex).ToArray();
            result = result.Where(b => includeRegexes.Any(r => r.IsMatch(b.Name))).ToList();
        }

        // Exclude filter: remove branches matching any exclude pattern
        if (options.Exclude is { Length: > 0 })
        {
            var excludeRegexes = options.Exclude.Select(GlobToRegex).ToArray();
            result = result.Where(b => !excludeRegexes.Any(r => r.IsMatch(b.Name))).ToList();
        }

        return result;
    }

    private static Commit ResolveCommitOrHead(Repository repo, string committish)
    {
        if (string.IsNullOrWhiteSpace(committish) || committish == "HEAD")
        {
            return repo.Head.Tip ?? throw new InvalidOperationException("Repository has no commits.");
        }

        return repo.Lookup<Commit>(committish)
            ?? throw new ArgumentException($"Commit '{committish}' was not found.");
    }

    private static Regex GlobToRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern)
            .Replace("\\*", ".*", StringComparison.Ordinal)
            .Replace("\\?", ".", StringComparison.Ordinal);
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase);
    }

    /// <inheritdoc/>
    public GitBranchInfo SwitchBranch(GitSwitchOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var checkoutOptions = new CheckoutOptions
        {
            CheckoutModifiers = options.Force
                ? CheckoutModifiers.Force
                : CheckoutModifiers.None,
        };

        if (options.Detach)
        {
            var committish = options.Committish
                ?? throw new ArgumentException("Committish is required when Detach is true.", nameof(options));
            var commit = repository.Lookup<Commit>(committish)
                ?? throw new ArgumentException($"Committish '{committish}' was not found.", nameof(options));
            Commands.Checkout(repository, commit, checkoutOptions);

            // Return a synthetic "detached HEAD" branch info
            return new GitBranchInfo(
                commit.Sha[..7],
                true,
                false,
                commit.Sha,
                null,
                null,
                null);
        }

        if (options.Create)
        {
            if (options.BranchName is null)
                throw new ArgumentException("BranchName is required when Create is true.", nameof(options));
            RepositoryGuard.ValidateRequiredString(options.BranchName, nameof(options), "BranchName is required when Create is true.");

            Branch newBranch;
            if (options.StartPoint is not null)
            {
                var startCommit = ResolveCommitOrHead(repository, options.StartPoint);
                newBranch = repository.CreateBranch(options.BranchName!, startCommit);
            }
            else
            {
                newBranch = repository.CreateBranch(options.BranchName!);
            }

            Commands.Checkout(repository, newBranch, checkoutOptions);
            var updatedNewBranch = repository.Branches[options.BranchName!]!;
            return MapBranch(updatedNewBranch);
        }

        // Default: switch to existing branch
        if (options.BranchName is null)
            throw new ArgumentException("BranchName is required.", nameof(options));
        RepositoryGuard.ValidateRequiredString(options.BranchName, nameof(options), "BranchName is required.");

        var branch = repository.Branches[options.BranchName!]
            ?? throw new ArgumentException($"The branch '{options.BranchName}' does not exist.", nameof(options));

        Commands.Checkout(repository, branch, checkoutOptions);

        var updatedBranch = repository.Branches[options.BranchName!]!;
        return MapBranch(updatedBranch);
    }

    /// <inheritdoc/>
    public GitBranchInfo CreateBranch(GitBranchCreateOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Branch name (options.Name) is required.");

        using var repository = new Repository(options.RepositoryPath);

        var existingBranch = repository.Branches[options.Name];

        if (existingBranch is not null && !options.Force)
        {
            throw new ArgumentException($"A branch named '{options.Name}' already exists.", nameof(options));
        }

        if (existingBranch is not null && options.Force)
        {
            repository.Branches.Remove(existingBranch);
        }

        // Resolve start point: specific committish or HEAD
        Commit startCommit;
        if (options.StartPoint is not null)
        {
            startCommit = repository.Lookup<Commit>(options.StartPoint)
                ?? throw new ArgumentException(
                    $"Start point '{options.StartPoint}' was not found in the repository.", nameof(options));
        }
        else
        {
            startCommit = repository.Head.Tip
                ?? throw new InvalidOperationException("Cannot create a branch: the repository has no commits.");
        }

        var branch = repository.CreateBranch(options.Name, startCommit);

        // Set remote tracking if requested and a remote exists
        if (options.Track)
        {
            var remote = repository.Network.Remotes.FirstOrDefault();
            if (remote is not null)
            {
                repository.Branches.Update(
                    branch,
                    b => b.Remote = remote.Name,
                    b => b.UpstreamBranch = $"refs/heads/{options.Name}");
            }
        }

        // Set branch description if provided
        if (!string.IsNullOrWhiteSpace(options.Description))
        {
            repository.Config.Set($"branch.{options.Name}.description", options.Description);
        }

        Commands.Checkout(repository, repository.Branches[options.Name]!);

        var updatedBranch = repository.Branches[options.Name]!;
        return MapBranch(updatedBranch);
    }

    /// <inheritdoc/>
    public void DeleteBranch(GitBranchDeleteOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Branch name (options.Name) is required.");

        using var repository = new Repository(options.RepositoryPath);

        var branch = repository.Branches[options.Name]
            ?? throw new ArgumentException($"The branch '{options.Name}' does not exist.", nameof(options));

        if (branch.IsCurrentRepositoryHead)
        {
            throw new InvalidOperationException($"Cannot delete the current branch '{options.Name}'. Switch to a different branch first.");
        }

        if (!options.Force && !IsBranchMerged(repository, branch))
        {
            throw new InvalidOperationException(
                $"The branch '{options.Name}' is not fully merged. Use force to delete it anyway.");
        }

        repository.Branches.Remove(branch);
    }

    /// <inheritdoc/>
    public GitBranchInfo SetBranch(GitBranchSetOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Branch name (options.Name) is required.");

        using var repository = new Repository(options.RepositoryPath);

        var branch = repository.Branches[options.Name]
            ?? throw new ArgumentException($"The branch '{options.Name}' does not exist.", nameof(options));

        if (branch.IsRemote)
        {
            throw new ArgumentException($"Cannot configure remote-tracking branch '{options.Name}'. Only local branches can be configured.", nameof(options));
        }

        // Set remote tracking: branch.<name>.remote and branch.<name>.merge
        if (options.Remote is not null)
        {
            var remote = repository.Network.Remotes[options.Remote]
                ?? throw new ArgumentException($"Remote '{options.Remote}' was not found in the repository.", nameof(options));

            var upstreamRef = options.Upstream is not null
                ? (options.Upstream.StartsWith("refs/", StringComparison.Ordinal)
                    ? options.Upstream
                    : $"refs/heads/{options.Upstream}")
                : $"refs/heads/{options.Name}";

            repository.Branches.Update(
                branch,
                b => b.Remote = remote.Name,
                b => b.UpstreamBranch = upstreamRef);
        }
        else if (options.Upstream is not null)
        {
            // Upstream without explicit remote — set merge ref only
            var mergeRef = options.Upstream.StartsWith("refs/", StringComparison.Ordinal)
                ? options.Upstream
                : $"refs/heads/{options.Upstream}";
            repository.Config.Set($"branch.{options.Name}.merge", mergeRef);
        }

        // Set branch description
        if (options.Description is not null)
        {
            repository.Config.Set($"branch.{options.Name}.description", options.Description);
        }

        // Re-read the branch to return updated info
        var updatedBranch = repository.Branches[options.Name]!;
        var descriptions = BuildDescriptionLookup(repository);
        return MapBranch(updatedBranch, descriptions: descriptions);
    }

    private static bool IsBranchMerged(Repository repository, Branch branch)
    {
        if (branch.Tip is null || repository.Head.Tip is null)
        {
            return true;
        }

        var mergeBase = repository.ObjectDatabase.FindMergeBase(repository.Head.Tip, branch.Tip);
        return mergeBase?.Sha == branch.Tip.Sha;
    }

    /// <inheritdoc/>
    public GitBranchInfo? FastForwardBranch(string repositoryPath, string branchName, string targetSha)
    {
        RepositoryGuard.ValidateRequiredString(repositoryPath, nameof(repositoryPath), "Repository path is required.");
        RepositoryGuard.ValidateRequiredString(branchName, nameof(branchName), "Branch name is required.");
        RepositoryGuard.ValidateRequiredString(targetSha, nameof(targetSha), "Target SHA is required.");

        using var repository = new Repository(repositoryPath);

        var branch = repository.Branches[branchName]
            ?? throw new ArgumentException($"Branch '{branchName}' was not found.", nameof(branchName));

        // Cannot advance a branch that is checked out — doing so would leave the
        // working tree detached from the new ref without a merge/reset.
        var worktreePaths = BuildWorktreeLookup(repository);
        if (worktreePaths.ContainsKey(branchName))
        {
            return null;
        }

        var targetCommit = repository.Lookup<Commit>(targetSha)
            ?? throw new ArgumentException($"Commit '{targetSha}' was not found.", nameof(targetSha));

        // Guard against rewinding: the local branch must not have commits
        // that are not reachable from the target.
        if (branch.Tip is not null)
        {
            var divergence = repository.ObjectDatabase.CalculateHistoryDivergence(branch.Tip, targetCommit);
            if (divergence.AheadBy > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot fast-forward branch '{branchName}': it has {divergence.AheadBy} commit(s) not in the target. " +
                    "Only a true fast-forward (no divergence) is supported.");
            }
        }

        repository.Refs.UpdateTarget(branch.Reference, targetCommit.Id);

        var updated = repository.Branches[branchName]!;
        return MapBranch(updated);
    }

    private static GitBranchInfo MapBranch(Branch branch, Dictionary<string, string>? worktreePaths = null, GitBranchComparisonInfo? referenceComparison = null, Dictionary<string, string>? descriptions = null)
    {
        string? worktreePath = null;
        worktreePaths?.TryGetValue(branch.FriendlyName, out worktreePath);

        string? description = null;
        if (!branch.IsRemote)
        {
            descriptions?.TryGetValue(branch.FriendlyName, out description);
        }

        return new GitBranchInfo(
            branch.FriendlyName,
            branch.IsCurrentRepositoryHead,
            branch.IsRemote,
            branch.Tip?.Sha ?? string.Empty,
            branch.TrackedBranch?.FriendlyName,
            branch.TrackingDetails?.AheadBy,
            branch.TrackingDetails?.BehindBy,
            worktreePath,
            referenceComparison,
            description);
    }

    /// <summary>
    /// Builds a dictionary mapping branch friendly names to their worktree paths.
    /// Includes both the main working tree and any linked worktrees.
    /// </summary>
    private static Dictionary<string, string> BuildWorktreeLookup(Repository repository)
    {
        var lookup = new Dictionary<string, string>(StringComparer.Ordinal);

        // Main worktree: the branch currently checked out in the main working tree.
        if (!repository.Info.IsBare && repository.Head is { FriendlyName: { } headName })
        {
            var mainPath = repository.Info.WorkingDirectory?.TrimEnd(
                System.IO.Path.DirectorySeparatorChar,
                System.IO.Path.AltDirectorySeparatorChar) ?? string.Empty;
            if (mainPath.Length > 0)
            {
                lookup[headName] = mainPath;
            }
        }

        // Linked worktrees: iterate and get the HEAD branch of each.
        foreach (var worktree in repository.Worktrees.Where(w => w is not null))
        {
            try
            {
                using var worktreeRepo = worktree.WorktreeRepository;
                var branchName = worktreeRepo.Head.FriendlyName;
                var path = worktreeRepo.Info.WorkingDirectory?.TrimEnd(
                    System.IO.Path.DirectorySeparatorChar,
                    System.IO.Path.AltDirectorySeparatorChar) ?? string.Empty;
                if (path.Length > 0)
                {
                    lookup[branchName] = path;
                }
            }
            catch
            {
                // Stale or invalid worktree — skip it.
            }
        }

        return lookup;
    }

    /// <summary>
    /// Builds a dictionary mapping local branch names to their descriptions
    /// from git config (<c>branch.&lt;name&gt;.description</c>).
    /// </summary>
    private static Dictionary<string, string> BuildDescriptionLookup(Repository repository)
    {
        var lookup = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var branch in repository.Branches.Where(b => !b.IsRemote))
        {
            var entry = repository.Config.Get<string>($"branch.{branch.FriendlyName}.description");
            if (entry is not null && !string.IsNullOrWhiteSpace(entry.Value))
            {
                lookup[branch.FriendlyName] = entry.Value.TrimEnd();
            }
        }

        return lookup;
    }
}
