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

        var result = branches.Select(b => MapBranch(b, worktreePaths)).ToList();

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

    private static bool IsBranchMerged(Repository repository, Branch branch)
    {
        if (branch.Tip is null || repository.Head.Tip is null)
        {
            return true;
        }

        var mergeBase = repository.ObjectDatabase.FindMergeBase(repository.Head.Tip, branch.Tip);
        return mergeBase?.Sha == branch.Tip.Sha;
    }

    private static GitBranchInfo MapBranch(Branch branch, Dictionary<string, string>? worktreePaths = null)
    {
        string? worktreePath = null;
        worktreePaths?.TryGetValue(branch.FriendlyName, out worktreePath);

        return new GitBranchInfo(
            branch.FriendlyName,
            branch.IsCurrentRepositoryHead,
            branch.IsRemote,
            branch.Tip?.Sha ?? string.Empty,
            branch.TrackedBranch?.FriendlyName,
            branch.TrackingDetails?.AheadBy,
            branch.TrackingDetails?.BehindBy,
            worktreePath);
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
}
