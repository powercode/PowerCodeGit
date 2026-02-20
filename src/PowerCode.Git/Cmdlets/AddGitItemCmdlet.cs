using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Stages files in the working tree for the next commit (git add).
/// <example>
/// <code>Add-GitItem -Path ./file.txt</code>
/// </example>
/// <example>
/// <code>Add-GitItem -All</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Add, "GitItem", SupportsShouldProcess = true)]
public sealed class AddGitItemCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddGitItemCmdlet"/> class.
    /// </summary>
    public AddGitItemCmdlet()
        : this(ServiceFactory.CreateGitWorkingTreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddGitItemCmdlet"/> class.
    /// </summary>
    /// <param name="workingTreeService">The working tree service used by the cmdlet.</param>
    internal AddGitItemCmdlet(IGitWorkingTreeService workingTreeService)
    {
        this.workingTreeService = workingTreeService ?? throw new ArgumentNullException(nameof(workingTreeService));
    }

    private readonly IGitWorkingTreeService workingTreeService;

    /// <summary>
    /// Gets or sets the paths to stage. Mutually exclusive with <see cref="All"/>.
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "Path")]
    [ValidateNotNullOrEmpty]
    [GitPathCompleter]
    public string[]? Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to stage all changes.
    /// Mutually exclusive with <see cref="Path"/>.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "All")]
    public SwitchParameter All { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath();
        var description = All.IsPresent ? "Stage all changes" : $"Stage {Path?.Length ?? 0} file(s)";

        if (!ShouldProcess(repositoryPath, description))
        {
            return;
        }

        try
        {
            var options = new GitStageOptions
            {
                RepositoryPath = repositoryPath,
                Paths = Path,
                All = All.IsPresent,
            };

            workingTreeService.Stage(options);
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "AddGitItemFailed",
                ErrorCategory.InvalidOperation,
                repositoryPath);

            WriteError(errorRecord);
        }
    }
}
