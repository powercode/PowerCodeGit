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
    /// Switches the repository HEAD using the specified options.
    /// </summary>
    /// <param name="options">Options controlling the switch operation.</param>
    /// <returns>The branch information for the new HEAD.</returns>
    GitBranchInfo SwitchBranch(GitSwitchOptions options);

    /// <summary>
    /// Switches the repository HEAD to the specified branch.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="branchName">The name of the branch to switch to.</param>
    /// <returns>The branch information for the new HEAD.</returns>
    GitBranchInfo SwitchBranch(string repositoryPath, string branchName)
        => SwitchBranch(new GitSwitchOptions { RepositoryPath = repositoryPath, BranchName = branchName });

    /// <summary>
    /// Creates a new branch using the specified options.
    /// </summary>
    /// <param name="options">Options for the branch to create.</param>
    /// <returns>Information about the newly created branch.</returns>
    GitBranchInfo CreateBranch(GitBranchCreateOptions options);

    /// <summary>
    /// Creates a new branch at the current HEAD and checks it out.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="name">The name of the new branch.</param>
    /// <returns>Information about the newly created branch.</returns>
    GitBranchInfo CreateBranch(string repositoryPath, string name)
        => CreateBranch(new GitBranchCreateOptions { RepositoryPath = repositoryPath, Name = name });

    /// <summary>
    /// Deletes a branch using the specified options.
    /// </summary>
    /// <param name="options">Options identifying the branch to delete.</param>
    void DeleteBranch(GitBranchDeleteOptions options);

    /// <summary>
    /// Deletes a branch.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="name">The name of the branch to delete.</param>
    /// <param name="force">
    /// When <see langword="true"/>, force-deletes the branch even if it is
    /// not fully merged.
    /// </param>
    void DeleteBranch(string repositoryPath, string name, bool force = false)
        => DeleteBranch(new GitBranchDeleteOptions { RepositoryPath = repositoryPath, Name = name, Force = force });

    /// <summary>
    /// Configures an existing local branch by setting its remote, upstream,
    /// and/or description.
    /// </summary>
    /// <param name="options">Options identifying the branch and the configuration values to set.</param>
    /// <returns>The updated branch information.</returns>
    GitBranchInfo SetBranch(GitBranchSetOptions options);

    /// <summary>
    /// Advances an existing local branch ref to <paramref name="targetSha"/> without
    /// switching HEAD or touching the working tree. Equivalent to moving a
    /// non-checked-out branch pointer forward.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="branchName">The local branch name to fast-forward.</param>
    /// <param name="targetSha">The full SHA of the commit to advance to.</param>
    /// <returns>
    /// The updated <see cref="GitBranchInfo"/>, or <see langword="null"/> when the branch
    /// is currently checked out in a worktree and cannot be advanced without
    /// modifying the working tree.
    /// </returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when advancing to <paramref name="targetSha"/> would not be a
    /// fast-forward (i.e. the local branch has commits not reachable from the target).
    /// </exception>
    GitBranchInfo? FastForwardBranch(string repositoryPath, string branchName, string targetSha);
}
