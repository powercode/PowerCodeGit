using System;
using System.Management.Automation;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Cmdlets;

/// <summary>
/// Lists branches in a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitBranch")]
[OutputType(typeof(GitBranchInfo))]
public sealed class GetGitBranchCmdlet : PSCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitBranchCmdlet"/> class.
    /// </summary>
    public GetGitBranchCmdlet()
        : this(ServiceFactory.CreateGitBranchService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitBranchCmdlet"/> class.
    /// </summary>
    /// <param name="branchService">The branch service used by the cmdlet.</param>
    internal GetGitBranchCmdlet(IGitBranchService branchService)
    {
        this.branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
    }

    private readonly IGitBranchService branchService;

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
            var branches = branchService.GetBranches(repositoryPath);

            foreach (var branch in branches)
            {
                WriteObject(branch);
            }
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "GetGitBranchFailed",
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
