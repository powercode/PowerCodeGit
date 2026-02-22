using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Aborts the current rebase operation and restores the branch to its
/// original state (<c>git rebase --abort</c>).
/// <example>
/// <code>Stop-GitRebase</code>
/// </example>
/// </summary>
[Cmdlet(VerbsLifecycle.Stop, "GitRebase", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
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

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath(SessionState.Path.CurrentFileSystemLocation.Path);

        if (!ShouldProcess(repositoryPath, "Abort rebase (git rebase --abort)"))
        {
            return;
        }

        try
        {
            rebaseService.Abort(repositoryPath);
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "StopGitRebaseFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
