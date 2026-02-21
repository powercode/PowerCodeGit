using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Deletes a git branch (git branch -d / -D).
/// <example>
/// <code>Remove-GitBranch -Name feature/old-feature</code>
/// </example>
/// <example>
/// <code>Remove-GitBranch -Name feature/old-feature -Force</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Remove, "GitBranch", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = "Delete")]
public sealed class RemoveGitBranchCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveGitBranchCmdlet"/> class.
    /// </summary>
    public RemoveGitBranchCmdlet()
        : this(ServiceFactory.CreateGitBranchService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveGitBranchCmdlet"/> class.
    /// </summary>
    /// <param name="branchService">The branch service used by the cmdlet.</param>
    internal RemoveGitBranchCmdlet(IGitBranchService branchService)
    {
        this.branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
    }

    private readonly IGitBranchService branchService;

    /// <summary>
    /// Gets or sets the name of the branch to delete.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "Delete")]
    [ValidateNotNullOrEmpty]
    [GitBranchCompleter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to force-delete the branch
    /// even if it is not fully merged. Equivalent to <c>git branch -D</c>.
    /// </summary>
    [Parameter(ParameterSetName = "Delete")]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object for full control over branch deletion.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitBranchDeleteOptions Options { get; set; } = null!;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        if (!ShouldProcess(options.RepositoryPath, $"Delete branch '{options.Name}'"))
        {
            return;
        }

        try
        {
            branchService.DeleteBranch(options);
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "RemoveGitBranchFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    /// <summary>
    /// Builds a <see cref="GitBranchDeleteOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitBranchDeleteOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == "Options")
        {
            return Options;
        }

        return new GitBranchDeleteOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Force = Force.IsPresent,
        };
    }
}
