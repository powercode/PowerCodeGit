using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Resets the current HEAD to a specified revision (git reset).
/// <example>
/// <code>Reset-GitHead</code>
/// </example>
/// <example>
/// <code>Reset-GitHead -Revision HEAD~1 -Hard</code>
/// </example>
/// <example>
/// <code>Reset-GitHead -Path file.txt</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Reset, "GitHead", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High, DefaultParameterSetName = MixedParameterSet)]
public sealed class ResetGitHeadCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetGitHeadCmdlet"/> class.
    /// </summary>
    public ResetGitHeadCmdlet()
        : this(ServiceFactory.CreateGitWorkingTreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetGitHeadCmdlet"/> class.
    /// </summary>
    /// <param name="workingTreeService">The working tree service used by the cmdlet.</param>
    internal ResetGitHeadCmdlet(IGitWorkingTreeService workingTreeService)
    {
        this.workingTreeService = workingTreeService ?? throw new ArgumentNullException(nameof(workingTreeService));
    }

    private readonly IGitWorkingTreeService workingTreeService;

    private const string MixedParameterSet = "Mixed";
    private const string SoftParameterSet = "Soft";
    private const string HardParameterSet = "Hard";
    private const string PathsParameterSet = "Paths";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the revision to reset to. When omitted, resets to HEAD.
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = MixedParameterSet)]
    [Parameter(Position = 0, ParameterSetName = SoftParameterSet)]
    [Parameter(Position = 0, ParameterSetName = HardParameterSet)]
    [GitCommittishCompleter(IncludeBranches = true, IncludeTags = true, IncludeRelativeRefs = true)]
    public string? Revision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use hard reset mode.
    /// Resets index and working tree — all changes are discarded.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = HardParameterSet)]
    public SwitchParameter Hard { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use soft reset mode.
    /// Only moves HEAD — index and working tree are unchanged.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = SoftParameterSet)]
    public SwitchParameter Soft { get; set; }

    /// <summary>
    /// Gets or sets the paths to reset (unstage). When specified, only the
    /// given files are unstaged and the <see cref="Revision"/> and mode parameters
    /// are ignored.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = PathsParameterSet, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    [GitPathCompleter(IncludeStaged = true)]
    public string[]? Path { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitResetOptions"/> instance.
    /// When specified, all other parameters are ignored.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitResetOptions? Options { get; set; }

    /// <summary>
    /// Builds a <see cref="GitResetOptions"/> from the current parameter set.
    /// </summary>
    /// <param name="currentFileSystemPath">The current working directory to use when resolving the repository path.</param>
    /// <returns>A fully populated <see cref="GitResetOptions"/> instance.</returns>
    internal GitResetOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options!;
        }

        var repositoryPath = ResolveRepositoryPath(currentFileSystemPath);

        if (ParameterSetName == PathsParameterSet)
        {
            return new GitResetOptions
            {
                RepositoryPath = repositoryPath,
                Paths = Path,
            };
        }

        return new GitResetOptions
        {
            RepositoryPath = repositoryPath,
            Revision = Revision,
            Mode = ResolveResetMode(),
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        try
        {
            var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

            var description = ParameterSetName switch
            {
                OptionsParameterSet => "Reset HEAD",
                PathsParameterSet => $"Reset {Path?.Length ?? 0} file(s)",
                _ => $"Reset HEAD to '{Revision ?? "HEAD"}' ({options.Mode})",
            };

            if (!ShouldProcess(options.RepositoryPath, description))
            {
                return;
            }

            workingTreeService.Reset(options);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "ResetGitHeadFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    private GitResetMode ResolveResetMode()
    {
        if (Hard.IsPresent)
        {
            return GitResetMode.Hard;
        }

        if (Soft.IsPresent)
        {
            return GitResetMode.Soft;
        }

        return GitResetMode.Mixed;
    }
}
