using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Searches git commit history using optional pickaxe content search and
/// an optional PowerShell ScriptBlock predicate.
/// </summary>
/// <remarks>
/// <para>
/// <c>Select-GitCommit</c> walks commit history and returns <see cref="GitCommitInfo"/>
/// objects for every commit that matches the active filters. Three parameter sets are
/// available:
/// </para>
/// <list type="bullet">
///   <item>
///     <description><b>Contains</b> (default) — finds commits whose diff against the first
///     parent contains the plain-text substring supplied via <see cref="Contains"/>.
///     The comparison is case-sensitive and ordinal (e.g. <c>-Contains 'TODO'</c>).</description>
///   </item>
///   <item>
///     <description><b>Match</b> — finds commits whose diff against the first parent
///     contains a line matching the .NET regular expression supplied via <see cref="Match"/>.
///     Equivalent to <c>git log -G</c>.</description>
///   </item>
///   <item>
///     <description><b>Where</b> — filters via an arbitrary PowerShell ScriptBlock. The
///     block receives the raw <c>LibGit2Sharp.Commit</c> as both a <c>$commit</c> variable
///     and <c>$args[0]</c>, giving access to the full LibGit2Sharp object graph
///     (Author, Tree, Parents, Notes, etc.).</description>
///   </item>
/// </list>
/// <para>
/// <see cref="Where"/> can be combined with either <see cref="Contains"/> or
/// <see cref="Match"/>: supply a diff-search parameter and <c>-Where</c> together to
/// apply the diff search first and then run the ScriptBlock predicate on the surviving
/// candidates only.
/// </para>
/// <para>
/// <b>Performance note:</b> Walking large histories with a ScriptBlock per commit is
/// slower than native <c>git log</c>. Use <see cref="First"/> for early termination and
/// <see cref="Path"/> to narrow the candidate set.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Select, "GitCommit", DefaultParameterSetName = ContainsParameterSet)]
[OutputType(typeof(GitCommitInfo))]
public sealed class SelectGitCommitCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectGitCommitCmdlet"/> class.
    /// </summary>
    public SelectGitCommitCmdlet()
        : this(ServiceFactory.CreateGitCommitSearchService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectGitCommitCmdlet"/> class
    /// with an explicit service (used in unit tests).
    /// </summary>
    /// <param name="commitSearchService">The commit search service to use.</param>
    internal SelectGitCommitCmdlet(IGitCommitSearchService commitSearchService)
    {
        this.commitSearchService = commitSearchService
            ?? throw new ArgumentNullException(nameof(commitSearchService));
    }

    private const string ContainsParameterSet = "Contains";
    private const string MatchParameterSet = "Match";
    private const string WhereParameterSet = "Where";
    private readonly IGitCommitSearchService commitSearchService;

    /// <summary>
    /// Backing field for the <see cref="CancellationTokenSource"/> used to signal
    /// <see cref="StopProcessing"/> requests (e.g. Ctrl+C) to the search loop.
    /// </summary>
    private CancellationTokenSource? cts;

    /// <summary>
    /// Gets or sets a plain-text search string. Only commits whose diff against the
    /// first parent contains this substring (case-sensitive, ordinal) are returned.
    /// </summary>
    /// <example>
    ///   <code>Select-GitCommit -Contains 'TODO'</code>
    /// </example>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = ContainsParameterSet)]
    [Parameter(ParameterSetName = WhereParameterSet)]
    public string Contains { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a .NET regular expression to match against commit diff text.
    /// Only commits whose diff against the first parent contains a line matching this
    /// regex are returned. Equivalent to <c>git log -G &lt;pattern&gt;</c>.
    /// </summary>
    /// <example>
    ///   <code>Select-GitCommit -Match 'TODO|FIXME'</code>
    /// </example>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = MatchParameterSet)]
    [Parameter(ParameterSetName = WhereParameterSet)]
    public string Match { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a ScriptBlock predicate. The block receives the raw
    /// <c>LibGit2Sharp.Commit</c> as an injected <c>$commit</c> variable and as
    /// <c>$args[0]</c>. Return <see langword="$true"/> to include the commit in results.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = WhereParameterSet)]
    [GitScriptBlockCompleter]
    public ScriptBlock? Where { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of matching commits to return.
    /// </summary>
    [Parameter]
    [ValidateRange(1, int.MaxValue)]
    public int First { get; set; }

    /// <summary>
    /// Gets or sets the starting ref (branch name, tag, or commit SHA).
    /// When omitted, the walk starts from <c>HEAD</c>.
    /// </summary>
    [Parameter]
    [GitCommittishCompleter(IncludeBranches = true, IncludeTags = true, IncludeRelativeRefs = true)]
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets one or more repository-relative file paths. When set,
    /// only commits that touch at least one of these paths are candidates.
    /// </summary>
    [Parameter]
    [GitPathCompleter]
    public string[]? Path { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);
        var predicate = BuildPredicate();

        cts = new CancellationTokenSource();

        try
        {
            foreach (var commit in commitSearchService.Search(options, predicate, cts.Token))
            {
                WriteObject(commit);
            }
        }
        catch (OperationCanceledException)
        {
            // The search was cancelled via StopProcessing() (e.g. Ctrl+C).
            // This is a clean stop — do not write an error, matching PowerShell's
            // built-in Ctrl+C behaviour for long-running cmdlets.
        }
        catch (RuntimeException exception)
        {
            // ScriptBlock errors propagate as RuntimeException through the predicate delegate
            WriteError(exception.ErrorRecord);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "SearchGitCommitFailed",
                ErrorCategory.InvalidOperation,
                options.RepositoryPath));
        }
        finally
        {
            cts.Dispose();
            cts = null;
        }
    }

    /// <summary>
    /// Called by PowerShell on a separate thread when the user presses Ctrl+C or the
    /// pipeline is stopped. Signals the search loop to abort at the next commit boundary.
    /// </summary>
    protected override void StopProcessing()
    {
        cts?.Cancel();
    }

    /// <summary>
    /// Creates search options from cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">The current PowerShell file system path.</param>
    internal GitCommitSearchOptions BuildOptions(string currentFileSystemPath)
    {
        return new GitCommitSearchOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            From = From,
            MaxCount = IsParameterBound(nameof(First)) ? First : null,
            Paths = Path,
            Contains = IsParameterBound(nameof(Contains)) ? Contains : null,
            Match = IsParameterBound(nameof(Match)) ? Match : null,
        };
    }

    /// <summary>
    /// Wraps the <see cref="Where"/> ScriptBlock as a <c>Func&lt;object, bool&gt;</c>
    /// predicate, or returns <see langword="null"/> when no ScriptBlock was supplied.
    /// The commit is injected into the ScriptBlock scope as a <c>$commit</c> variable
    /// and is also available as <c>$args[0]</c>.
    /// </summary>
    internal Func<object, bool>? BuildPredicate()
    {
        if (Where is null)
        {
            return null;
        }

        var scriptBlock = Where;
        return commit =>
        {
            var variables = new List<PSVariable> { new PSVariable("commit", commit) };
            var results = scriptBlock.InvokeWithContext(null, variables, commit);
            return results.Count > 0 && LanguagePrimitives.IsTrue(results.First());
        };
    }
}
