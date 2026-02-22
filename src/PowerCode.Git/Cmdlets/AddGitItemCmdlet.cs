using System;
using System.Collections.Generic;
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
/// <example>
/// <code>Get-GitStatus | Select-Object -ExpandProperty Entries | Where-Object Status -EQ Modified | Add-GitItem</code>
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

    // Paths accumulated from InputObject pipeline calls, dispatched in EndProcessing.
    private readonly List<string> inputObjectPaths = [];

    /// <summary>
    /// Gets or sets the paths to stage. Mutually exclusive with <see cref="All"/> and <see cref="Update"/>.
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Path")]
    [ValidateNotNullOrEmpty]
    [Alias("FilePath")]
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
    /// Gets or sets one or more diff hunks to stage. Accepts pipeline input
    /// from <c>Get-GitDiff -Hunk</c>.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Hunk")]
    public GitDiffHunk[]? Hunk { get; set; }

    /// <summary>
    /// Gets or sets an object whose path is resolved by inspecting its
    /// <c>FilePath</c>, <c>NewPath</c>, or <c>Path</c> property (in that
    /// order). Use this parameter explicitly (e.g. <c>-InputObject $entry</c>)
    /// when piping objects whose type is not covered by the <c>Hunk</c> or
    /// <c>Path</c> parameter sets. For pipeline scenarios, prefer relying on
    /// <c>ValueFromPipelineByPropertyName</c> via the <c>FilePath</c> alias on
    /// the <c>Path</c> parameter.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "InputObject")]
    public PSObject? InputObject { get; set; }

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
        // InputObject set — accumulate resolved paths; the stage call is deferred to
        // EndProcessing so all pipeline objects are batched into a single git invocation.
        if (ParameterSetName == "InputObject" && InputObject is not null)
        {
            // If the wrapped object is a GitDiffHunk, delegate to hunk staging directly.
            if (InputObject.BaseObject is GitDiffHunk hunkObj)
            {
                var hunkRepoPath = ResolveRepositoryPath();

                if (!ShouldProcess(hunkRepoPath, "Stage 1 hunk(s)"))
                {
                    return;
                }

                try
                {
                    workingTreeService.StageHunks(new GitStageHunkOptions
                    {
                        RepositoryPath = hunkRepoPath,
                        Hunks = [hunkObj],
                    });
                }
                catch (Exception exception)
                {
                    WriteError(new ErrorRecord(exception, "AddGitItemFailed", ErrorCategory.InvalidOperation, hunkRepoPath));
                }

                return;
            }

            var resolvedPath = ResolveInputObjectPath(InputObject);

            if (resolvedPath is not null)
            {
                inputObjectPaths.Add(resolvedPath);
            }
            else
            {
                WriteWarning(
                    $"InputObject of type '{InputObject.BaseObject?.GetType().Name}' does not expose a FilePath, NewPath, or Path property and will be skipped.");
            }

            return;
        }

        if (ParameterSetName == "Hunk" && Hunk is { Length: > 0 })
        {
            var repositoryPath = ResolveRepositoryPath();

            if (!ShouldProcess(repositoryPath, $"Stage {Hunk.Length} hunk(s)"))
            {
                return;
            }

            try
            {
                var hunkOptions = new GitStageHunkOptions
                {
                    RepositoryPath = repositoryPath,
                    Hunks = Hunk,
                };

                workingTreeService.StageHunks(hunkOptions);
            }
            catch (Exception exception)
            {
                WriteError(new ErrorRecord(exception, "AddGitItemHunkFailed", ErrorCategory.InvalidOperation, repositoryPath));
            }

            return;
        }

        var repoPath = ResolveRepositoryPath();

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

        if (!ShouldProcess(repoPath, description))
        {
            return;
        }

        try
        {
            var options = new GitStageOptions
            {
                RepositoryPath = repoPath,
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
                repoPath);

            WriteError(errorRecord);
        }
    }

    /// <summary>
    /// Dispatches accumulated InputObject paths (if any) as a single stage operation.
    /// </summary>
    protected override void EndProcessing()
    {
        if (inputObjectPaths.Count == 0)
        {
            return;
        }

        var repositoryPath = ResolveRepositoryPath();

        if (!ShouldProcess(repositoryPath, $"Stage {inputObjectPaths.Count} file(s)"))
        {
            return;
        }

        try
        {
            workingTreeService.Stage(new GitStageOptions
            {
                RepositoryPath = repositoryPath,
                Paths = inputObjectPaths,
            });
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "AddGitItemFailed",
                ErrorCategory.InvalidOperation,
                repositoryPath));
        }
    }

    /// <summary>
    /// Attempts to extract a file path from a <see cref="PSObject"/> by inspecting
    /// its properties in priority order: <c>FilePath</c>, <c>NewPath</c>, <c>Path</c>.
    /// </summary>
    /// <param name="obj">The object to inspect.</param>
    /// <returns>
    /// The resolved path string, or <see langword="null"/> when no compatible
    /// property is found.
    /// </returns>
    private static string? ResolveInputObjectPath(PSObject obj)
    {
        foreach (var propertyName in (string[])["FilePath", "NewPath", "Path"])
        {
            var property = obj.Properties[propertyName];

            if (property?.Value is string value && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
