using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for listing and switching git branches.
/// </summary>
public interface IGitBranchService
{
    /// <summary>
    /// Gets all branches in the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>A list of branch information objects.</returns>
    IReadOnlyList<GitBranchInfo> GetBranches(string repositoryPath);

    /// <summary>
    /// Switches the repository HEAD to the specified branch.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="branchName">The name of the branch to switch to.</param>
    /// <returns>The branch information for the new HEAD.</returns>
    GitBranchInfo SwitchBranch(string repositoryPath, string branchName);
}
