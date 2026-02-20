using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Creates a new branch at the current HEAD and checks it out (git checkout -b).
/// <example>
/// <code>New-GitBranch -Name feature/my-feature</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.New, "GitBranch", SupportsShouldProcess = true)]
[OutputType(typeof(GitBranchInfo))]
public sealed class NewGitBranchCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewGitBranchCmdlet"/> class.
    /// </summary>
    public NewGitBranchCmdlet()
        : this(ServiceFactory.CreateGitBranchService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewGitBranchCmdlet"/> class.
    /// </summary>
    /// <param name="branchService">The branch service used by the cmdlet.</param>
    internal NewGitBranchCmdlet(IGitBranchService branchService)
    {
        this.branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
    }

    private readonly IGitBranchService branchService;

    /// <summary>
    /// Gets or sets the name of the new branch.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath();

        if (!ShouldProcess(repositoryPath, $"Create branch '{Name}'"))
        {
            return;
        }

        try
        {
            var result = branchService.CreateBranch(repositoryPath, Name);
            WriteObject(result);
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "NewGitBranchFailed",
                ErrorCategory.InvalidOperation,
                repositoryPath);

            WriteError(errorRecord);
        }
    }
}
