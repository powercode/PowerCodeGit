using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PowerCode.Git.Completers;

/// <summary>
/// Provides argument completions for ScriptBlock parameters by offering
/// example ScriptBlocks that demonstrate available functionality.
/// </summary>
/// <remarks>
/// <para>
/// The completer selects examples based on the <c>commandName</c> and
/// <c>parameterName</c> supplied by the PowerShell completion engine.
/// Register examples for a command/parameter pair via
/// <see cref="RegisterExamples"/>, or rely on the built-in defaults that
/// ship with the module.
/// </para>
/// <para>
/// This completer returns static example ScriptBlocks and does not require
/// repository access. Each completion inserts a valid ScriptBlock literal
/// that the user can customise after selection.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [GitScriptBlockCompleter]
/// public ScriptBlock? Where { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitScriptBlockCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <inheritdoc/>
    public override IArgumentCompleter Create() => new ScriptBlockCompleter();

    /// <summary>
    /// A single completion example consisting of the ScriptBlock text, a
    /// short list label, and a descriptive tooltip.
    /// </summary>
    /// <param name="CompletionText">
    /// The ScriptBlock literal inserted on selection (e.g.
    /// <c>{ $commit.Author.Name -eq 'name' }</c>).
    /// </param>
    /// <param name="ListItemText">
    /// Short label shown in the completion list (e.g. "Author name equals").
    /// </param>
    /// <param name="ToolTip">
    /// Descriptive tooltip shown alongside the completion.
    /// </param>
    public readonly record struct Example(string CompletionText, string ListItemText, string ToolTip);

    /// <summary>
    /// Composite key used to look up examples in the registry.
    /// Both parts are compared case-insensitively.
    /// </summary>
    private readonly record struct RegistryKey(string CommandName, string ParameterName)
    {
        /// <inheritdoc/>
        public bool Equals(RegistryKey other) =>
            string.Equals(CommandName, other.CommandName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ParameterName, other.ParameterName, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(CommandName),
                StringComparer.OrdinalIgnoreCase.GetHashCode(ParameterName));
    }

    /// <summary>
    /// Thread-safe registry of examples keyed by (command, parameter).
    /// </summary>
    private static readonly Dictionary<RegistryKey, IReadOnlyList<Example>> Registry = new();
    private static readonly object RegistryLock = new();

    /// <summary>
    /// Registers (or replaces) the example set for a given command and
    /// parameter combination.
    /// </summary>
    /// <param name="commandName">
    /// The cmdlet verb-noun name (e.g. <c>Select-GitCommit</c>).
    /// </param>
    /// <param name="parameterName">
    /// The parameter name (e.g. <c>Where</c>).
    /// </param>
    /// <param name="examples">The examples to offer for that parameter.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="commandName"/> or <paramref name="parameterName"/> is
    /// <see langword="null"/> or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="examples"/> is <see langword="null"/>.
    /// </exception>
    public static void RegisterExamples(string commandName, string parameterName, IReadOnlyList<Example> examples)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        ArgumentNullException.ThrowIfNull(examples);

        lock (RegistryLock)
        {
            Registry[new RegistryKey(commandName, parameterName)] = examples;
        }
    }

    /// <summary>
    /// Returns the examples registered for the specified command and
    /// parameter, or an empty list when no examples have been registered.
    /// </summary>
    internal static IReadOnlyList<Example> GetExamples(string commandName, string parameterName)
    {
        lock (RegistryLock)
        {
            return Registry.TryGetValue(new RegistryKey(commandName, parameterName), out var examples)
                ? examples
                : [];
        }
    }

    /// <summary>
    /// Removes all registered examples. Intended for test isolation.
    /// </summary>
    internal static void ClearRegistry()
    {
        lock (RegistryLock)
        {
            Registry.Clear();
        }
    }

    // ------------------------------------------------------------------
    //  Built-in examples registered via the static constructor
    // ------------------------------------------------------------------

    static GitScriptBlockCompleterAttribute()
    {
        RegisterDefaults();
    }

    /// <summary>
    /// Registers the built-in example sets that ship with the module.
    /// Called from the static constructor and also available for tests
    /// that call <see cref="ClearRegistry"/> and need to restore defaults.
    /// </summary>
    internal static void RegisterDefaults()
    {
        RegisterExamples("Select-GitCommit", "Where", SelectGitCommitWhereExamples);
        RegisterExamples("Invoke-GitRepository", "Action", InvokeGitRepositoryActionExamples);
    }

    /// <summary>
    /// Example predicates for <c>Select-GitCommit -Where</c> demonstrating
    /// properties available on the raw <c>LibGit2Sharp.Commit</c> object
    /// via the injected <c>$commit</c> variable.
    /// </summary>
    internal static readonly IReadOnlyList<Example> SelectGitCommitWhereExamples =
    [
        new("{ $commit.Author.Name -eq 'name' }",
            "Author name equals",
            "Filter commits by exact author name. Replace 'name' with the desired author."),
        new("{ $commit.Author.Email -like '*@domain.com' }",
            "Author email domain",
            "Filter commits by author email domain. Replace 'domain.com' with the desired domain."),
        new("{ $commit.Author.When.DateTime -gt (Get-Date).AddDays(-7) }",
            "Authored in last 7 days",
            "Filter commits authored within the last 7 days. Adjust AddDays(-7) for a different window."),
        new("{ $commit.Committer.Name -ne $commit.Author.Name }",
            "Committer differs from author",
            "Find commits where the committer is not the same person as the author (e.g. cherry-picks, rebases)."),
        new("{ $commit.Parents.Count() -gt 1 }",
            "Merge commits",
            "Find merge commits — commits with more than one parent."),
        new("{ $commit.Parents.Count() -eq 1 }",
            "Non-merge commits",
            "Exclude merge commits — only return commits with exactly one parent."),
        new("{ $commit.MessageShort -match 'pattern' }",
            "Message matches regex",
            "Filter commits whose short message matches a regular expression. Replace 'pattern' with the desired regex."),
        new("{ $commit.Message -match '(?m)^BREAKING CHANGE:' }",
            "Breaking changes",
            "Find commits with a 'BREAKING CHANGE:' trailer in the full commit message."),
    ];

    /// <summary>
    /// Example actions for <c>Invoke-GitRepository -Action</c> demonstrating
    /// common operations on the raw <c>LibGit2Sharp.Repository</c> object.
    /// </summary>
    internal static readonly IReadOnlyList<Example> InvokeGitRepositoryActionExamples =
    [
        new("{ $repo.Head.Tip.Sha }",
            "HEAD commit SHA",
            "Return the full SHA of the commit HEAD points to."),
        new("{ $repo.Head.FriendlyName }",
            "Current branch name",
            "Return the friendly name of the current branch."),
        new("{ $repo.Refs | ForEach-Object { $_.CanonicalName } }",
            "List all refs",
            "Enumerate all references (branches, tags, HEAD) in the repository."),
        new("{ $repo.Tags | ForEach-Object { $_.FriendlyName } }",
            "List tag names",
            "Enumerate all tag names in the repository."),
        new("{ $repo.Network.Remotes | ForEach-Object { [pscustomobject]@{ Name = $_.Name; Url = $_.Url } } }",
            "List remotes",
            "Return remote name and URL pairs as objects."),
        new("{ $repo.Head.Tip.Author }",
            "HEAD commit author",
            "Return the author signature of the commit at HEAD."),
    ];

    internal sealed class ScriptBlockCompleter : IArgumentCompleter
    {
        /// <inheritdoc/>
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            try
            {
                var examples = GetExamples(commandName, parameterName);

                // Strip surrounding braces / whitespace so users can filter by
                // typing part of the content inside the braces.
                var filter = wordToComplete
                    .TrimStart('{')
                    .TrimEnd('}')
                    .Trim();

                return examples
                    .Where(e => Matches(e, filter))
                    .Select(e => new CompletionResult(
                        e.CompletionText,
                        e.ListItemText,
                        CompletionResultType.ParameterValue,
                        e.ToolTip));
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> when the example matches the user's
        /// partial input. An empty filter matches everything.
        /// </summary>
        private static bool Matches(Example example, string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }

            return example.CompletionText.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || example.ListItemText.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }
    }
}
