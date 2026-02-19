using System;
using System.Management.Automation;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Cmdlets;

/// <summary>
/// Retrieves diff entries for working tree or staged changes in a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitDiff")]
[OutputType(typeof(GitDiffEntry))]
public sealed class GetGitDiffCmdlet : PSCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitDiffCmdlet"/> class.
    /// </summary>
    public GetGitDiffCmdlet()
        : this(ServiceFactory.CreateGitWorkingTreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitDiffCmdlet"/> class.
    /// </summary>
    /// <param name="workingTreeService">The working tree service used by the cmdlet.</param>
    internal GetGitDiffCmdlet(IGitWorkingTreeService workingTreeService)
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
    /// Gets or sets a value indicating whether to show staged (index) changes
    /// instead of unstaged (working directory) changes.
    /// </summary>
    [Parameter]
    public SwitchParameter Staged { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        try
        {
            var entries = workingTreeService.GetDiff(options);

            foreach (var entry in entries)
            {
                WriteObject(entry);
            }
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "GetGitDiffFailed",
                ErrorCategory.InvalidOperation,
                options.RepositoryPath);

            WriteError(errorRecord);
        }
    }

    /// <summary>
    /// Creates diff options from cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">The current PowerShell file system path.</param>
    /// <returns>A populated diff options object.</returns>
    internal GitDiffOptions BuildOptions(string currentFileSystemPath)
    {
        var repositoryPath = Path;

        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            repositoryPath = currentFileSystemPath;
        }

        return new GitDiffOptions
        {
            RepositoryPath = repositoryPath!,
            Staged = Staged.IsPresent,
        };
    }
}
