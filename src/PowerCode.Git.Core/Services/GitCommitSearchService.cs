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

        // Compile content-search regex once, outside the per-commit loop
        Regex? contentRegex = null;
        if (!string.IsNullOrEmpty(options.ContentSearch) && options.ContentSearchIsRegex)
        {
            contentRegex = new Regex(
                options.ContentSearch,
                RegexOptions.Compiled | RegexOptions.Multiline);
        }

        var matched = 0;

        foreach (var commit in commits)
        {
            // Pickaxe content search: diff commit against first parent and scan patch text
            if (!string.IsNullOrEmpty(options.ContentSearch))
            {
                if (!PatchContainsSearch(repository, commit, options.ContentSearch, contentRegex))
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
    /// against its first parent contains <paramref name="search"/>. Each file's patch
    /// text is inspected independently so the search short-circuits on the first hit.
    /// </summary>
    private static bool PatchContainsSearch(
        Repository repository,
        Commit commit,
        string search,
        Regex? regex)
    {
        var parentTree = commit.Parents.FirstOrDefault()?.Tree;

        using var patch = repository.Diff.Compare<Patch>(parentTree, commit.Tree);

        foreach (var entry in patch)
        {
            var patchText = entry.Patch;
            if (string.IsNullOrEmpty(patchText))
            {
                continue;
            }

            if (regex is not null)
            {
                if (regex.IsMatch(patchText))
                {
                    return true;
                }
            }
            else
            {
                if (patchText.Contains(search, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
