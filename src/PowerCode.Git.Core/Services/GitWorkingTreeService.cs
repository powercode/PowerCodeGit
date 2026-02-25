using System;
using System.Collections.Generic;
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
    private readonly IGitExecutable gitExecutable;

    /// <summary>
    /// Initializes a new instance using the default <see cref="GitExecutable"/>.
    /// </summary>
    public GitWorkingTreeService() : this(new GitExecutable()) { }

    /// <summary>
    /// Initializes a new instance with the specified <see cref="IGitExecutable"/>
    /// for testability.
    /// </summary>
    /// <param name="gitExecutable">The git process runner to use.</param>
    internal GitWorkingTreeService(IGitExecutable gitExecutable)
    {
        this.gitExecutable = gitExecutable;
    }
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

        var trackedBranchName = repository.Head.TrackedBranch?.FriendlyName;
        var aheadBy = repository.Head.TrackingDetails?.AheadBy;
        var behindBy = repository.Head.TrackingDetails?.BehindBy;

        return new GitStatusResult(
            options.RepositoryPath,
            currentBranch,
            entries,
            stagedCount,
            modifiedCount,
            untrackedCount,
            trackedBranchName,
            aheadBy,
            behindBy);
    }

    /// <inheritdoc/>
    public IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        // Note: LibGit2Sharp CompareOptions does not expose an IgnoreWhitespace property.
        // The IgnoreWhitespace option is accepted in GitDiffOptions for future support
        // and for use by callers that process the result set further.

        var compareOptions = options.ContextLines is { } contextLines
            ? new CompareOptions { ContextLines = contextLines }
            : null;

        Patch changes;

        if (options.FromCommit is not null && options.ToCommit is not null)
        {
            // Range diff: git diff <from> <to>
            var fromCommit = repository.Lookup<Commit>(options.FromCommit)
                ?? throw new ArgumentException($"Commit '{options.FromCommit}' was not found.", nameof(options));
            var toCommit = repository.Lookup<Commit>(options.ToCommit)
                ?? throw new ArgumentException($"Commit '{options.ToCommit}' was not found.", nameof(options));
            changes = repository.Diff.Compare<Patch>(fromCommit.Tree, toCommit.Tree, compareOptions);
        }
        else if (options.Commit is not null)
        {
            // Diff working tree against a commit: git diff <commit>
            var commit = repository.Lookup<Commit>(options.Commit)
                ?? throw new ArgumentException($"Commit '{options.Commit}' was not found.", nameof(options));
            changes = repository.Diff.Compare<Patch>(commit.Tree, DiffTargets.WorkingDirectory, null, null, compareOptions);
        }
        else if (options.Staged)
        {
            // Staged diff: git diff --staged
            changes = repository.Diff.Compare<Patch>(repository.Head.Tip?.Tree, DiffTargets.Index, null, null, compareOptions);
        }
        else
        {
            // Default: working directory diff
            changes = compareOptions is null
                ? repository.Diff.Compare<Patch>()
                : repository.Diff.Compare<Patch>(null, false, null, compareOptions);
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

    /// <inheritdoc/>
    public void StageHunks(GitStageHunkOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        if (options.Hunks is not { Count: > 0 })
        {
            throw new ArgumentException("At least one hunk must be specified.", nameof(options));
        }

        var patch = BuildPatch(options.Hunks);

        gitExecutable.Run(options.RepositoryPath, ["apply", "--cached"], patch);
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
            var status = group.First().Status;

            // For new files the old side is /dev/null; for deletions the new side is.
            var oldPrefix = status == GitFileStatus.Added ? "/dev/null" : $"a/{oldPath}";
            var newPrefix = status == GitFileStatus.Deleted ? "/dev/null" : $"b/{newPath}";

            // Use \n explicitly — git apply rejects \r\n in patch headers.
            sb.Append("diff --git a/").Append(oldPath).Append(" b/").Append(newPath).Append('\n');
            sb.Append("--- ").Append(oldPrefix).Append('\n');
            sb.Append("+++ ").Append(newPrefix).Append('\n');

            foreach (var hunk in group)
            {
                sb.Append(hunk.Content).Append('\n');
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc/>
    public void Restore(GitRestoreOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        // Build the git restore argument list.
        // --worktree is the default target when Staged is false.
        // --staged restores the index; --source must be provided explicitly in that case
        // because git restore --staged defaults to HEAD only if a source is given.
        var args = new List<string> { "restore" };

        if (options.Staged)
        {
            args.Add("--staged");
        }

        if (options.Source is not null)
        {
            args.Add($"--source={options.Source}");
        }

        if (options.All)
        {
            args.Add(".");
        }
        else if (options.Paths is { Count: > 0 })
        {
            args.Add("--");

            foreach (var path in options.Paths)
            {
                args.Add(path);
            }
        }
        else
        {
            throw new ArgumentException(
                "Either Paths or All must be specified.", nameof(options));
        }

        gitExecutable.Run(options.RepositoryPath, args);
    }

    /// <inheritdoc/>
    public void RestoreHunks(GitRestoreHunkOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        if (options.Hunks is not { Count: > 0 })
        {
            throw new ArgumentException("At least one hunk must be specified.", nameof(options));
        }

        var patch = BuildPatch(options.Hunks);

        // Apply the patch in reverse (-R). When restoring staged hunks, also
        // pass --cached so only the index is modified.
        var args = options.Staged
            ? (IReadOnlyList<string>)["apply", "-R", "--cached"]
            : ["apply", "-R"];

        gitExecutable.Run(options.RepositoryPath, args, patch);
    }

    /// <inheritdoc/>
    public int GetStashCount(string repositoryPath)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            throw new ArgumentException("Repository path must not be null or empty.", nameof(repositoryPath));
        }

        using var repository = new Repository(repositoryPath);
        return repository.Stashes.Count();
    }

    /// <inheritdoc/>
    public GitWorkingTreePromptInfo GetPromptInfo(string repositoryPath)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            throw new ArgumentException("Repository path must not be null or empty.", nameof(repositoryPath));
        }

        using var repository = new Repository(repositoryPath);

        var isDetached = repository.Info.IsHeadDetached;
        var branchName = isDetached
            ? repository.Head.Tip?.Sha[..7] ?? "HEAD"
            : repository.Head.FriendlyName;

        var trackedBranchName = repository.Head.TrackedBranch?.FriendlyName;
        var aheadBy = repository.Head.TrackingDetails?.AheadBy;
        var behindBy = repository.Head.TrackingDetails?.BehindBy;

        var status = repository.RetrieveStatus(new StatusOptions
        {
            IncludeIgnored = false,
            IncludeUntracked = true,
            RecurseUntrackedDirs = false,
        });

        var stagedCount = 0;
        var modifiedCount = 0;
        var untrackedCount = 0;

        foreach (var entry in status)
        {
            var state = entry.State;
            if ((state & (FileStatus.NewInIndex | FileStatus.ModifiedInIndex | FileStatus.DeletedFromIndex |
                          FileStatus.RenamedInIndex | FileStatus.TypeChangeInIndex)) != 0)
            {
                stagedCount++;
            }

            if ((state & (FileStatus.ModifiedInWorkdir | FileStatus.DeletedFromWorkdir |
                          FileStatus.TypeChangeInWorkdir | FileStatus.RenamedInWorkdir)) != 0)
            {
                modifiedCount++;
            }

            if ((state & FileStatus.NewInWorkdir) != 0)
            {
                untrackedCount++;
            }
        }

        var stashCount = repository.Stashes.Count();

        return new GitWorkingTreePromptInfo(
            branchName,
            isDetached,
            trackedBranchName,
            aheadBy,
            behindBy,
            stagedCount,
            modifiedCount,
            untrackedCount,
            stashCount);
    }
}
