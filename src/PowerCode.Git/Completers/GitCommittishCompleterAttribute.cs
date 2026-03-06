using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Completers;

/// <summary>
/// Provides argument completions for committish values.  By default only
/// recent commit SHAs and messages are offered.  Set the <c>Include*</c>
/// properties to also offer branch names, tags, and/or relative references
/// such as <c>HEAD~5</c>.
/// </summary>
/// <remarks>
/// Configure the completer via properties:
/// <list type="bullet">
///   <item><see cref="MaxCount"/> — maximum number of recent commits to
///   consider (default 100).</item>
///   <item><see cref="AllBranches"/> — when <see langword="true"/>, include
///   commits reachable from all local branches, not just HEAD.</item>
///   <item><see cref="IncludeBranches"/> — when <see langword="true"/>,
///   complete local branch names.</item>
///   <item><see cref="IncludeRemoteBranches"/> — when <see langword="true"/>,
///   also complete remote-tracking branch names (implies branches).</item>
///   <item><see cref="IncludeTags"/> — when <see langword="true"/>, complete
///   tag names.</item>
///   <item><see cref="IncludeRelativeRefs"/> — when <see langword="true"/>,
///   offer <c>HEAD~1</c> through <c>HEAD~10</c> and <c>HEAD^</c>.</item>
/// </list>
/// Filtering is case-insensitive on the commit short SHA and message.
/// </remarks>
/// <example>
/// <code>
/// [GitCommittishCompleter(MaxCount = 50, AllBranches = true)]
/// public string Commit { get; set; }
/// </code>
/// </example>
/// <example>
/// <code>
/// [GitCommittishCompleter(IncludeBranches = true, IncludeRemoteBranches = true, IncludeRelativeRefs = true)]
/// public string Upstream { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitCommittishCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <summary>
    /// Gets or sets the maximum number of recent commits to consider for
    /// completion. Defaults to <c>50</c>.
    /// </summary>
    public int MaxCount { get; set; } = 50;

    /// <summary>
    /// Gets or sets a value indicating whether commits reachable from all
    /// local branches should be included. When <see langword="false"/>, only
    /// commits reachable from HEAD are considered.
    /// </summary>
    public bool AllBranches { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether local branch names should be
    /// included in the completion results.
    /// </summary>
    public bool IncludeBranches { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether remote-tracking branch names
    /// should be included in the completion results. When
    /// <see langword="true"/>, local branches are also included.
    /// </summary>
    public bool IncludeRemoteBranches { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tag names should be included
    /// in the completion results.
    /// </summary>
    public bool IncludeTags { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether relative references such as
    /// <c>HEAD~1</c> through <c>HEAD~10</c> and <c>HEAD^</c> should be
    /// included in the completion results.
    /// </summary>
    public bool IncludeRelativeRefs { get; set; }

    /// <inheritdoc/>
    public override IArgumentCompleter Create()
    {
        var needBranches = IncludeBranches || IncludeRemoteBranches;
        IGitBranchService? branchService = needBranches ? ServiceFactory.CreateGitBranchService() : null;
        IGitTagService? tagService = IncludeTags ? ServiceFactory.CreateGitTagService() : null;

        return new CommittishCompleter(
            MaxCount,
            AllBranches,
            needBranches,
            IncludeRemoteBranches,
            IncludeTags,
            IncludeRelativeRefs,
            ServiceFactory.CreateGitHistoryService(),
            branchService,
            tagService);
    }

    /// <summary>
    /// Maximum depth used for generating <c>HEAD~N</c> completions.
    /// </summary>
    internal const int MaxRelativeDepth = 10;

    internal sealed class CommittishCompleter(
        int maxCount,
        bool allBranches,
        bool includeBranches,
        bool includeRemoteBranches,
        bool includeTags,
        bool includeRelativeRefs,
        IGitHistoryService historyService,
        IGitBranchService? branchService,
        IGitTagService? tagService) : IArgumentCompleter
    {
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            try
            {
                var repositoryPath = CompletionHelper.ResolveRepositoryPath(fakeBoundParameters);
                var results = new List<CompletionResult>();

                if (includeRelativeRefs)
                {
                    var headCommits = GetHeadCommits(repositoryPath);
                    results.AddRange(GetRelativeRefCompletions(wordToComplete, headCommits));
                }

                if (includeBranches && branchService is not null)
                {
                    results.AddRange(GetBranchCompletions(repositoryPath, wordToComplete));
                }

                if (includeTags && tagService is not null)
                {
                    results.AddRange(GetTagCompletions(repositoryPath, wordToComplete));
                }

                results.AddRange(GetCommitCompletions(repositoryPath, wordToComplete));

                return results;
            }
            catch
            {
                return [];
            }
        }

        private IEnumerable<CompletionResult> GetCommitCompletions(string repositoryPath, string wordToComplete)
        {
            var options = new GitLogOptions
            {
                RepositoryPath = repositoryPath,
                MaxCount = maxCount,
                AllBranches = allBranches,
            };

            var commits = historyService.GetLog(options);

            foreach (var commit in commits)
            {
                // Collect logical names (branches, tags, HEAD) from this commit's
                // decorations that are NOT already offered by the dedicated branch/tag
                // completers.  HEAD is always uncovered because no dedicated completer
                // offers it.
                var logicalNames = commit.Decorations
                    .Where(d => d.Type is GitDecorationType.Head
                             || (d.Type is GitDecorationType.LocalBranch && !includeBranches)
                             || (d.Type is GitDecorationType.RemoteBranch && !includeRemoteBranches)
                             || (d.Type is GitDecorationType.Tag && !includeTags))
                    .Select(d => d.Name)
                    .ToList();

                // Check logical names first — they take priority over SHA and must be
                // evaluated before MatchesCommit so that prefix-typing a ref name
                // (e.g. "ma" for "main") still surfaces the commit.
                var nameMatches = logicalNames
                    .Where(n => string.IsNullOrEmpty(wordToComplete)
                             || n.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (nameMatches.Count > 0)
                {
                    // Prefer logical names over the raw SHA.
                    foreach (var name in nameMatches)
                    {
                        yield return new CompletionResult(
                            name,
                            $"{name} {commit.MessageShort}",
                            CompletionResultType.ParameterValue,
                            $"{name} - {commit.MessageShort} ({commit.AuthorName}, {commit.AuthorDate:yyyy-MM-dd})");
                    }
                    continue; // Don't also emit the SHA for this commit.
                }

                // No logical name matched; fall back to SHA / message matching.
                if (!MatchesCommit(commit, wordToComplete))
                    continue;

                // Every decoration is already covered by the dedicated branch/tag completers.
                // Only emit the SHA when the user is explicitly typing a SHA prefix to avoid
                // duplicating completions already offered above.
                if (commit.Decorations.Count > 0 && logicalNames.Count == 0)
                {
                    var shaPrefix = !string.IsNullOrEmpty(wordToComplete)
                        && (commit.ShortSha.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase)
                            || commit.Sha.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase));

                    if (!shaPrefix)
                        continue;
                }

                yield return new CompletionResult(
                    commit.ShortSha,
                    $"{commit.ShortSha} {commit.MessageShort}",
                    CompletionResultType.ParameterValue,
                    $"{commit.ShortSha} - {commit.MessageShort} ({commit.AuthorName}, {commit.AuthorDate:yyyy-MM-dd})");
            }
        }

        private IEnumerable<CompletionResult> GetBranchCompletions(string repositoryPath, string wordToComplete)
        {
            var branches = branchService!.GetBranches(repositoryPath);

            return branches
                .Where(b => includeRemoteBranches || !b.IsRemote)
                .Where(b => b.Name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                .Select(b => new CompletionResult(
                    b.Name,
                    b.Name,
                    CompletionResultType.ParameterValue,
                    b.IsRemote ? $"Remote branch: {b.Name}" : b.IsHead ? $"* {b.Name} (HEAD)" : $"Branch: {b.Name}"));
        }

        private IEnumerable<CompletionResult> GetTagCompletions(string repositoryPath, string wordToComplete)
        {
            var tags = tagService!.GetTags(repositoryPath);

            return tags
                .Where(t => t.Name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                .Select(t => new CompletionResult(
                    t.Name,
                    t.Name,
                    CompletionResultType.ParameterValue,
                    t.IsAnnotated ? $"Tag: {t.Name} ({t.Message})" : $"Tag: {t.Name}"));
        }

        private IReadOnlyList<GitCommitInfo> GetHeadCommits(string repositoryPath)
        {
            var options = new GitLogOptions
            {
                RepositoryPath = repositoryPath,
                MaxCount = MaxRelativeDepth + 1,
                AllBranches = false,
            };
            return historyService.GetLog(options);
        }

        private static IEnumerable<CompletionResult> GetRelativeRefCompletions(
            string wordToComplete, IReadOnlyList<GitCommitInfo> headCommits)
        {
            var refs = new List<CompletionResult>();

            // HEAD^ (parent of HEAD) = ancestor at index 1
            var parent = headCommits.Count > 1 ? headCommits[1] : null;
            var caretTooltip = parent is not null
                ? $"HEAD^ — {parent.ShortSha} {parent.MessageShort}"
                : "HEAD^ — parent of HEAD";
            AddIfMatches(refs, "HEAD^", caretTooltip, wordToComplete);

            // HEAD~1 through HEAD~N
            for (var i = 1; i <= MaxRelativeDepth; i++)
            {
                var refText = $"HEAD~{i}";
                var commit = headCommits.Count > i ? headCommits[i] : null;
                var tooltip = commit is not null
                    ? $"{refText} — {commit.ShortSha} {commit.MessageShort}"
                    : $"{refText} — {i} commit{(i > 1 ? "s" : "")} before HEAD";
                AddIfMatches(refs, refText, tooltip, wordToComplete);
            }

            return refs;
        }

        private static void AddIfMatches(List<CompletionResult> results, string refText, string tooltip, string wordToComplete)
        {
            if (string.IsNullOrEmpty(wordToComplete) || refText.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new CompletionResult(
                    refText,
                    refText,
                    CompletionResultType.ParameterValue,
                    tooltip));
            }
        }

        private static bool MatchesCommit(GitCommitInfo commit, string wordToComplete)
        {
            if (string.IsNullOrEmpty(wordToComplete))
            {
                return true;
            }

            return commit.ShortSha.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase)
                || commit.Sha.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase)
                || commit.MessageShort.Contains(wordToComplete, StringComparison.OrdinalIgnoreCase);
        }
    }
}
