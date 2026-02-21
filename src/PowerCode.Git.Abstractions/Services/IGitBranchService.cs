using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for listing and switching git branches.
/// </summary>
public interface IGitBranchService
{
    /// <summary>
    /// Gets branches in the repository filtered by the specified options.
    /// </summary>
    /// <param name="options">Options controlling which branches are returned.</param>
    /// <returns>A list of branch information objects.</returns>
    IReadOnlyList<GitBranchInfo> GetBranches(GitBranchListOptions options);

    /// <summary>
    /// Gets all branches in the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>A list of branch information objects.</returns>
    IReadOnlyList<GitBranchInfo> GetBranches(string repositoryPath)
        => GetBranches(new GitBranchListOptions { RepositoryPath = repositoryPath });

    /// <summary>
    /// Switches the repository HEAD to the specified branch.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="branchName">The name of the branch to switch to.</param>
    /// <returns>The branch information for the new HEAD.</returns>
    GitBranchInfo SwitchBranch(string repositoryPath, string branchName);

    /// <summary>
    /// Creates a new branch at the current HEAD and checks it out.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="name">The name of the new branch.</param>
    /// <returns>Information about the newly created branch.</returns>
    GitBranchInfo CreateBranch(string repositoryPath, string name);

    /// <summary>
    /// Deletes a branch.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="name">The name of the branch to delete.</param>
    /// <param name="force">
    /// When <see langword="true"/>, force-deletes the branch even if it is
    /// not fully merged.
    /// </param>
    void DeleteBranch(string repositoryPath, string name, bool force = false);
}
