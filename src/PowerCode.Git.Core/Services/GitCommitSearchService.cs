using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Searches git commit history using optional pickaxe content search and
/// an optional caller-supplied predicate.
/// </summary>
public sealed class GitCommitSearchService : IGitCommitSearchService
{
    /// <inheritdoc/>
    public IEnumerable<GitCommitInfo> Search(GitCommitSearchOptions options, Func<object, bool>? predicate = null)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var filter = new CommitFilter
        {
            SortBy = CommitSortStrategies.Time,
        };

        if (!string.IsNullOrWhiteSpace(options.From))
        {
            // Try as branch name first, then as a generic committish (tag, SHA, HEAD~N, etc.)
            var branch = repository.Branches[options.From];
            if (branch is not null)
            {
                filter.IncludeReachableFrom = branch;
            }
            else
            {
                var commit = repository.Lookup<Commit>(options.From)
                    ?? throw new ArgumentException(
                        $"The ref '{options.From}' was not found in the repository.",
                        nameof(options));
                filter.IncludeReachableFrom = commit;
            }
        }

        var commits = repository.Commits.QueryBy(filter).AsEnumerable();

        // Path filter: only walk commits that touch at least one specified path
        if (options.Paths is { Length: > 0 })
        {
            var paths = options.Paths;
            commits = commits.Where(commit => CommitMapper.CommitTouchesAnyPath(repository, commit, paths));
        }

        var decorationMap = CommitMapper.BuildDecorationMap(repository);

        // Build the diff-search filter once, outside the per-commit loop.
        // -Contains → plain case-sensitive substring match (no regex needed).
        // -Match    → compile the caller-supplied regex directly.
        string? containsSearch = !string.IsNullOrEmpty(options.Contains) ? options.Contains : null;
        Regex? diffRegex = null;
        if (!string.IsNullOrEmpty(options.Match))
        {
            diffRegex = new Regex(
                options.Match,
                RegexOptions.Compiled | RegexOptions.Multiline);
        }

        var matched = 0;

        foreach (var commit in commits)
        {
            // Diff-search: plain substring or compiled regex against each changed hunk.
            if (containsSearch is not null)
            {
                if (!PatchContainsText(repository, commit, containsSearch))
                {
                    continue;
                }
            }
            else if (diffRegex is not null)
            {
                if (!PatchMatchesRegex(repository, commit, diffRegex))
                {
                    continue;
                }
            }

            // Caller-supplied predicate (wraps the PowerShell -Where ScriptBlock)
            if (predicate is not null && !predicate(commit))
            {
                continue;
            }

            yield return CommitMapper.MapCommit(commit, decorationMap);

            matched++;
            if (options.MaxCount is not null && matched >= options.MaxCount.Value)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if the unified diff of <paramref name="commit"/>
    /// against its first parent contains <paramref name="text"/> as a case-sensitive
    /// substring. Inspection short-circuits on the first hit.
    /// </summary>
    private static bool PatchContainsText(Repository repository, Commit commit, string text)
    {
        var parentTree = commit.Parents.FirstOrDefault()?.Tree;

        using var patch = repository.Diff.Compare<Patch>(parentTree, commit.Tree);

        foreach (var entry in patch)
        {
            var patchText = entry.Patch;
            if (!string.IsNullOrEmpty(patchText) && patchText.Contains(text, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if any hunk in the unified diff of
    /// <paramref name="commit"/> against its first parent matches
    /// <paramref name="regex"/>. Inspection short-circuits on the first hit.
    /// </summary>
    private static bool PatchMatchesRegex(Repository repository, Commit commit, Regex regex)
    {
        var parentTree = commit.Parents.FirstOrDefault()?.Tree;

        using var patch = repository.Diff.Compare<Patch>(parentTree, commit.Tree);

        foreach (var entry in patch)
        {
            var patchText = entry.Patch;
            if (!string.IsNullOrEmpty(patchText) && regex.IsMatch(patchText))
            {
                return true;
            }
        }

        return false;
    }

}
