using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Locks a worktree in a git repository to prevent it from being pruned.
/// <example>
/// <code>Lock-GitWorktree -Name feature</code>
/// </example>
/// <example>
/// <code>Lock-GitWorktree -Name feature -Reason "Work in progress"</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Lock, "GitWorktree", SupportsShouldProcess = true, DefaultParameterSetName = LockParameterSet)]
public sealed class LockGitWorktreeCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LockGitWorktreeCmdlet"/> class.
    /// </summary>
    public LockGitWorktreeCmdlet()
        : this(ServiceFactory.CreateGitWorktreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LockGitWorktreeCmdlet"/> class.
    /// </summary>
    /// <param name="worktreeService">The worktree service used by the cmdlet.</param>
    internal LockGitWorktreeCmdlet(IGitWorktreeService worktreeService)
    {
        this.worktreeService = worktreeService ?? throw new ArgumentNullException(nameof(worktreeService));
    }

    private readonly IGitWorktreeService worktreeService;

    private const string LockParameterSet = "Lock";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the name of the worktree to lock.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = LockParameterSet)]
    [ValidateNotNullOrEmpty]
    [GitWorktreeCompleter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for locking the worktree.
    /// </summary>
    [Parameter(Position = 1, ParameterSetName = LockParameterSet)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object for full control over the lock operation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitWorktreeLockOptions Options { get; set; } = null!;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        if (!ShouldProcess(options.RepositoryPath, $"Lock worktree '{options.Name}'"))
        {
            return;
        }

        try
        {
            worktreeService.LockWorktree(options);
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "LockGitWorktreeFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    /// <summary>
    /// Builds a <see cref="GitWorktreeLockOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitWorktreeLockOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitWorktreeLockOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Reason = Reason,
        };
    }
}
