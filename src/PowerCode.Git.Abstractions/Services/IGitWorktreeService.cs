using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for managing git worktrees.
/// </summary>
public interface IGitWorktreeService
{
    /// <summary>
    /// Gets all worktrees in the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>A list of worktree information objects.</returns>
    IReadOnlyList<GitWorktreeInfo> GetWorktrees(string repositoryPath);

    /// <summary>
    /// Gets all worktrees in the repository using the specified options.
    /// </summary>
    /// <param name="options">Options identifying the repository to list worktrees for.</param>
    /// <returns>A list of worktree information objects.</returns>
    IReadOnlyList<GitWorktreeInfo> GetWorktrees(GitWorktreeListOptions options)
        => GetWorktrees(options.RepositoryPath);

    /// <summary>
    /// Adds a new worktree using the specified options.
    /// </summary>
    /// <param name="options">Options controlling the worktree creation.</param>
    /// <returns>Information about the newly created worktree.</returns>
    GitWorktreeInfo AddWorktree(GitWorktreeAddOptions options);

    /// <summary>
    /// Adds a new worktree at the specified path.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="name">The name for the new worktree.</param>
    /// <param name="path">The filesystem path where the worktree will be created.</param>
    /// <returns>Information about the newly created worktree.</returns>
    GitWorktreeInfo AddWorktree(string repositoryPath, string name, string path)
        => AddWorktree(new GitWorktreeAddOptions { RepositoryPath = repositoryPath, Name = name, Path = path });

    /// <summary>
    /// Removes a worktree using the specified options.
    /// </summary>
    /// <param name="options">Options identifying the worktree to remove.</param>
    void RemoveWorktree(GitWorktreeRemoveOptions options);

    /// <summary>
    /// Removes a worktree by name.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="name">The name of the worktree to remove.</param>
    /// <param name="force">When <see langword="true"/>, removes even if the worktree is locked.</param>
    void RemoveWorktree(string repositoryPath, string name, bool force = false)
        => RemoveWorktree(new GitWorktreeRemoveOptions { RepositoryPath = repositoryPath, Name = name, Force = force });

    /// <summary>
    /// Locks a worktree using the specified options.
    /// </summary>
    /// <param name="options">Options identifying the worktree to lock.</param>
    void LockWorktree(GitWorktreeLockOptions options);

    /// <summary>
    /// Locks a worktree by name.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="name">The name of the worktree to lock.</param>
    /// <param name="reason">The reason for locking, or <see langword="null"/>.</param>
    void LockWorktree(string repositoryPath, string name, string? reason = null)
        => LockWorktree(new GitWorktreeLockOptions { RepositoryPath = repositoryPath, Name = name, Reason = reason });

    /// <summary>
    /// Unlocks a worktree using the specified options.
    /// </summary>
    /// <param name="options">Options identifying the worktree to unlock.</param>
    void UnlockWorktree(GitWorktreeUnlockOptions options);

    /// <summary>
    /// Unlocks a worktree by name.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="name">The name of the worktree to unlock.</param>
    void UnlockWorktree(string repositoryPath, string name)
        => UnlockWorktree(new GitWorktreeUnlockOptions { RepositoryPath = repositoryPath, Name = name });
}
