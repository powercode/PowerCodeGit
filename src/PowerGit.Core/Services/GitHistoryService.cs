using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerGit.Core.Models;

namespace PowerGit.Core.Services;

/// <summary>
/// Reads git history information from repositories using LibGit2Sharp.
/// </summary>
public sealed class GitHistoryService : IGitHistoryService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options)
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

        var filter = new CommitFilter
        {
            SortBy = CommitSortStrategies.Time,
        };

        if (!string.IsNullOrWhiteSpace(options.BranchName))
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

        if (options.MaxCount is not null && options.MaxCount.Value > 0)
        {
            commits = commits.Take(options.MaxCount.Value);
        }

        return commits.Select(MapCommit).ToList();
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return source?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static GitCommitInfo MapCommit(Commit commit)
    {
        var parentShas = commit.Parents.Select(parent => parent.Sha).ToList();

        return new GitCommitInfo(
            commit.Sha,
            commit.Author.Name,
            commit.Author.Email,
            commit.Author.When,
            commit.Committer.Name,
            commit.Committer.Email,
            commit.Committer.When,
            commit.MessageShort,
            commit.Message,
            parentShas);
    }
}