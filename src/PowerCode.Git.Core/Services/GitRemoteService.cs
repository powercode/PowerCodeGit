using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Lists configured git remotes using LibGit2Sharp.
/// </summary>
public sealed class GitRemoteService : IGitRemoteService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitRemoteInfo> GetRemotes(string repositoryPath)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));

        using var repository = new Repository(repositoryPath);

        return repository.Network.Remotes
            .Select(remote => new GitRemoteInfo(remote.Name, remote.Url, remote.PushUrl ?? remote.Url))
            .ToList();
    }
}
