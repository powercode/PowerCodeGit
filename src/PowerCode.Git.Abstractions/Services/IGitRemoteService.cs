using System;
using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for interacting with git remotes — including listing,
/// adding, removing, renaming, updating, cloning, pushing, and pulling.
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
    /// Gets remotes in the repository, optionally filtered by name.
    /// </summary>
    /// <param name="options">
    /// List options. When <see cref="GitRemoteListOptions.Name"/> is set, returns
    /// only the matching remote (empty list if not found).
    /// </param>
    /// <returns>A list of remote information objects.</returns>
    IReadOnlyList<GitRemoteInfo> GetRemotes(GitRemoteListOptions options);

    /// <summary>
    /// Adds a new remote to the repository, equivalent to
    /// <c>git remote add &lt;name&gt; &lt;url&gt;</c>.
    /// </summary>
    /// <param name="options">Options specifying the remote name and URLs.</param>
    /// <returns>The newly created remote.</returns>
    GitRemoteInfo AddRemote(GitRemoteAddOptions options);

    /// <summary>
    /// Removes a remote from the repository, equivalent to
    /// <c>git remote remove &lt;name&gt;</c>. All remote-tracking branches and
    /// configuration settings for the remote are removed.
    /// </summary>
    /// <param name="options">Options specifying the remote to remove.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when the specified remote does not exist.
    /// </exception>
    void RemoveRemote(GitRemoteRemoveOptions options);

    /// <summary>
    /// Renames a remote, equivalent to <c>git remote rename &lt;old&gt; &lt;new&gt;</c>.
    /// All remote-tracking branches and configuration settings are updated.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="oldName">The current name of the remote.</param>
    /// <param name="newName">The new name for the remote.</param>
    /// <returns>The renamed remote with its updated name.</returns>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="oldName"/> does not exist.
    /// </exception>
    GitRemoteInfo RenameRemote(string repositoryPath, string oldName, string newName);

    /// <summary>
    /// Updates the fetch URL and/or push URL of an existing remote, equivalent to
    /// <c>git remote set-url</c>.
    /// </summary>
    /// <param name="options">
    /// Options specifying which remote to update and which URLs to change.
    /// At least one of <see cref="GitRemoteUpdateOptions.Url"/> or
    /// <see cref="GitRemoteUpdateOptions.PushUrl"/> must be non-<see langword="null"/>.
    /// </param>
    /// <returns>The updated remote information.</returns>
    /// <exception cref="System.ArgumentException">
    /// Thrown when the specified remote does not exist.
    /// </exception>
    GitRemoteInfo UpdateRemoteUrl(GitRemoteUpdateOptions options);

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
