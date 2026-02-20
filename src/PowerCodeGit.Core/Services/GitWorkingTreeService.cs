using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCodeGit.Abstractions.Models;
using PowerCodeGit.Abstractions.Services;

namespace PowerCodeGit.Core.Services;

/// <summary>
/// Inspects the working tree and index state of a git repository using LibGit2Sharp.
/// </summary>
public sealed class GitWorkingTreeService : IGitWorkingTreeService
{
    /// <inheritdoc/>
    public GitStatusResult GetStatus(string repositoryPath)
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
        var status = repository.RetrieveStatus(new StatusOptions());

        var entries = new List<GitStatusEntry>();

        foreach (var entry in status)
        {
            MapStatusEntry(entry, entries);
        }

        var stagedCount = entries.Count(e => e.StagingState == GitStagingState.Staged);
        var modifiedCount = entries.Count(e => e.StagingState == GitStagingState.Unstaged && e.Status != GitFileStatus.Untracked);
        var untrackedCount = entries.Count(e => e.Status == GitFileStatus.Untracked);

        var currentBranch = repository.Head.FriendlyName ?? "(detached)";

        return new GitStatusResult(
            repositoryPath,
            currentBranch,
            entries,
            stagedCount,
            modifiedCount,
            untrackedCount);
    }

    /// <inheritdoc/>
    public IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.RepositoryPath))
        {
            throw new ArgumentException("RepositoryPath is required.", nameof(options));
        }

        if (!Repository.IsValid(options.RepositoryPath))
        {
            throw new ArgumentException("RepositoryPath does not reference a valid git repository.", nameof(options));
        }

        using var repository = new Repository(options.RepositoryPath);

        using var changes = options.Staged
            ? repository.Diff.Compare<Patch>(repository.Head.Tip?.Tree, DiffTargets.Index)
            : repository.Diff.Compare<Patch>();

        var entries = changes.AsEnumerable();

        if (options.Paths is { Length: > 0 })
        {
            var paths = options.Paths;
            entries = entries.Where(change =>
                paths.Any(p =>
                    string.Equals(change.Path, p, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(change.OldPath, p, StringComparison.OrdinalIgnoreCase)));
        }

        return entries
            .Select(MapDiffEntry)
            .ToList();
    }

    private static void MapStatusEntry(StatusEntry entry, List<GitStatusEntry> results)
    {
        var state = entry.State;

        // Staged (index) changes
        if (state.HasFlag(FileStatus.NewInIndex))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Added, GitStagingState.Staged));
        }

        if (state.HasFlag(FileStatus.ModifiedInIndex))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Modified, GitStagingState.Staged));
        }

        if (state.HasFlag(FileStatus.DeletedFromIndex))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Deleted, GitStagingState.Staged));
        }

        if (state.HasFlag(FileStatus.RenamedInIndex))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Renamed, GitStagingState.Staged));
        }

        // Unstaged (working directory) changes
        if (state.HasFlag(FileStatus.ModifiedInWorkdir))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Modified, GitStagingState.Unstaged));
        }

        if (state.HasFlag(FileStatus.DeletedFromWorkdir))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Deleted, GitStagingState.Unstaged));
        }

        if (state.HasFlag(FileStatus.RenamedInWorkdir))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Renamed, GitStagingState.Unstaged));
        }

        // Untracked
        if (state.HasFlag(FileStatus.NewInWorkdir))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Untracked, GitStagingState.Unstaged));
        }

        // Ignored
        if (state.HasFlag(FileStatus.Ignored))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Ignored, GitStagingState.Unstaged));
        }

        // Conflicted
        if (state.HasFlag(FileStatus.Conflicted))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Conflicted, GitStagingState.Unstaged));
        }
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
