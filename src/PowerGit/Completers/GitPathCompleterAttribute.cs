using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using PowerGit.Abstractions.Services;

namespace PowerGit.Completers;

/// <summary>
/// Provides argument completions for tracked file paths in a git repository.
/// </summary>
/// <example>
/// <code>
/// [GitPathCompleter]
/// public string[] FilePath { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitPathCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <inheritdoc/>
    public override IArgumentCompleter Create()
    {
        return new PathCompleter(ServiceFactory.CreateGitPathService());
    }

    internal sealed class PathCompleter(IGitPathService pathService) : IArgumentCompleter
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
                    .Where(p => p.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
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
