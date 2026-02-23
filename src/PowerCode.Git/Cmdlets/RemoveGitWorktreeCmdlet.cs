using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Removes a worktree from a git repository.
/// <example>
/// <code>Remove-GitWorktree -Name feature</code>
/// </example>
/// <example>
/// <code>Remove-GitWorktree -Name feature -Force</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Remove, "GitWorktree", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = RemoveParameterSet)]
public sealed class RemoveGitWorktreeCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveGitWorktreeCmdlet"/> class.
    /// </summary>
    public RemoveGitWorktreeCmdlet()
        : this(ServiceFactory.CreateGitWorktreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveGitWorktreeCmdlet"/> class.
    /// </summary>
    /// <param name="worktreeService">The worktree service used by the cmdlet.</param>
    internal RemoveGitWorktreeCmdlet(IGitWorktreeService worktreeService)
    {
        this.worktreeService = worktreeService ?? throw new ArgumentNullException(nameof(worktreeService));
    }

    private readonly IGitWorktreeService worktreeService;

    private const string RemoveParameterSet = "Remove";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the name of the worktree to remove.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = RemoveParameterSet)]
    [ValidateNotNullOrEmpty]
    [GitWorktreeCompleter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to force removal of a locked worktree.
    /// </summary>
    [Parameter(ParameterSetName = RemoveParameterSet)]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object for full control over worktree removal.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitWorktreeRemoveOptions Options { get; set; } = null!;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        if (!ShouldProcess(options.RepositoryPath, $"Remove worktree '{options.Name}'"))
        {
            return;
        }

        try
        {
            worktreeService.RemoveWorktree(options);
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "RemoveGitWorktreeFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    /// <summary>
    /// Builds a <see cref="GitWorktreeRemoveOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitWorktreeRemoveOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitWorktreeRemoveOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Force = Force.IsPresent,
        };
    }
}
