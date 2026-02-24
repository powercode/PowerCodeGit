using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Lists branches in a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitBranch", DefaultParameterSetName = ListParameterSet)]
[OutputType(typeof(GitBranchInfo))]
public sealed class GetGitBranchCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitBranchCmdlet"/> class.
    /// </summary>
    public GetGitBranchCmdlet()
        : this(ServiceFactory.CreateGitBranchService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitBranchCmdlet"/> class.
    /// </summary>
    /// <param name="branchService">The branch service used by the cmdlet.</param>
    internal GetGitBranchCmdlet(IGitBranchService branchService)
    {
        this.branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
    }

    private const string ListParameterSet = "List";
    private const string OptionsParameterSet = "Options";
    private readonly IGitBranchService branchService;

    /// <summary>
    /// Gets or sets a value indicating whether to list only remote-tracking branches.
    /// Equivalent to <c>git branch -r</c>.
    /// </summary>
    [Parameter(ParameterSetName = ListParameterSet)]
    public SwitchParameter Remote { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to list both local and remote-tracking branches.
    /// Equivalent to <c>git branch -a</c>.
    /// </summary>
    [Parameter(ParameterSetName = ListParameterSet)]
    // git -a muscle-memory alias
    [Alias("a")]
    public SwitchParameter All { get; set; }

    /// <summary>
    /// Gets or sets a glob pattern to filter branch names.
    /// Equivalent to <c>git branch -l &lt;pattern&gt;</c>.
    /// </summary>
    [Parameter(ParameterSetName = ListParameterSet)]
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets a committish; only branches containing this commit are shown.
    /// Equivalent to <c>git branch --contains &lt;commit&gt;</c>.
    /// </summary>
    [Parameter(ParameterSetName = ListParameterSet)]
    [GitCommittishCompleter]
    public string? Contains { get; set; }

    /// <summary>
    /// Gets or sets a committish; only branches merged into this commit are shown.
    /// Equivalent to <c>git branch --merged [&lt;commit&gt;]</c>.
    /// </summary>
    [Parameter(ParameterSetName = ListParameterSet)]
    [GitCommittishCompleter]
    public string? Merged { get; set; }

    /// <summary>
    /// Gets or sets a committish; only branches NOT merged into this commit are shown.
    /// Equivalent to <c>git branch --no-merged [&lt;commit&gt;]</c>.
    /// </summary>
    [Parameter(ParameterSetName = ListParameterSet)]
    [GitCommittishCompleter]
    public string? NoMerged { get; set; }

    /// <summary>
    /// Gets or sets wildcard patterns used to include branches by name.
    /// Only branches matching at least one pattern are returned.
    /// </summary>
    [Parameter(ParameterSetName = ListParameterSet, Position = 0)]
    [SupportsWildcards]
    public string[]? Include { get; set; }

    /// <summary>
    /// Gets or sets wildcard patterns used to exclude branches by name.
    /// Branches matching any pattern are removed from the result.
    /// </summary>
    [Parameter(ParameterSetName = ListParameterSet)]
    [SupportsWildcards]
    public string[]? Exclude { get; set; }

    /// <summary>
    /// Gets or sets the name of a branch to compare each result against,
    /// computing ahead/behind divergence (e.g. <c>origin/master</c>).
    /// When set, each returned <see cref="GitBranchInfo"/> will have its
    /// <see cref="GitBranchInfo.ReferenceComparison"/> property populated.
    /// </summary>
    [Parameter(ParameterSetName = ListParameterSet)]
    [GitCommittishCompleter]
    public string? ReferenceBranch { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object for full control over branch listing.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitBranchListOptions Options { get; set; } = null!;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        try
        {
            var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);
            var branches = branchService.GetBranches(options);
            var hasReference = !string.IsNullOrEmpty(options.ReferenceBranch);

            foreach (var branch in branches)
            {
                WriteObject(CreateOutputObject(branch, hasReference));
            }
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "GetGitBranchFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    /// <summary>
    /// Wraps a <see cref="GitBranchInfo"/> in a <see cref="PSObject"/> and, when reference
    /// comparison data is present, prepends the <c>#WithReference</c> synthetic typename so that
    /// the formatter selects the view that includes the Reference column.
    /// </summary>
    /// <param name="branch">The branch info to wrap.</param>
    /// <param name="hasReference">
    /// <see langword="true"/> when <c>-ReferenceBranch</c> was specified, causing the wider
    /// formatting view to be selected.
    /// </param>
    /// <returns>A <see cref="PSObject"/> ready to pass to <see cref="Cmdlet.WriteObject(object)"/>.</returns>
    internal static PSObject CreateOutputObject(GitBranchInfo branch, bool hasReference)
    {
        var pso = PSObject.AsPSObject(branch);

        if (hasReference)
        {
            // Add a synthetic typename so the formatter can select a view that includes
            // the Reference comparison columns (ahead/behind vs. the reference branch).
            pso.TypeNames.Insert(0, "PowerCode.Git.Abstractions.Models.GitBranchInfo#WithReference");
        }

        return pso;
    }

    /// <summary>
    /// Builds a <see cref="GitBranchListOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitBranchListOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitBranchListOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            ListRemote = Remote.IsPresent,
            ListAll = All.IsPresent,
            Pattern = Pattern,
            ContainsCommit = Contains,
            MergedInto = Merged,
            NotMergedInto = NoMerged,
            Include = Include,
            Exclude = Exclude,
            ReferenceBranch = ReferenceBranch,
        };
    }
}
