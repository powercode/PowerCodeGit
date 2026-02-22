using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
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
    /// Executes the cmdlet operation by dispatching to the handler for the
    /// active parameter set.
    /// </summary>
    protected override void ProcessRecord()
    {
        switch (ParameterSetName)
        {
            case "InputObject" when InputObject is not null:
                AccumulateInputObject(InputObject);
                break;

            case "Hunk" when Hunk is { Length: > 0 }:
                ProcessHunks(Hunk);
                break;

            case "Options" when Options is not null:
                ExecuteStage(Options);
                break;

            default:
                ExecutePathOrAllStage();
                break;
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

        ExecuteStage(new GitStageOptions
        {
            RepositoryPath = ResolveRepositoryPath(),
            Paths = inputObjectPaths,
        });
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Accumulates a path from an <see cref="InputObject"/> pipeline object.
    /// If the wrapped object is a <see cref="GitDiffHunk"/>, it is staged
    /// immediately; otherwise the resolved path is deferred to
    /// <see cref="EndProcessing"/>.
    /// </summary>
    private void AccumulateInputObject(PSObject inputObject)
    {
        if (inputObject.BaseObject is GitDiffHunk hunkObj)
        {
            ProcessHunks([hunkObj]);
            return;
        }

        var resolvedPath = ResolveInputObjectPath(inputObject);

        if (resolvedPath is not null)
        {
            inputObjectPaths.Add(resolvedPath);
        }
        else
        {
            WriteWarning(
                $"InputObject of type '{inputObject.BaseObject?.GetType().Name}' does not expose a FilePath, NewPath, or Path property and will be skipped.");
        }
    }

    /// <summary>
    /// Confirms and applies each hunk individually so the user can accept or
    /// reject every change when <c>-Confirm</c> or <c>-WhatIf</c> is active.
    /// </summary>
    private void ProcessHunks(GitDiffHunk[] hunks)
    {
        var repositoryPath = ResolveRepositoryPath();

        foreach (var hunk in hunks)
        {
            if (!ShouldProcess(repositoryPath, FormatHunkDescription("Stage", hunk)))
            {
                continue;
            }

            try
            {
                workingTreeService.StageHunks(new GitStageHunkOptions
                {
                    RepositoryPath = repositoryPath,
                    Hunks = [hunk],
                });
            }
            catch (Exception exception)
            {
                WriteError(new ErrorRecord(exception, "AddGitItemHunkFailed", ErrorCategory.InvalidOperation, repositoryPath));
            }
        }
    }

    /// <summary>
    /// Performs a stage operation using a pre-built <see cref="GitStageOptions"/>.
    /// Gates on <see cref="Cmdlet.ShouldProcess(string, string)"/> and emits a
    /// non-terminating error on failure.
    /// </summary>
    private void ExecuteStage(GitStageOptions options)
    {
        if (!ShouldProcess(options.RepositoryPath, "Stage files"))
        {
            return;
        }

        try
        {
            workingTreeService.Stage(options);
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(exception, "AddGitItemFailed", ErrorCategory.InvalidOperation, options.RepositoryPath));
        }
    }

    /// <summary>
    /// Handles the <c>Path</c>, <c>All</c>, and <c>Update</c> parameter sets
    /// by building options from the current parameters and executing the stage.
    /// </summary>
    private void ExecutePathOrAllStage()
    {
        var repoPath = ResolveRepositoryPath();

        var description = All.IsPresent ? "Stage all changes"
            : Update.IsPresent ? "Stage tracked file changes"
            : $"Stage {Path?.Length ?? 0} file(s)";

        if (!ShouldProcess(repoPath, description))
        {
            return;
        }

        try
        {
            workingTreeService.Stage(new GitStageOptions
            {
                RepositoryPath = repoPath,
                Paths = Path,
                All = All.IsPresent,
                Update = Update.IsPresent,
                Force = Force.IsPresent,
            });
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "AddGitItemFailed",
                ErrorCategory.InvalidOperation,
                repoPath));
        }
    }

    /// <summary>
    /// Formats a description for <see cref="Cmdlet.ShouldProcess(string, string)"/> that includes a
    /// content preview of the specified hunk so the user can make an informed
    /// decision when <c>-Confirm</c> or <c>-WhatIf</c> is in effect.
    /// </summary>
    /// <param name="verb">The action verb (e.g. "Stage").</param>
    /// <param name="hunk">The hunk to describe.</param>
    /// <param name="maxPreviewLines">Maximum number of diff lines to show.</param>
    /// <returns>A multi-line description string.</returns>
    private static string FormatHunkDescription(string verb, GitDiffHunk hunk, int maxPreviewLines = 5)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{verb} hunk in {hunk.FilePath} {hunk.Header}");

        var contentLines = hunk.Content.Split('\n');
        var shown = 0;

        // Skip index 0 — the @@ header line.
        for (var i = 1; i < contentLines.Length && shown < maxPreviewLines; i++)
        {
            var line = contentLines[i].TrimEnd('\r');

            if (line.Length == 0)
            {
                continue;
            }

            var display = line.Length > 80 ? line[..80] + "\u2026" : line;
            sb.AppendLine($"  {display}");
            shown++;
        }

        if (shown >= maxPreviewLines)
        {
            sb.AppendLine("  \u2026");
        }

        return sb.ToString().TrimEnd();
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
