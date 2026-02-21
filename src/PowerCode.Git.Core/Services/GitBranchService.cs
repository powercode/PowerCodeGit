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
    public GitBranchInfo SwitchBranch(string repositoryPath, string branchName)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));
        RepositoryGuard.ValidateRequiredString(branchName, nameof(branchName), "BranchName is required.");

        using var repository = new Repository(repositoryPath);

        var branch = repository.Branches[branchName]
            ?? throw new ArgumentException($"The branch '{branchName}' does not exist.", nameof(branchName));

        Commands.Checkout(repository, branch);

        // Re-read the branch after checkout to get accurate IsHead state
        var updatedBranch = repository.Branches[branchName]!;
        return MapBranch(updatedBranch);
    }

    /// <inheritdoc/>
    public GitBranchInfo CreateBranch(string repositoryPath, string name)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));
        RepositoryGuard.ValidateRequiredString(name, nameof(name), "Branch name is required.");

        using var repository = new Repository(repositoryPath);

        if (repository.Branches[name] is not null)
        {
            throw new ArgumentException($"A branch named '{name}' already exists.", nameof(name));
        }

        var branch = repository.CreateBranch(name);
        Commands.Checkout(repository, branch);

        var updatedBranch = repository.Branches[name]!;
        return MapBranch(updatedBranch);
    }

    /// <inheritdoc/>
    public void DeleteBranch(string repositoryPath, string name, bool force = false)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));
        RepositoryGuard.ValidateRequiredString(name, nameof(name), "Branch name is required.");

        using var repository = new Repository(repositoryPath);

        var branch = repository.Branches[name]
            ?? throw new ArgumentException($"The branch '{name}' does not exist.", nameof(name));

        if (branch.IsCurrentRepositoryHead)
        {
            throw new InvalidOperationException($"Cannot delete the current branch '{name}'. Switch to a different branch first.");
        }

        if (!force && !IsBranchMerged(repository, branch))
        {
            throw new InvalidOperationException(
                $"The branch '{name}' is not fully merged. Use force to delete it anyway.");
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

    private static GitBranchInfo MapBranch(Branch branch)
    {
        return new GitBranchInfo(
            branch.FriendlyName,
            branch.IsCurrentRepositoryHead,
            branch.IsRemote,
            branch.Tip?.Sha ?? string.Empty,
            branch.TrackedBranch?.FriendlyName,
            branch.TrackingDetails?.AheadBy,
            branch.TrackingDetails?.BehindBy);
    }
}
