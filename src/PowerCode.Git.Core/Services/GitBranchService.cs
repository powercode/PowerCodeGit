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
