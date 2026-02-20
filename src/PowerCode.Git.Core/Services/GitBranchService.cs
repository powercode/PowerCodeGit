using System;
using System.Collections.Generic;
using System.Linq;
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
    public IReadOnlyList<GitBranchInfo> GetBranches(string repositoryPath)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));

        using var repository = new Repository(repositoryPath);

        return repository.Branches
            .Select(MapBranch)
            .ToList();
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
