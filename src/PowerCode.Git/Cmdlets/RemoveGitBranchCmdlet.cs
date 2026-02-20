using System;
using System.Management.Automation;
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
[Cmdlet(VerbsCommon.Remove, "GitBranch", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
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
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    [GitBranchCompleter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to force-delete the branch
    /// even if it is not fully merged.
    /// </summary>
    [Parameter]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath();

        if (!ShouldProcess(repositoryPath, $"Delete branch '{Name}'"))
        {
            return;
        }

        try
        {
            branchService.DeleteBranch(repositoryPath, Name, Force.IsPresent);
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "RemoveGitBranchFailed",
                ErrorCategory.InvalidOperation,
                repositoryPath);

            WriteError(errorRecord);
        }
    }
}
