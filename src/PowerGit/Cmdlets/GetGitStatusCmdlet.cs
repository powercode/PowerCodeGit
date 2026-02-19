using System;
using System.Management.Automation;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Cmdlets;

/// <summary>
/// Retrieves the working tree and index status of a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitStatus")]
[OutputType(typeof(GitStatusResult))]
public sealed class GetGitStatusCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitStatusCmdlet"/> class.
    /// </summary>
    public GetGitStatusCmdlet()
        : this(ServiceFactory.CreateGitWorkingTreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitStatusCmdlet"/> class.
    /// </summary>
    /// <param name="workingTreeService">The working tree service used by the cmdlet.</param>
    internal GetGitStatusCmdlet(IGitWorkingTreeService workingTreeService)
    {
        this.workingTreeService = workingTreeService ?? throw new ArgumentNullException(nameof(workingTreeService));
    }

    private readonly IGitWorkingTreeService workingTreeService;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath();

        try
        {
            var result = workingTreeService.GetStatus(repositoryPath);
            WriteObject(result);
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "GetGitStatusFailed",
                ErrorCategory.InvalidOperation,
                repositoryPath);

            WriteError(errorRecord);
        }
    }
}
