using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Creates a new branch and checks it out (git branch + git switch).
/// <example>
/// <code>New-GitBranch -Name feature/my-feature</code>
/// </example>
/// <example>
/// <code>New-GitBranch -Name hotfix/p1 -StartPoint v2.0.0</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.New, "GitBranch", SupportsShouldProcess = true, DefaultParameterSetName = CreateParameterSet)]
[OutputType(typeof(GitBranchInfo))]
public sealed class NewGitBranchCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewGitBranchCmdlet"/> class.
    /// </summary>
    public NewGitBranchCmdlet()
        : this(ServiceFactory.CreateGitBranchService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewGitBranchCmdlet"/> class.
    /// </summary>
    /// <param name="branchService">The branch service used by the cmdlet.</param>
    internal NewGitBranchCmdlet(IGitBranchService branchService)
    {
        this.branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
    }

    private readonly IGitBranchService branchService;

    private const string CreateParameterSet = "Create";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the name of the new branch.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = CreateParameterSet)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start point (commit, tag, or branch) to branch from.
    /// Defaults to HEAD when not specified.
    /// Equivalent to the <c>[&lt;start-point&gt;]</c> argument of <c>git branch</c>.
    /// </summary>
    [Parameter(Position = 1, ParameterSetName = CreateParameterSet)]
    [GitCommittishCompleter(IncludeBranches = true, IncludeRemoteBranches = true, IncludeRelativeRefs = true)]
    public string? StartPoint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to set up the new branch to track the
    /// remote upstream branch. Equivalent to <c>git branch --track</c>.
    /// </summary>
    [Parameter(ParameterSetName = CreateParameterSet)]
    public SwitchParameter Track { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite an existing branch with the
    /// same name. Equivalent to <c>git branch -f</c>.
    /// </summary>
    [Parameter(ParameterSetName = CreateParameterSet)]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// Gets or sets an optional description for the branch. The value is stored in
    /// the local repository configuration as <c>branch.&lt;name&gt;.description</c>.
    /// </summary>
    [Parameter(ParameterSetName = CreateParameterSet)]
    [ValidateNotNullOrEmpty]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object for full control over branch creation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitBranchCreateOptions Options { get; set; } = null!;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        if (!ShouldProcess(options.RepositoryPath, $"Create branch '{options.Name}'"))
        {
            return;
        }

        try
        {
            var result = branchService.CreateBranch(options);
            WriteObject(result);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "NewGitBranchFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    /// <summary>
    /// Builds a <see cref="GitBranchCreateOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitBranchCreateOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitBranchCreateOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            StartPoint = StartPoint,
            Track = Track.IsPresent,
            Force = Force.IsPresent,
            Description = Description,
        };
    }
}
