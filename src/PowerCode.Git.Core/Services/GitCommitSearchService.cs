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

        // Compile the diff-search pattern once, outside the per-commit loop.
        // -Like  → convert the PowerShell wildcard to a case-sensitive regex.
        // -Match → compile the caller-supplied regex directly.
        Regex? diffRegex = null;
        if (!string.IsNullOrEmpty(options.Like))
        {
            diffRegex = new Regex(
                WildcardToRegex(options.Like),
                RegexOptions.Compiled | RegexOptions.Multiline);
        }
        else if (!string.IsNullOrEmpty(options.Match))
        {
            diffRegex = new Regex(
                options.Match,
                RegexOptions.Compiled | RegexOptions.Multiline);
        }

        var matched = 0;

        foreach (var commit in commits)
        {
            // Diff-search: match the compiled regex against each changed hunk.
            if (diffRegex is not null)
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

    /// <summary>
    /// Converts a PowerShell wildcard pattern (<c>*</c> and <c>?</c>) to an
    /// equivalent .NET regular expression string.
    /// All regex metacharacters in the original pattern are escaped first so that
    /// only the wildcard characters retain special meaning.
    /// </summary>
    /// <param name="pattern">A PowerShell-style wildcard pattern, e.g. <c>*TODO*</c>.</param>
    /// <returns>A regex string that is semantically equivalent to the wildcard pattern.</returns>
    private static string WildcardToRegex(string pattern)
    {
        // Escape all regex metacharacters, then restore * and ? wildcard semantics.
        var escaped = Regex.Escape(pattern);

        // Regex.Escape turns * into \* and ? into \? — convert those back to .* and .
        return escaped
            .Replace(@"\*", ".*", StringComparison.Ordinal)
            .Replace(@"\?", ".", StringComparison.Ordinal);
    }
}
