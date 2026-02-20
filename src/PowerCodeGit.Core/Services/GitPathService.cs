using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCodeGit.Abstractions.Services;

namespace PowerCodeGit.Core.Services;

/// <summary>
/// Lists tracked file paths in a git repository using LibGit2Sharp.
/// </summary>
public sealed class GitPathService : IGitPathService
{
    /// <inheritdoc/>
    public IReadOnlyList<string> GetTrackedPaths(string repositoryPath)
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

        if (repository.Head.Tip is null)
        {
            return [];
        }

        return repository.Head.Tip.Tree
            .SelectMany(FlattenTree)
            .ToList();
    }

    private static IEnumerable<string> FlattenTree(TreeEntry entry)
    {
        if (entry.TargetType == TreeEntryTargetType.Blob)
        {
            return [entry.Path];
        }

        if (entry.TargetType == TreeEntryTargetType.Tree)
        {
            return ((Tree)entry.Target)
                .SelectMany(FlattenTree);
        }

        return [];
    }
}
