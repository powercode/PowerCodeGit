using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using PowerCodeGit.Abstractions.Services;

namespace PowerCodeGit.Completers;

/// <summary>
/// Provides argument completions for git remote names, displaying the
/// remote URL as the tooltip.
/// </summary>
/// <example>
/// <code>
/// [GitRemoteCompleter]
/// public string Remote { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitRemoteCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <inheritdoc/>
    public override IArgumentCompleter Create()
    {
        return new RemoteCompleter(ServiceFactory.CreateGitRemoteService());
    }

    internal sealed class RemoteCompleter(IGitRemoteService remoteService) : IArgumentCompleter
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
                var remotes = remoteService.GetRemotes(repositoryPath);

                return remotes
                    .Where(r => r.Name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    .Select(r => new CompletionResult(
                        r.Name,
                        r.Name,
                        CompletionResultType.ParameterValue,
                        r.FetchUrl));
            }
            catch
            {
                return [];
            }
        }
    }
}
