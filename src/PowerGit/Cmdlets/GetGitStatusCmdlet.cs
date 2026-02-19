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
public sealed class GetGitStatusCmdlet : PSCmdlet
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
    /// Gets or sets the repository path. Defaults to the current location.
    /// </summary>
    [Parameter]
    public string? Path { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolvePath();

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

    /// <summary>
    /// Resolves the repository path from the <see cref="Path"/> parameter or the current location.
    /// </summary>
    /// <param name="currentFileSystemPath">The current PowerShell file system path.</param>
    /// <returns>The resolved repository path.</returns>
    internal string ResolvePath(string? currentFileSystemPath = null)
    {
        if (!string.IsNullOrWhiteSpace(Path))
        {
            return Path!;
        }

        return currentFileSystemPath ?? SessionState.Path.CurrentFileSystemLocation.Path;
    }
}
