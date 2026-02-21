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
    public IReadOnlyList<GitWorktreeInfo> GetWorktrees(string repositoryPath)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));

        using var repository = new Repository(repositoryPath);
        return repository.Worktrees.Select(MapWorktree).ToList();
    }

    /// <inheritdoc/>
    public GitWorktreeInfo AddWorktree(GitWorktreeAddOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Worktree name is required.");
        RepositoryGuard.ValidateRequiredString(options.Path, nameof(options), "Worktree path is required.");

        using var repository = new Repository(options.RepositoryPath);

        Worktree worktree;
        if (options.Branch is not null)
        {
            worktree = repository.Worktrees.Add(options.Branch, options.Name, options.Path, options.Locked);
        }
        else
        {
            worktree = repository.Worktrees.Add(options.Name, options.Path, options.Locked);
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
