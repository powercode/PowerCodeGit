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
            commits = commits.Where(commit => CommitTouchesAnyPath(repository, commit, paths));
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

        var decorationMap = BuildDecorationMap(repository);

        return commits.Select(c => MapCommit(c, decorationMap)).ToList();
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

    /// <summary>
    /// Builds a lookup from commit SHA to the list of ref-name decorations
    /// (HEAD, local branches, remote branches, tags) pointing at that commit.
    /// </summary>
    private static Dictionary<string, List<GitDecoration>> BuildDecorationMap(Repository repository)
    {
        var map = new Dictionary<string, List<GitDecoration>>(StringComparer.OrdinalIgnoreCase);

        // HEAD pointer
        if (repository.Head?.Tip is not null)
        {
            var headSha = repository.Head.Tip.Sha;
            GetOrCreate(map, headSha).Add(new GitDecoration("HEAD", GitDecorationType.Head));
        }

        // Branches (local and remote)
        foreach (var branch in repository.Branches)
        {
            if (branch.Tip is null)
            {
                continue;
            }

            var type = branch.IsRemote ? GitDecorationType.RemoteBranch : GitDecorationType.LocalBranch;
            GetOrCreate(map, branch.Tip.Sha).Add(new GitDecoration(branch.FriendlyName, type));
        }

        // Tags
        foreach (var tag in repository.Tags)
        {
            var targetSha = (tag.PeeledTarget ?? tag.Target).Sha;
            GetOrCreate(map, targetSha).Add(new GitDecoration($"tag: {tag.FriendlyName}", GitDecorationType.Tag));
        }

        return map;
    }

    private static List<GitDecoration> GetOrCreate(Dictionary<string, List<GitDecoration>> map, string sha)
    {
        if (!map.TryGetValue(sha, out var list))
        {
            list = [];
            map[sha] = list;
        }

        return list;
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return source?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool CommitTouchesAnyPath(Repository repository, Commit commit, string[] paths)
    {
        var parentTree = commit.Parents.FirstOrDefault()?.Tree;

        using var changes = repository.Diff.Compare<TreeChanges>(parentTree, commit.Tree);

        return changes.Any(change =>
            paths.Any(p =>
                string.Equals(change.Path, p, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(change.OldPath, p, StringComparison.OrdinalIgnoreCase)));
    }

    private static GitCommitInfo MapCommit(Commit commit, Dictionary<string, List<GitDecoration>>? decorationMap = null)
    {
        var parentShas = commit.Parents.Select(parent => parent.Sha).ToList();
        List<GitDecoration>? decorations = null;
        decorationMap?.TryGetValue(commit.Sha, out decorations);

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
            parentShas,
            decorations);
    }

    /// <inheritdoc/>
    public GitCommitInfo Commit(GitCommitOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var config = repository.Config;
        var name = config.Get<string>("user.name")?.Value
            ?? throw new InvalidOperationException("Git user.name is not configured.");
        var email = config.Get<string>("user.email")?.Value
            ?? throw new InvalidOperationException("Git user.email is not configured.");
        var signature = new Signature(name, email, DateTimeOffset.Now);

        var message = options.Message;

        if (options.Amend)
        {
            message ??= repository.Head.Tip?.Message
                ?? throw new InvalidOperationException("No previous commit to amend.");

            var amendedCommit = repository.Commit(
                message,
                signature,
                signature,
                new CommitOptions { AmendPreviousCommit = true });

            return MapCommit(amendedCommit);
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A commit message is required.", nameof(options));
        }

        var commitOptions = new CommitOptions
        {
            AllowEmptyCommit = options.AllowEmpty,
        };

        var newCommit = repository.Commit(message, signature, signature, commitOptions);
        return MapCommit(newCommit);
    }
}
