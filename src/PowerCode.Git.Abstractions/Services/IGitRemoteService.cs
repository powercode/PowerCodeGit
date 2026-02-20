using System;
using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for interacting with git remotes — including listing
/// remotes, cloning, pushing, and pulling.
/// </summary>
public interface IGitRemoteService
{
    /// <summary>
    /// Gets all configured remotes in the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>A list of remote information objects.</returns>
    IReadOnlyList<GitRemoteInfo> GetRemotes(string repositoryPath);

    /// <summary>
    /// Clones a remote repository to a local path.
    /// </summary>
    /// <param name="options">Clone options including URL and local path.</param>
    /// <param name="onProgress">
    /// Optional callback receiving progress percentage (0–100) and a message.
    /// </param>
    /// <returns>The resolved absolute path of the cloned repository.</returns>
    string Clone(GitCloneOptions options, Action<int, string>? onProgress = null);

    /// <summary>
    /// Pushes a branch to a remote.
    /// </summary>
    /// <param name="options">The push options including remote and branch names.</param>
    /// <param name="onProgress">
    /// Optional callback receiving progress percentage (0–100) and a message.
    /// </param>
    /// <returns>Updated branch information after the push.</returns>
    GitBranchInfo Push(GitPushOptions options, Action<int, string>? onProgress = null);

    /// <summary>
    /// Pulls remote changes into the current branch.
    /// </summary>
    /// <param name="options">The pull options including remote and merge strategy.</param>
    /// <param name="onProgress">
    /// Optional callback receiving progress percentage (0–100) and a message.
    /// </param>
    /// <returns>Information about the resulting merge commit.</returns>
    GitCommitInfo Pull(GitPullOptions options, Action<int, string>? onProgress = null);
}
