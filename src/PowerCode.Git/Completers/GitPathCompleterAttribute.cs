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
/// Provides argument completions for file paths in a git repository.
/// When <see cref="IncludeModified"/> or <see cref="IncludeUntracked"/> is set,
/// completions are filtered to only matching unstaged status entries;
/// otherwise all tracked paths are returned.
/// </summary>
/// <example>
/// <code>
/// [GitPathCompleter]
/// public string[] FilePath { get; set; }
/// </code>
/// </example>
/// <example>
/// <code>
/// [GitPathCompleter(IncludeModified = true, IncludeUntracked = true)]
/// public string[] Path { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitPathCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <summary>
    /// Gets or sets a value indicating whether modified (unstaged) files are included in completions.
    /// </summary>
    public bool IncludeModified { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether untracked files are included in completions.
    /// </summary>
    public bool IncludeUntracked { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether staged (index) files are included in completions.
    /// When set, only files with <see cref="GitStagingState.Staged"/> are returned.
    /// </summary>
    public bool IncludeStaged { get; set; }

    /// <inheritdoc/>
    public override IArgumentCompleter Create()
    {
        if (IncludeStaged)
        {
            return new StagedPathCompleter(ServiceFactory.CreateGitWorkingTreeService());
        }

        if (IncludeModified || IncludeUntracked)
        {
            return new StatusPathCompleter(
                ServiceFactory.CreateGitWorkingTreeService(),
                IncludeModified,
                IncludeUntracked);
        }

        return new TrackedPathCompleter(ServiceFactory.CreateGitPathService());
    }

    /// <summary>
    /// Completes against all tracked paths in the repository.
    /// </summary>
    internal sealed class TrackedPathCompleter(IGitPathService pathService) : IArgumentCompleter
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
                var paths = pathService.GetTrackedPaths(repositoryPath);

                return paths
                    .Where(p => p.Contains(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .Select(p => new CompletionResult(
                        p,
                        p,
                        CompletionResultType.ParameterValue,
                        p));
            }
            catch
            {
                return [];
            }
        }
    }

    /// <summary>
    /// Completes against staged (index) file paths in the repository.
    /// </summary>
    internal sealed class StagedPathCompleter(IGitWorkingTreeService workingTreeService) : IArgumentCompleter
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
                var statusResult = workingTreeService.GetStatus(new GitStatusOptions
                {
                    RepositoryPath = repositoryPath,
                });

                return statusResult.Entries
                    .Where(e => e.StagingState == GitStagingState.Staged)
                    .Select(e => e.FilePath)
                    .Where(p => p.Contains(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .Select(p => new CompletionResult(
                        p,
                        p,
                        CompletionResultType.ParameterValue,
                        p));
            }
            catch
            {
                return [];
            }
        }
    }

    /// <summary>
    /// Completes against unstaged file paths filtered by <see cref="GitFileStatus"/>.
    /// </summary>
    internal sealed class StatusPathCompleter(
        IGitWorkingTreeService workingTreeService,
        bool includeModified,
        bool includeUntracked) : IArgumentCompleter
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
                var statusResult = workingTreeService.GetStatus(new GitStatusOptions
                {
                    RepositoryPath = repositoryPath,
                    UntrackedFilesMode = includeUntracked ? GitUntrackedFilesMode.Normal : GitUntrackedFilesMode.No,
                });

                return statusResult.Entries
                    .Where(e => e.StagingState == GitStagingState.Unstaged)
                    .Where(e => (includeModified && e.Status is GitFileStatus.Modified or GitFileStatus.Deleted or GitFileStatus.Renamed)
                             || (includeUntracked && e.Status == GitFileStatus.Untracked))
                    .Select(e => e.FilePath)
                    .Where(p => p.Contains(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .Select(p => new CompletionResult(
                        p,
                        p,
                        CompletionResultType.ParameterValue,
                        p));
            }
            catch
            {
                return [];
            }
        }
    }
}
