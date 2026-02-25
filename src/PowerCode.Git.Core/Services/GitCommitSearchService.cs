using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
    public IEnumerable<GitCommitInfo> Search(GitCommitSearchOptions options, Func<object, bool>? predicate = null, CancellationToken cancellationToken = default)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var filter = BuildCommitFilter(repository, options);
        var candidates = BuildCandidates(repository, filter, options.Paths);
        var decorationMap = CommitMapper.BuildDecorationMap(repository);
        var (containsSearch, diffRegex) = CompileDiffFilter(options);

        var matched = 0;

        foreach (var commit in candidates)
        {
            // Check for cancellation (e.g. Ctrl+C) before the expensive diff computation.
            cancellationToken.ThrowIfCancellationRequested();

            if (!MatchesDiffFilter(repository, commit, containsSearch, diffRegex))
            {
                continue;
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
    /// Builds a <see cref="CommitFilter"/> for the given <paramref name="options"/>,
    /// resolving the optional starting ref as a branch name first, then a tag name
    /// (peeling annotated tags), then as a generic committish (SHA, <c>HEAD~N</c>, etc.).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="GitCommitSearchOptions.From"/> is set but not found in the repository.
    /// </exception>
    private static CommitFilter BuildCommitFilter(Repository repository, GitCommitSearchOptions options)
    {
        var filter = new CommitFilter
        {
            SortBy = CommitSortStrategies.Time,
        };

        if (string.IsNullOrWhiteSpace(options.From))
        {
            return filter;
        }

        // Try as branch name first, then tag name, then as a generic committish (SHA, HEAD~N, etc.)
        var branch = repository.Branches[options.From];
        if (branch is not null)
        {
            filter.IncludeReachableFrom = branch;
        }
        else if (repository.Tags[options.From] is { } tag)
        {
            // Peel annotated tags to the underlying commit.
            var target = tag.PeeledTarget ?? tag.Target;
            var tagCommit = target as Commit
                ?? repository.Lookup<Commit>(target.Id)
                ?? throw new ArgumentException(
                    $"The tag '{options.From}' does not point to a commit.",
                    nameof(options));
            filter.IncludeReachableFrom = tagCommit;
        }
        else
        {
            var commit = repository.Lookup<Commit>(options.From)
                ?? throw new ArgumentException(
                    $"The ref '{options.From}' was not found in the repository.",
                    nameof(options));
            filter.IncludeReachableFrom = commit;
        }

        return filter;
    }

    /// <summary>
    /// Returns the candidate commit sequence for the given <paramref name="filter"/>,
    /// optionally narrowed to commits that touch at least one of <paramref name="paths"/>.
    /// </summary>
    private static IEnumerable<Commit> BuildCandidates(Repository repository, CommitFilter filter, string[]? paths)
    {
        var commits = repository.Commits.QueryBy(filter).AsEnumerable();

        if (paths is { Length: > 0 })
        {
            commits = commits.Where(commit => CommitMapper.CommitTouchesAnyPath(repository, commit, paths));
        }

        return commits;
    }

    /// <summary>
    /// Compiles the diff-search parameters from <paramref name="options"/> once, outside
    /// the per-commit loop. Returns a plain-text needle for <c>-Contains</c> or a compiled
    /// <see cref="Regex"/> for <c>-Match</c>; both are <see langword="null"/> when the
    /// corresponding option is absent.
    /// </summary>
    private static (string? ContainsSearch, Regex? DiffRegex) CompileDiffFilter(GitCommitSearchOptions options)
    {
        // -Contains → plain case-sensitive substring match (no regex needed).
        var containsSearch = !string.IsNullOrEmpty(options.Contains) ? options.Contains : null;

        // -Match → compile the caller-supplied regex directly.
        Regex? diffRegex = null;
        if (!string.IsNullOrEmpty(options.Match))
        {
            diffRegex = new Regex(options.Match, RegexOptions.Compiled | RegexOptions.Multiline);
        }

        return (containsSearch, diffRegex);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the commit passes the active diff filter.
    /// When neither <paramref name="containsSearch"/> nor <paramref name="diffRegex"/> is
    /// set, the commit is unconditionally accepted.
    /// </summary>
    private static bool MatchesDiffFilter(Repository repository, Commit commit, string? containsSearch, Regex? diffRegex)
    {
        if (containsSearch is not null)
        {
            return PatchContainsText(repository, commit, containsSearch);
        }

        if (diffRegex is not null)
        {
            return PatchMatchesRegex(repository, commit, diffRegex);
        }

        return true;
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
