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
/// Provides argument completions for committish values (commit SHAs / short
/// descriptions). The user can type a partial SHA or message substring and
/// matching commits will be offered.
/// </summary>
/// <remarks>
/// Configure the completer via properties:
/// <list type="bullet">
///   <item><see cref="MaxCount"/> — maximum number of recent commits to
///   consider (default 100).</item>
///   <item><see cref="AllBranches"/> — when <see langword="true"/>, include
///   commits reachable from all local branches, not just HEAD.</item>
/// </list>
/// Filtering is case-insensitive on the commit short SHA and message.
/// </remarks>
/// <example>
/// <code>
/// [GitCommittishCompleter(MaxCount = 50, AllBranches = true)]
/// public string Commit { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitCommittishCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <summary>
    /// Gets or sets the maximum number of recent commits to consider for
    /// completion. Defaults to <c>100</c>.
    /// </summary>
    public int MaxCount { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether commits reachable from all
    /// local branches should be included. When <see langword="false"/>, only
    /// commits reachable from HEAD are considered.
    /// </summary>
    public bool AllBranches { get; set; }

    /// <inheritdoc/>
    public override IArgumentCompleter Create()
    {
        return new CommittishCompleter(MaxCount, AllBranches, ServiceFactory.CreateGitHistoryService());
    }

    internal sealed class CommittishCompleter(int maxCount, bool allBranches, IGitHistoryService historyService) : IArgumentCompleter
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

                var options = new GitLogOptions
                {
                    RepositoryPath = repositoryPath,
                    MaxCount = maxCount,
                    AllBranches = allBranches,
                };

                var commits = historyService.GetLog(options);

                return commits
                    .Where(c => MatchesWord(c, wordToComplete))
                    .Select(c => new CompletionResult(
                        c.ShortSha,
                        $"{c.ShortSha} {c.MessageShort}",
                        CompletionResultType.ParameterValue,
                        $"{c.ShortSha} - {c.MessageShort} ({c.AuthorName}, {c.AuthorDate:yyyy-MM-dd})"));
            }
            catch
            {
                return [];
            }
        }

        private static bool MatchesWord(GitCommitInfo commit, string wordToComplete)
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
