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
/// <example>
/// <code>Add-GitItem -Update</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Add, "GitItem", SupportsShouldProcess = true, DefaultParameterSetName = "Path")]
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
    /// Gets or sets the paths to stage. Mutually exclusive with <see cref="All"/> and <see cref="Update"/>.
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "Path")]
    [ValidateNotNullOrEmpty]
    [GitPathCompleter]
    public string[]? Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to stage all changes.
    /// Mutually exclusive with <see cref="Path"/> and <see cref="Update"/>.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "All")]
    public SwitchParameter All { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to stage only already-tracked files (git add -u).
    /// Mutually exclusive with <see cref="Path"/> and <see cref="All"/>.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Update")]
    public SwitchParameter Update { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow staging ignored files (git add -f).
    /// Can be combined with <see cref="Path"/>, <see cref="All"/>, or <see cref="Update"/>.
    /// </summary>
    [Parameter(ParameterSetName = "Path")]
    [Parameter(ParameterSetName = "All")]
    [Parameter(ParameterSetName = "Update")]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitStageOptions"/> instance.
    /// When specified, all other parameters are ignored.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitStageOptions? Options { get; set; }

    /// <summary>
    /// Builds a <see cref="GitStageOptions"/> from the current parameter set.
    /// </summary>
    /// <param name="currentFileSystemPath">The current working directory to use when resolving the repository path.</param>
    /// <returns>A fully populated <see cref="GitStageOptions"/> instance.</returns>
    internal GitStageOptions BuildOptions(string currentFileSystemPath)
    {
        if (Options is not null)
        {
            return Options;
        }

        var repositoryPath = ResolveRepositoryPath(currentFileSystemPath);

        return new GitStageOptions
        {
            RepositoryPath = repositoryPath,
            Paths = Path,
            All = All.IsPresent,
            Update = Update.IsPresent,
            Force = Force.IsPresent,
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath();

        if (ParameterSetName == "Options" && Options is not null)
        {
            if (!ShouldProcess(Options.RepositoryPath, "Stage files"))
            {
                return;
            }

            try
            {
                workingTreeService.Stage(Options);
            }
            catch (Exception exception)
            {
                WriteError(new ErrorRecord(exception, "AddGitItemFailed", ErrorCategory.InvalidOperation, Options.RepositoryPath));
            }

            return;
        }

        var description = All.IsPresent ? "Stage all changes"
            : Update.IsPresent ? "Stage tracked file changes"
            : $"Stage {Path?.Length ?? 0} file(s)";

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
                Update = Update.IsPresent,
                Force = Force.IsPresent,
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
