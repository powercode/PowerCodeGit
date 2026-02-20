using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Switches the current branch of a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Switch, "GitBranch", SupportsShouldProcess = true)]
[OutputType(typeof(GitBranchInfo))]
public sealed class SwitchGitBranchCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchGitBranchCmdlet"/> class.
    /// </summary>
    public SwitchGitBranchCmdlet()
        : this(ServiceFactory.CreateGitBranchService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchGitBranchCmdlet"/> class.
    /// </summary>
    /// <param name="branchService">The branch service used by the cmdlet.</param>
    internal SwitchGitBranchCmdlet(IGitBranchService branchService)
    {
        this.branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
    }

    private readonly IGitBranchService branchService;

    /// <summary>
    /// Gets or sets the name of the branch to switch to.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    [GitBranchCompleter(IncludeRemote = true)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath();

        if (!ShouldProcess(repositoryPath, $"Switch to branch '{Name}'"))
        {
            return;
        }

        try
        {
            var result = branchService.SwitchBranch(repositoryPath, Name);
            WriteObject(result);
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "SwitchGitBranchFailed",
                ErrorCategory.InvalidOperation,
                repositoryPath);

            WriteError(errorRecord);
        }
    }
}
