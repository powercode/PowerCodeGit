using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Lists tracked file paths in a git repository using LibGit2Sharp.
/// </summary>
public sealed class GitPathService : IGitPathService
{
    /// <inheritdoc/>
    public IReadOnlyList<string> GetTrackedPaths(string repositoryPath)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));

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
