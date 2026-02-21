using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Completers;

/// <summary>
/// Provides argument completions for git worktree names.
/// </summary>
/// <remarks>
/// Apply this attribute to a parameter that accepts a worktree name.
/// </remarks>
/// <example>
/// <code>
/// [GitWorktreeCompleter]
/// public string Name { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitWorktreeCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <inheritdoc/>
    public override IArgumentCompleter Create()
    {
        return new WorktreeCompleter(ServiceFactory.CreateGitWorktreeService());
    }

    internal sealed class WorktreeCompleter(IGitWorktreeService worktreeService) : IArgumentCompleter
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
                var worktrees = worktreeService.GetWorktrees(repositoryPath);

                return worktrees
                    .Where(w => w.Name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    .Select(w => new CompletionResult(
                        w.Name,
                        w.Name,
                        CompletionResultType.ParameterValue,
                        $"{w.Name} ({w.Path})"));
            }
            catch
            {
                return Array.Empty<CompletionResult>();
            }
        }
    }
}
