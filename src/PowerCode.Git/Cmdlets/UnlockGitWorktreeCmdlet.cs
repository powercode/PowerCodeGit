using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Unlocks a previously locked worktree in a git repository.
/// <example>
/// <code>Unlock-GitWorktree -Name feature</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Unlock, "GitWorktree", SupportsShouldProcess = true, DefaultParameterSetName = "Unlock")]
public sealed class UnlockGitWorktreeCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnlockGitWorktreeCmdlet"/> class.
    /// </summary>
    public UnlockGitWorktreeCmdlet()
        : this(ServiceFactory.CreateGitWorktreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnlockGitWorktreeCmdlet"/> class.
    /// </summary>
    /// <param name="worktreeService">The worktree service used by the cmdlet.</param>
    internal UnlockGitWorktreeCmdlet(IGitWorktreeService worktreeService)
    {
        this.worktreeService = worktreeService ?? throw new ArgumentNullException(nameof(worktreeService));
    }

    private readonly IGitWorktreeService worktreeService;

    /// <summary>
    /// Gets or sets the name of the worktree to unlock.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "Unlock")]
    [ValidateNotNullOrEmpty]
    [GitWorktreeCompleter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a pre-built options object for full control over the unlock operation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitWorktreeUnlockOptions Options { get; set; } = null!;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        if (!ShouldProcess(options.RepositoryPath, $"Unlock worktree '{options.Name}'"))
        {
            return;
        }

        try
        {
            worktreeService.UnlockWorktree(options);
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "UnlockGitWorktreeFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    /// <summary>
    /// Builds a <see cref="GitWorktreeUnlockOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitWorktreeUnlockOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == "Options")
        {
            return Options;
        }

        return new GitWorktreeUnlockOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
        };
    }
}
