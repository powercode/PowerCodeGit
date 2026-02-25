using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Reads git history information and creates commits using LibGit2Sharp.
/// </summary>
public sealed class GitHistoryService : IGitHistoryService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var filter = new CommitFilter
        {
            SortBy = CommitSortStrategies.Time,
        };

        if (options.AllBranches)
        {
            filter.IncludeReachableFrom = repository.Branches.Where(b => !b.IsRemote).Select(b => b.Tip);
        }
        else if (!string.IsNullOrWhiteSpace(options.BranchName))
        {
            var branch = repository.Branches[options.BranchName];
            if (branch is null)
            {
                throw new ArgumentException("The specified branch does not exist.", nameof(options));
            }

            filter.IncludeReachableFrom = branch;
        }

        var commits = repository.Commits.QueryBy(filter).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(options.AuthorFilter))
        {
            var authorFilter = options.AuthorFilter!;
            commits = commits.Where(commit =>
                ContainsIgnoreCase(commit.Author.Name, authorFilter) ||
                ContainsIgnoreCase(commit.Author.Email, authorFilter));
        }

        if (options.Since is not null)
        {
            commits = commits.Where(commit => commit.Author.When >= options.Since.Value);
        }

        if (options.Until is not null)
        {
            commits = commits.Where(commit => commit.Author.When <= options.Until.Value);
        }

        if (!string.IsNullOrWhiteSpace(options.MessagePattern))
        {
            var messagePattern = options.MessagePattern!;
            commits = commits.Where(commit => ContainsIgnoreCase(commit.Message, messagePattern));
        }

        if (options.Paths is { Length: > 0 })
        {
            var paths = options.Paths;
            commits = commits.Where(commit => CommitMapper.CommitTouchesAnyPath(repository, commit, paths));
        }

        if (options.FirstParent)
        {
            commits = FilterFirstParentOnly(commits);
        }

        if (options.NoMerges)
        {
            commits = commits.Where(c => c.Parents.Count() <= 1);
        }

        if (options.MaxCount is not null && options.MaxCount.Value > 0)
        {
            commits = commits.Take(options.MaxCount.Value);
        }

        var decorationMap = CommitMapper.BuildDecorationMap(repository);

        return commits.Select(c => CommitMapper.MapCommit(c, decorationMap)).ToList();
    }

    /// <summary>
    /// Filters a commit sequence to follow only the first parent of each commit
    /// (equivalent to <c>git log --first-parent</c>).
    /// </summary>
    private static IEnumerable<Commit> FilterFirstParentOnly(IEnumerable<Commit> commits)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var commit in commits)
        {
            if (!visited.Add(commit.Sha))
            {
                continue;
            }

            yield return commit;

            // Only follow the first parent's ancestry chain
            if (commit.Parents.Any())
            {
                visited.Add(commit.Parents.First().Sha);
            }
        }
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return source?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <inheritdoc/>
    public GitCommitInfo Commit(GitCommitOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var config = repository.Config;
        var configName = config.Get<string>("user.name")?.Value
            ?? throw new InvalidOperationException("Git user.name is not configured.");
        var configEmail = config.Get<string>("user.email")?.Value
            ?? throw new InvalidOperationException("Git user.email is not configured.");

        var when = options.Date ?? DateTimeOffset.Now;

        Signature authorSignature;
        if (!string.IsNullOrWhiteSpace(options.Author))
        {
            // Parse "Name <email>" format
            var authorStr = options.Author!;
            var emailStart = authorStr.LastIndexOf('<');
            var emailEnd = authorStr.LastIndexOf('>');

            if (emailStart >= 0 && emailEnd > emailStart)
            {
                var authorName = authorStr[..emailStart].Trim();
                var authorEmail = authorStr[(emailStart + 1)..emailEnd].Trim();
                authorSignature = new Signature(authorName, authorEmail, when);
            }
            else
            {
                authorSignature = new Signature(authorStr, configEmail, when);
            }
        }
        else
        {
            authorSignature = new Signature(configName, configEmail, when);
        }

        var committerSignature = new Signature(configName, configEmail, when);

        if (options.All)
        {
            // Stage all tracked modified files (git commit -a)
            var trackedPaths = repository.RetrieveStatus()
                .Where(e => !e.State.HasFlag(FileStatus.NewInWorkdir) && !e.State.HasFlag(FileStatus.Ignored))
                .Select(e => e.FilePath)
                .ToList();

            foreach (var path in trackedPaths)
            {
                Commands.Stage(repository, path);
            }
        }

        var message = options.Message;

        if (options.Amend)
        {
            message ??= repository.Head.Tip?.Message
                ?? throw new InvalidOperationException("No previous commit to amend.");

            var amendedCommit = repository.Commit(
                message,
                authorSignature,
                committerSignature,
                new CommitOptions { AmendPreviousCommit = true });

            return CommitMapper.MapCommit(amendedCommit);
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A commit message is required.", nameof(options));
        }

        var commitOptions = new CommitOptions
        {
            AllowEmptyCommit = options.AllowEmpty,
        };

        var newCommit = repository.Commit(message, authorSignature, committerSignature, commitOptions);
        return CommitMapper.MapCommit(newCommit);
    }
}
