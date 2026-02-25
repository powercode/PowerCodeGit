using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
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
/// <c>Search-GitCommit</c> walks commit history and returns <see cref="GitCommitInfo"/>
/// objects for every commit that matches the active filters. Two parameter sets are
/// available:
/// </para>
/// <list type="bullet">
///   <item>
///     <description><b>Search</b> (default) — finds commits whose diff against the first
///     parent contains the text or pattern supplied via <see cref="ContentSearch"/>. This is
///     the equivalent of <c>git log -S</c> (plain string) or <c>git log -G</c> (regex).</description>
///   </item>
///   <item>
///     <description><b>Where</b> — filters via an arbitrary PowerShell ScriptBlock. The block
///     receives the raw <c>LibGit2Sharp.Commit</c> as <c>$args[0]</c>, giving access to the
///     full LibGit2Sharp object graph (Author, Tree, Parents, Notes, etc.).</description>
///   </item>
/// </list>
/// <para>
/// Both filters can be combined: supply <see cref="ContentSearch"/> (Search set) and
/// <see cref="Where"/> together to apply content search first, then the ScriptBlock
/// predicate on the surviving candidates.
/// </para>
/// <para>
/// <b>Performance note:</b> Walking large histories with a ScriptBlock per commit is
/// slower than native <c>git log</c>. Use <see cref="First"/> for early termination and
/// <see cref="Path"/> to narrow the candidate set.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Search, "GitCommit", DefaultParameterSetName = SearchParameterSet)]
[OutputType(typeof(GitCommitInfo))]
public sealed class SearchGitCommitCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchGitCommitCmdlet"/> class.
    /// </summary>
    public SearchGitCommitCmdlet()
        : this(ServiceFactory.CreateGitCommitSearchService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchGitCommitCmdlet"/> class
    /// with an explicit service (used in unit tests).
    /// </summary>
    /// <param name="commitSearchService">The commit search service to use.</param>
    internal SearchGitCommitCmdlet(IGitCommitSearchService commitSearchService)
    {
        this.commitSearchService = commitSearchService
            ?? throw new ArgumentNullException(nameof(commitSearchService));
    }

    private const string SearchParameterSet = "Search";
    private const string WhereParameterSet = "Where";
    private readonly IGitCommitSearchService commitSearchService;

    /// <summary>
    /// Gets or sets the pickaxe content search string. Only commits whose diff
    /// against the first parent contains this string (or matches this pattern
    /// when <see cref="ContentSearchIsRegex"/> is set) are returned.
    /// Equivalent to <c>git log -S</c> (plain) or <c>git log -G</c> (regex).
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = SearchParameterSet)]
    public string ContentSearch { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ContentSearch"/> is
    /// treated as a regular expression. When specified, the pattern must appear
    /// in the diff of the matching commit (equivalent to <c>git log -G</c>).
    /// </summary>
    [Parameter(ParameterSetName = SearchParameterSet)]
    public SwitchParameter ContentSearchIsRegex { get; set; }

    /// <summary>
    /// Gets or sets a ScriptBlock predicate. The block receives the raw
    /// <c>LibGit2Sharp.Commit</c> as <c>$args[0]</c>. Return <see langword="$true"/>
    /// to include the commit in results.
    /// </summary>
    [Parameter(ParameterSetName = SearchParameterSet)]
    [Parameter(Mandatory = true, ParameterSetName = WhereParameterSet)]
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
    [GitCommittishCompleter(IncludeBranches = true, IncludeTags = true)]
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

        try
        {
            foreach (var commit in commitSearchService.Search(options, predicate))
            {
                WriteObject(commit);
            }
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
            ContentSearch = ContentSearch,
            ContentSearchIsRegex = ContentSearchIsRegex.IsPresent,
        };
    }

    /// <summary>
    /// Wraps the <see cref="Where"/> ScriptBlock as a <c>Func&lt;object, bool&gt;</c>
    /// predicate, or returns <see langword="null"/> when no ScriptBlock was supplied.
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
            var results = scriptBlock.InvokeWithContext(null, [], commit);
            return results.Count > 0 && LanguagePrimitives.IsTrue(results.First());
        };
    }
}
