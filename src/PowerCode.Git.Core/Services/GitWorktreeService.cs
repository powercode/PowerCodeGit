using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Manages git worktrees using LibGit2Sharp.
/// </summary>
public sealed class GitWorktreeService : IGitWorktreeService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitWorktreeInfo> GetWorktrees(GitWorktreeListOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        // LibGit2Sharp's WorktreeCollection.Lookup returns null for stale or
        // invalid worktree entries, so filter them out before mapping.
        return repository.Worktrees
            .Where(w => w is not null)
            .Select(MapWorktree)
            .ToList();
    }

    /// <inheritdoc/>
    public GitWorktreeInfo AddWorktree(GitWorktreeAddOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Worktree name is required.");
        RepositoryGuard.ValidateRequiredString(options.Path, nameof(options), "Worktree path is required.");

        // LibGit2Sharp returns null when the branch spec equals the worktree name
        // because it internally creates a branch with the worktree name first.
        if (options.Branch is not null && string.Equals(options.Branch, options.Name, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The worktree name '{options.Name}' cannot be the same as the target branch. " +
                "Use a different worktree name when checking out an existing branch.",
                nameof(options));
        }

        // Ensure the parent directory exists — LibGit2Sharp does not create
        // intermediate directories and will fail with a path-not-found error.
        var parentDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(options.Path));
        if (parentDir is not null && !System.IO.Directory.Exists(parentDir))
        {
            System.IO.Directory.CreateDirectory(parentDir);
        }

        using var repository = new Repository(options.RepositoryPath);

        Worktree? worktree;
        if (options.Branch is not null)
        {
            worktree = repository.Worktrees.Add(options.Branch, options.Name, options.Path, options.Locked);
        }
        else
        {
            worktree = repository.Worktrees.Add(options.Name, options.Path, options.Locked);
        }

        if (worktree is null)
        {
            throw new InvalidOperationException(
                $"Failed to create worktree '{options.Name}'. The underlying git operation returned no result.");
        }

        return MapWorktree(worktree);
    }

    /// <inheritdoc/>
    public void RemoveWorktree(GitWorktreeRemoveOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Worktree name is required.");

        using var repository = new Repository(options.RepositoryPath);

        var worktree = repository.Worktrees[options.Name]
            ?? throw new ArgumentException($"The worktree '{options.Name}' does not exist.", nameof(options));

        repository.Worktrees.Prune(worktree, options.Force);
    }

    /// <inheritdoc/>
    public void LockWorktree(GitWorktreeLockOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Worktree name is required.");

        using var repository = new Repository(options.RepositoryPath);

        var worktree = repository.Worktrees[options.Name]
            ?? throw new ArgumentException($"The worktree '{options.Name}' does not exist.", nameof(options));

        worktree.Lock(options.Reason ?? string.Empty);
    }

    /// <inheritdoc/>
    public void UnlockWorktree(GitWorktreeUnlockOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Worktree name is required.");

        using var repository = new Repository(options.RepositoryPath);

        var worktree = repository.Worktrees[options.Name]
            ?? throw new ArgumentException($"The worktree '{options.Name}' does not exist.", nameof(options));

        worktree.Unlock();
    }

    private static GitWorktreeInfo MapWorktree(Worktree worktree)
    {
        string path;
        try
        {
            using var worktreeRepo = worktree.WorktreeRepository;
            path = worktreeRepo.Info.WorkingDirectory?.TrimEnd(
                System.IO.Path.DirectorySeparatorChar,
                System.IO.Path.AltDirectorySeparatorChar) ?? string.Empty;
        }
        catch
        {
            path = string.Empty;
        }

        return new GitWorktreeInfo(
            worktree.Name,
            path,
            worktree.IsLocked,
            worktree.IsLocked ? worktree.LockReason : null);
    }
}
