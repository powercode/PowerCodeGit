using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Compares two tree-ish references and returns the file-level diff entries.
/// </summary>
/// <remarks>
/// <para>
/// <c>Compare-GitTree</c> computes the diff between two commits, branches, or tags
/// and emits one <see cref="GitDiffEntry"/> per changed file. This is the tree-to-tree
/// equivalent of <c>git diff &lt;base&gt; &lt;compare&gt;</c>.
/// </para>
/// <para>
/// The optional <see cref="Where"/> parameter accepts a PowerShell ScriptBlock that
/// filters results. The current <see cref="GitDiffEntry"/> is injected as a <c>$change</c>
/// variable and as <c>$args[0]</c>. Return <see langword="$true"/> to include the entry.
/// </para>
/// <para>
/// The optional <see cref="Transform"/> parameter accepts a PowerShell ScriptBlock that
/// transforms each result before it is written to the pipeline. The current
/// <see cref="GitDiffEntry"/> is injected as a <c>$change</c> variable and as
/// <c>$args[0]</c>. The ScriptBlock return value is emitted instead of the raw entry.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Compare, "GitTree")]
[OutputType(typeof(GitDiffEntry))]
public sealed class CompareGitTreeCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompareGitTreeCmdlet"/> class.
    /// </summary>
    public CompareGitTreeCmdlet()
        : this(ServiceFactory.CreateGitTreeComparisonService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareGitTreeCmdlet"/> class
    /// with an explicit service (used in unit tests).
    /// </summary>
    /// <param name="treeComparisonService">The tree comparison service to use.</param>
    internal CompareGitTreeCmdlet(IGitTreeComparisonService treeComparisonService)
    {
        this.treeComparisonService = treeComparisonService
            ?? throw new ArgumentNullException(nameof(treeComparisonService));
    }

    private readonly IGitTreeComparisonService treeComparisonService;

    /// <summary>
    /// Gets or sets the base tree-ish reference (branch, tag, or commit SHA).
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [GitCommittishCompleter(IncludeBranches = true, IncludeTags = true, IncludeRelativeRefs = true)]
    public string Base { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comparison tree-ish reference (branch, tag, or commit SHA).
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    [GitCommittishCompleter(IncludeBranches = true, IncludeTags = true, IncludeRelativeRefs = true)]
    public string Compare { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a ScriptBlock predicate that filters diff entries. The block
    /// receives the current <see cref="GitDiffEntry"/> as a <c>$change</c> variable
    /// and as <c>$args[0]</c>. Return <see langword="$true"/> to include the entry.
    /// </summary>
    [Parameter]
    [GitScriptBlockCompleter]
    public ScriptBlock? Where { get; set; }

    /// <summary>
    /// Gets or sets a ScriptBlock that transforms each diff entry before it is
    /// written to the pipeline. The block receives the current <see cref="GitDiffEntry"/>
    /// as a <c>$change</c> variable and as <c>$args[0]</c>.
    /// </summary>
    [Parameter]
    [GitScriptBlockCompleter]
    public ScriptBlock? Transform { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore whitespace changes.
    /// </summary>
    [Parameter]
    public SwitchParameter IgnoreWhitespace { get; set; }

    /// <summary>
    /// Gets or sets one or more pathspec patterns to restrict
    /// the comparison output.
    /// Supports git-style globs: <c>*</c> (single segment), <c>**</c> (cross-directory),
    /// <c>?</c> (single character), and directory prefixes (e.g. <c>src/</c>).
    /// </summary>
    [Parameter]
    [SupportsWildcards]
    [GitPathCompleter]
    public string[]? Path { get; set; }

    /// <summary>
    /// Creates tree comparison options from cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">The current PowerShell file system path.</param>
    /// <returns>A populated tree comparison options object.</returns>
    internal GitTreeCompareOptions BuildOptions(string currentFileSystemPath)
    {
        return new GitTreeCompareOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Base = Base,
            Compare = Compare,
            IgnoreWhitespace = IgnoreWhitespace.IsPresent,
            Paths = Path,
        };
    }

    /// <summary>
    /// Wraps the <see cref="Where"/> ScriptBlock as a <c>Func&lt;GitDiffEntry, bool&gt;</c>
    /// predicate, or returns <see langword="null"/> when no ScriptBlock was supplied.
    /// The entry is injected into the ScriptBlock scope as a <c>$change</c> variable
    /// and is also available as <c>$args[0]</c>.
    /// </summary>
    internal Func<GitDiffEntry, bool>? BuildWherePredicate()
    {
        if (Where is null)
        {
            return null;
        }

        var scriptBlock = Where;
        return entry =>
        {
            var variables = new List<PSVariable> { new PSVariable("change", entry) };
            var results = scriptBlock.InvokeWithContext(null, variables, entry);
            return results.Count > 0 && LanguagePrimitives.IsTrue(results.First());
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);
        var predicate = BuildWherePredicate();

        try
        {
            var entries = treeComparisonService.Compare(options);

            foreach (var entry in entries)
            {
                if (predicate is not null && !predicate(entry))
                {
                    continue;
                }

                if (Transform is not null)
                {
                    var variables = new List<PSVariable> { new PSVariable("change", entry) };
                    var results = Transform.InvokeWithContext(null, variables, entry);

                    foreach (var result in results)
                    {
                        WriteObject(result);
                    }
                }
                else
                {
                    WriteObject(entry);
                }
            }
        }
        catch (RuntimeException exception)
        {
            WriteError(exception.ErrorRecord);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "CompareGitTreeFailed",
                ErrorCategory.InvalidOperation,
                options.RepositoryPath));
        }
    }
}
