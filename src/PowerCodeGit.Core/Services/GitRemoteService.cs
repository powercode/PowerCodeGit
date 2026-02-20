using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCodeGit.Abstractions.Models;
using PowerCodeGit.Abstractions.Services;

namespace PowerCodeGit.Core.Services;

/// <summary>
/// Lists configured git remotes using LibGit2Sharp.
/// </summary>
public sealed class GitRemoteService : IGitRemoteService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitRemoteInfo> GetRemotes(string repositoryPath)
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

        return repository.Network.Remotes
            .Select(remote => new GitRemoteInfo(remote.Name, remote.Url, remote.PushUrl ?? remote.Url))
            .ToList();
    }
}
