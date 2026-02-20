using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

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
    /// Gets or sets a value indicating whether files matched by <c>.gitignore</c>
    /// should be included in the status results.
    /// </summary>
    [Parameter]
    public SwitchParameter IncludeIgnored { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath();

        try
        {
            var options = new GitStatusOptions
            {
                RepositoryPath = repositoryPath,
                IncludeIgnored = IncludeIgnored.IsPresent,
            };

            var result = workingTreeService.GetStatus(options);
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
