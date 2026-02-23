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

            foreach (var branch in branches)
            {
                WriteObject(branch);
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
        };
    }
}
