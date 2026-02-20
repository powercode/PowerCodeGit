using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Completers;

/// <summary>
/// Provides argument completions for git branch names.
/// </summary>
/// <remarks>
/// Apply this attribute to a parameter that accepts a branch name. Set
/// <see cref="IncludeRemote"/> to <see langword="true"/> to also complete
/// remote-tracking branches.
/// </remarks>
/// <example>
/// <code>
/// [GitBranchCompleter(IncludeRemote = true)]
/// public string Name { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitBranchCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <summary>
    /// Gets or sets a value indicating whether remote-tracking branches
    /// should be included in the completion results.
    /// </summary>
    public bool IncludeRemote { get; set; }

    /// <inheritdoc/>
    public override IArgumentCompleter Create()
    {
        return new BranchCompleter(IncludeRemote, ServiceFactory.CreateGitBranchService());
    }

    internal sealed class BranchCompleter(bool includeRemote, IGitBranchService branchService) : IArgumentCompleter
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
                var branches = branchService.GetBranches(repositoryPath);

                return branches
                    .Where(b => includeRemote || !b.IsRemote)
                    .Where(b => b.Name.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    .Select(b => new CompletionResult(
                        b.Name,
                        b.Name,
                        CompletionResultType.ParameterValue,
                        b.IsRemote ? $"Remote: {b.Name}" : b.IsHead ? $"* {b.Name} (HEAD)" : b.Name));
            }
            catch
            {
                return [];
            }
        }
    }
}
