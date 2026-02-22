using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        // Note: LibGit2Sharp CompareOptions does not expose an IgnoreWhitespace property.
        // The IgnoreWhitespace option is accepted in GitDiffOptions for future support
        // and for use by callers that process the result set further.

        Patch changes;

        if (options.FromCommit is not null && options.ToCommit is not null)
        {
            // Range diff: git diff <from> <to>
            var fromCommit = repository.Lookup<Commit>(options.FromCommit)
                ?? throw new ArgumentException($"Commit '{options.FromCommit}' was not found.", nameof(options));
            var toCommit = repository.Lookup<Commit>(options.ToCommit)
                ?? throw new ArgumentException($"Commit '{options.ToCommit}' was not found.", nameof(options));
            changes = repository.Diff.Compare<Patch>(fromCommit.Tree, toCommit.Tree);
        }
        else if (options.Commit is not null)
        {
            // Diff working tree against a commit: git diff <commit>
            var commit = repository.Lookup<Commit>(options.Commit)
                ?? throw new ArgumentException($"Commit '{options.Commit}' was not found.", nameof(options));
            changes = repository.Diff.Compare<Patch>(commit.Tree, DiffTargets.WorkingDirectory);
        }
        else if (options.Staged)
        {
            // Staged diff: git diff --staged
            changes = repository.Diff.Compare<Patch>(repository.Head.Tip?.Tree, DiffTargets.Index);
        }
        else
        {
            // Default: working directory diff
            changes = repository.Diff.Compare<Patch>();
        }

        var entries = changes.AsEnumerable();

        if (options.Paths is { Length: > 0 })
        {
            var paths = options.Paths;
            entries = entries.Where(change =>
                paths.Any(p =>
                    string.Equals(change.Path, p, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(change.OldPath, p, StringComparison.OrdinalIgnoreCase)));
        }

        var result = entries
            .Select(MapDiffEntry)
            .ToList();

        changes.Dispose();
        return result;
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

        var stageOptions = options.Force
            ? new StageOptions { IncludeIgnored = true }
            : null;

        if (options.Update)
        {
            // Stage only already-tracked files that have modifications (git add -u)
            var trackedPaths = repository.RetrieveStatus()
                .Where(e => !e.State.HasFlag(FileStatus.NewInWorkdir) && !e.State.HasFlag(FileStatus.Ignored))
                .Select(e => e.FilePath)
                .ToList();

            foreach (var path in trackedPaths)
            {
                Commands.Stage(repository, path, stageOptions);
            }

            return;
        }

        if (options.All)
        {
            Commands.Stage(repository, "*", stageOptions);
            return;
        }

        if (options.Paths is not { Count: > 0 })
        {
            throw new ArgumentException("Either Paths, All, or Update must be specified.", nameof(options));
        }

        foreach (var path in options.Paths)
        {
            Commands.Stage(repository, path, stageOptions);
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
    public void Reset(GitResetOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        if (options.Paths is { Count: > 0 })
        {
            // Path-based reset: unstage specific files (git reset -- <path>)
            foreach (var path in options.Paths)
            {
                Commands.Unstage(repository, path);
            }

            return;
        }

        var resetMode = options.Mode switch
        {
            GitResetMode.Soft => ResetMode.Soft,
            GitResetMode.Hard => ResetMode.Hard,
            _ => ResetMode.Mixed,
        };

        var target = options.Revision is not null
            ? repository.Lookup<Commit>(options.Revision)
                ?? throw new ArgumentException($"Revision '{options.Revision}' was not found.", nameof(options))
            : repository.Head.Tip
                ?? throw new InvalidOperationException("Repository has no commits to reset to.");

        repository.Reset(resetMode, target);
    }

    /// <inheritdoc/>
    public void Reset(string repositoryPath, string? revision, GitResetMode mode)
        => Reset(new GitResetOptions { RepositoryPath = repositoryPath, Revision = revision, Mode = mode });

    private static readonly TimeSpan GitApplyTimeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public void StageHunks(GitStageHunkOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        if (options.Hunks is not { Count: > 0 })
        {
            throw new ArgumentException("At least one hunk must be specified.", nameof(options));
        }

        var patch = BuildPatch(options.Hunks);

        var startInfo = new ProcessStartInfo("git", "apply --cached")
        {
            WorkingDirectory = options.RepositoryPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git process.");

        process.StandardInput.Write(patch);
        process.StandardInput.Close();

        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(GitApplyTimeout);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git apply --cached failed (exit code {process.ExitCode}): {stderr.Trim()}");
        }
    }

    private static string BuildPatch(IReadOnlyList<GitDiffHunk> hunks)
    {
        var sb = new StringBuilder();

        // Group hunks by file so each file gets a single diff header.
        var grouped = hunks
            .GroupBy(h => (h.OldPath, h.FilePath))
            .ToList();

        foreach (var group in grouped)
        {
            var (oldPath, newPath) = group.Key;

            // Use \n explicitly — git apply rejects \r\n in patch headers.
            sb.Append("diff --git a/").Append(oldPath).Append(" b/").Append(newPath).Append('\n');
            sb.Append("--- a/").Append(oldPath).Append('\n');
            sb.Append("+++ b/").Append(newPath).Append('\n');

            foreach (var hunk in group)
            {
                sb.Append(hunk.Content).Append('\n');
            }
        }

        return sb.ToString();
    }
}
