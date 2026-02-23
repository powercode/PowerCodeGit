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
/// Provides argument completions for modified file paths in a git repository.
/// When the parameter named by <see cref="StagedParameterName"/> is present in
/// the bound parameters, completions are drawn from staged (index) entries;
/// otherwise completions come from unstaged modified, deleted, or renamed entries.
/// </summary>
/// <example>
/// <code>
/// [GitModifiedPathCompleter(StagedParameterName = nameof(Staged))]
/// public string[] Path { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitModifiedPathCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <summary>
    /// Gets or sets the name of the switch parameter whose presence in the
    /// bound parameters indicates that staged (index) files should be
    /// completed instead of unstaged working-tree files.
    /// </summary>
    public string StagedParameterName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override IArgumentCompleter Create() =>
        new ModifiedPathCompleter(ServiceFactory.CreateGitWorkingTreeService(), StagedParameterName);

    /// <summary>
    /// Completes against modified file paths, switching between staged and
    /// unstaged entries based on whether the configured switch parameter is
    /// present in the bound parameters.
    /// </summary>
    internal sealed class ModifiedPathCompleter(
        IGitWorkingTreeService workingTreeService,
        string stagedParameterName) : IArgumentCompleter
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
                var wantStaged = !string.IsNullOrEmpty(stagedParameterName)
                              && fakeBoundParameters is not null
                              && fakeBoundParameters.Contains(stagedParameterName);

                var repositoryPath = CompletionHelper.ResolveRepositoryPath(fakeBoundParameters!);
                var statusResult = workingTreeService.GetStatus(new GitStatusOptions
                {
                    RepositoryPath = repositoryPath,
                });

                var entries = wantStaged
                    ? statusResult.Entries
                        .Where(e => e.StagingState == GitStagingState.Staged)
                    : statusResult.Entries
                        .Where(e => e.StagingState == GitStagingState.Unstaged)
                        .Where(e => e.Status is GitFileStatus.Modified or GitFileStatus.Deleted or GitFileStatus.Renamed);

                return entries
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
