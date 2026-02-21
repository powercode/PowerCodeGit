using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Inspects and modifies the working tree, index, and HEAD of a git repository
/// using LibGit2Sharp — including status, diff, staging, and reset.
/// </summary>
public sealed class GitWorkingTreeService : IGitWorkingTreeService
{
    /// <inheritdoc/>
    public GitStatusResult GetStatus(GitStatusOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var showUntracked = options.UntrackedFilesMode != GitUntrackedFilesMode.No;

        var status = repository.RetrieveStatus(new StatusOptions
        {
            IncludeIgnored = options.IncludeIgnored,
            IncludeUntracked = showUntracked,
            RecurseUntrackedDirs = options.UntrackedFilesMode == GitUntrackedFilesMode.All,
        });

        var entries = new List<GitStatusEntry>();

        foreach (var entry in status)
        {
            MapStatusEntry(entry, entries);
        }

        // Filter by paths if specified
        if (options.Paths is { Length: > 0 })
        {
            var paths = options.Paths;
            entries = entries
                .Where(e => paths.Any(p =>
                    string.Equals(e.FilePath, p, StringComparison.OrdinalIgnoreCase) ||
                    e.FilePath.StartsWith(p.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var stagedCount = entries.Count(e => e.StagingState == GitStagingState.Staged);
        var modifiedCount = entries.Count(e => e.StagingState == GitStagingState.Unstaged && e.Status != GitFileStatus.Untracked);
        var untrackedCount = entries.Count(e => e.Status == GitFileStatus.Untracked);

        var currentBranch = repository.Head.FriendlyName ?? "(detached)";

        return new GitStatusResult(
            options.RepositoryPath,
            currentBranch,
            entries,
            stagedCount,
            modifiedCount,
            untrackedCount);
    }

    /// <inheritdoc/>
    public IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

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

        // Conflicted
        if (state.HasFlag(FileStatus.Conflicted))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Conflicted, GitStagingState.Unstaged));
        }

        // Ignored (only present when StatusOptions.IncludeIgnored = true)
        if (state.HasFlag(FileStatus.Ignored))
        {
            results.Add(new GitStatusEntry(entry.FilePath, GitFileStatus.Ignored, GitStagingState.Unstaged));
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

    /// <inheritdoc/>
    public void Stage(GitStageOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        if (options.All)
        {
            Commands.Stage(repository, "*");
            return;
        }

        if (options.Paths is not { Count: > 0 })
        {
            throw new ArgumentException("Either Paths or All must be specified.", nameof(options));
        }

        foreach (var path in options.Paths)
        {
            Commands.Stage(repository, path);
        }
    }

    /// <inheritdoc/>
    public void Unstage(string repositoryPath, IReadOnlyList<string>? paths = null)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));

        using var repository = new Repository(repositoryPath);

        if (paths is { Count: > 0 })
        {
            foreach (var path in paths)
            {
                Commands.Unstage(repository, path);
            }
        }
        else
        {
            // Unstage everything
            if (repository.Head.Tip is not null)
            {
                repository.Reset(ResetMode.Mixed, repository.Head.Tip);
            }
        }
    }

    /// <inheritdoc/>
    public void Reset(string repositoryPath, string? revision, GitResetMode mode)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));

        using var repository = new Repository(repositoryPath);

        var resetMode = mode switch
        {
            GitResetMode.Soft => ResetMode.Soft,
            GitResetMode.Hard => ResetMode.Hard,
            _ => ResetMode.Mixed,
        };

        var target = revision is not null
            ? repository.Lookup<Commit>(revision)
                ?? throw new ArgumentException($"Revision '{revision}' was not found.", nameof(revision))
            : repository.Head.Tip
                ?? throw new InvalidOperationException("Repository has no commits to reset to.");

        repository.Reset(resetMode, target);
    }
}
