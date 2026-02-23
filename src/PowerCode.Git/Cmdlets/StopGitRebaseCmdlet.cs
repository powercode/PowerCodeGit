using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Aborts the current rebase operation and restores the branch to its
/// original state (<c>git rebase --abort</c>).
/// <example>
/// <code>Stop-GitRebase</code>
/// </example>
/// </summary>
[Cmdlet(VerbsLifecycle.Stop, "GitRebase", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High, DefaultParameterSetName = AbortParameterSet)]
public sealed class StopGitRebaseCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StopGitRebaseCmdlet"/> class.
    /// </summary>
    public StopGitRebaseCmdlet()
        : this(ServiceFactory.CreateGitRebaseService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StopGitRebaseCmdlet"/> class.
    /// </summary>
    /// <param name="rebaseService">The rebase service used by the cmdlet.</param>
    internal StopGitRebaseCmdlet(IGitRebaseService rebaseService)
    {
        this.rebaseService = rebaseService ?? throw new ArgumentNullException(nameof(rebaseService));
    }

    private readonly IGitRebaseService rebaseService;

    private const string AbortParameterSet = "Abort";
    private const string OptionsParameterSet = "Options";

    // ── Options parameter set ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a pre-built options object, allowing full control over the abort operation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitStopRebaseOptions? Options { get; set; }

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="GitStopRebaseOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitStopRebaseOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options!;
        }

        return new GitStopRebaseOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
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

            if (!ShouldProcess(options.RepositoryPath, "Abort rebase (git rebase --abort)"))
            {
                return;
            }

            rebaseService.Abort(options);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "StopGitRebaseFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
