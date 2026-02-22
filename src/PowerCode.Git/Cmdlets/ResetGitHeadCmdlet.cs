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
[Cmdlet(VerbsCommon.Reset, "GitHead", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High, DefaultParameterSetName = "Mixed")]
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

    /// <summary>
    /// Gets or sets the revision to reset to. When omitted, resets to HEAD.
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = "Mixed")]
    [Parameter(Position = 0, ParameterSetName = "Soft")]
    [Parameter(Position = 0, ParameterSetName = "Hard")]
    [GitCommittishCompleter]
    public string? Revision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use hard reset mode.
    /// Resets index and working tree — all changes are discarded.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Hard")]
    public SwitchParameter Hard { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use soft reset mode.
    /// Only moves HEAD — index and working tree are unchanged.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Soft")]
    public SwitchParameter Soft { get; set; }

    /// <summary>
    /// Gets or sets the paths to reset (unstage). When specified, only the
    /// given files are unstaged and the <see cref="Revision"/> and mode parameters
    /// are ignored.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Paths", ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    [GitPathCompleter]
    public string[]? Path { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitResetOptions"/> instance.
    /// When specified, all other parameters are ignored.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitResetOptions? Options { get; set; }

    /// <summary>
    /// Builds a <see cref="GitResetOptions"/> from the current parameter set.
    /// </summary>
    /// <param name="currentFileSystemPath">The current working directory to use when resolving the repository path.</param>
    /// <returns>A fully populated <see cref="GitResetOptions"/> instance.</returns>
    internal GitResetOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == "Options")
        {
            return Options!;
        }

        var repositoryPath = ResolveRepositoryPath(currentFileSystemPath);

        if (ParameterSetName == "Paths")
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
                "Options" => "Reset HEAD",
                "Paths" => $"Reset {Path?.Length ?? 0} file(s)",
                _ => $"Reset HEAD to '{Revision ?? "HEAD"}' ({options.Mode})",
            };

            if (!ShouldProcess(options.RepositoryPath, description))
            {
                return;
            }

            workingTreeService.Reset(options);
        }
        catch (Exception exception)
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
