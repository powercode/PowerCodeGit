using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Returns the files changed by a specific commit by comparing its tree
/// against the first parent's tree. Uses LibGit2Sharp inside the isolated
/// assembly load context.
/// </summary>
public sealed class GitCommitFileService : IGitCommitFileService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitDiffEntry> GetCommitFiles(GitCommitFileOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var commit = options.Commit is not null
            ? repository.Lookup<Commit>(options.Commit)
                ?? throw new ArgumentException($"Commit '{options.Commit}' was not found.", nameof(options))
            : repository.Head.Tip
                ?? throw new InvalidOperationException("The repository has no commits.");

        var parentTree = commit.Parents.FirstOrDefault()?.Tree;

        using var changes = repository.Diff.Compare<Patch>(parentTree, commit.Tree);

        var entries = changes.AsEnumerable();

        if (options.Paths is { Length: > 0 })
        {
            var paths = options.Paths;
            entries = entries.Where(change =>
                paths.Any(p =>
                    string.Equals(change.Path, p, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(change.OldPath, p, StringComparison.OrdinalIgnoreCase)));
        }

        return entries.Select(MapDiffEntry).ToList();
    }

    private static GitDiffEntry MapDiffEntry(PatchEntryChanges change)
    {
        var status = change.Status switch
        {
            ChangeKind.Added => GitFileStatus.Added,
            ChangeKind.Deleted => GitFileStatus.Deleted,
            ChangeKind.Modified => GitFileStatus.Modified,
            ChangeKind.Renamed => GitFileStatus.Renamed,
            ChangeKind.Conflicted => GitFileStatus.Conflicted,
            _ => GitFileStatus.Modified,
        };

        return new GitDiffEntry(
            change.OldPath,
            change.Path,
            status,
            change.LinesAdded,
            change.LinesDeleted,
            change.Patch);
    }
}
