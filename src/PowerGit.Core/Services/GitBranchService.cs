using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Core.Services;

/// <summary>
/// Lists and switches git branches using LibGit2Sharp.
/// </summary>
public sealed class GitBranchService : IGitBranchService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitBranchInfo> GetBranches(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("RepositoryPath is required.", nameof(repositoryPath));
        }

        if (!Repository.IsValid(repositoryPath))
        {
            throw new ArgumentException("RepositoryPath does not reference a valid git repository.", nameof(repositoryPath));
        }

        using var repository = new Repository(repositoryPath);

        return repository.Branches
            .Select(MapBranch)
            .ToList();
    }

    /// <inheritdoc/>
    public GitBranchInfo SwitchBranch(string repositoryPath, string branchName)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("RepositoryPath is required.", nameof(repositoryPath));
        }

        if (string.IsNullOrWhiteSpace(branchName))
        {
            throw new ArgumentException("BranchName is required.", nameof(branchName));
        }

        if (!Repository.IsValid(repositoryPath))
        {
            throw new ArgumentException("RepositoryPath does not reference a valid git repository.", nameof(repositoryPath));
        }

        using var repository = new Repository(repositoryPath);

        var branch = repository.Branches[branchName]
            ?? throw new ArgumentException($"The branch '{branchName}' does not exist.", nameof(branchName));

        Commands.Checkout(repository, branch);

        // Re-read the branch after checkout to get accurate IsHead state
        var updatedBranch = repository.Branches[branchName]!;
        return MapBranch(updatedBranch);
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
