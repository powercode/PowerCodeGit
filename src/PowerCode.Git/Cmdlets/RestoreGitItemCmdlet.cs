using System;
using System.Collections.Generic;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;
using PowerCode.Git.Formatting;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Discards working-tree changes or unstages index changes for files in a git
/// repository (git restore).
/// <example>
/// <code>Restore-GitItem -Path ./file.txt</code>
/// </example>
/// <example>
/// <code>Restore-GitItem -All</code>
/// </example>
/// <example>
/// <code>Restore-GitItem -Path ./file.txt -Staged</code>
/// </example>
/// <example>
/// <code>Get-GitDiff -Hunk | Restore-GitItem</code>
/// </example>
/// <example>
/// <code>Get-GitStatus | Select-Object -ExpandProperty Entries | Restore-GitItem</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.Restore, "GitItem", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High, DefaultParameterSetName = "Path")]
public sealed class RestoreGitItemCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreGitItemCmdlet"/> class.
    /// </summary>
    public RestoreGitItemCmdlet()
        : this(ServiceFactory.CreateGitWorkingTreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreGitItemCmdlet"/> class.
    /// </summary>
    /// <param name="workingTreeService">The working tree service used by the cmdlet.</param>
    internal RestoreGitItemCmdlet(IGitWorkingTreeService workingTreeService)
    {
        this.workingTreeService = workingTreeService ?? throw new ArgumentNullException(nameof(workingTreeService));
    }

    private readonly IGitWorkingTreeService workingTreeService;

    // Paths accumulated from InputObject pipeline calls, dispatched in EndProcessing.
    private readonly List<string> inputObjectPaths = [];

    // -- Path parameter set --------------------------------------------------

    /// <summary>
    /// Gets or sets one or more repository-relative file paths to restore.
    /// Mutually exclusive with <see cref="All"/> and <see cref="Hunk"/>.
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Path")]
    [ValidateNotNullOrEmpty]
    [Alias("FilePath")]
    [GitPathCompleter]
    public string[]? Path { get; set; }

    // -- All parameter set ---------------------------------------------------

    /// <summary>
    /// Gets or sets a value indicating whether to restore all files in the
    /// repository. Mutually exclusive with <see cref="Path"/> and <see cref="Hunk"/>.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "All")]
    public SwitchParameter All { get; set; }

    // -- Hunk parameter set --------------------------------------------------

    /// <summary>
    /// Gets or sets one or more diff hunks to revert. Accepts pipeline input
    /// from <c>Get-GitDiff -Hunk</c>. Applies an inverted patch via
    /// <c>git apply -R</c>. Mutually exclusive with <see cref="Path"/> and
    /// <see cref="All"/>.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Hunk")]
    public GitDiffHunk[]? Hunk { get; set; }

    // -- InputObject parameter set -------------------------------------------

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

    // -- Options parameter set -----------------------------------------------

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitRestoreOptions"/> instance.
    /// When specified, all other parameters are ignored.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitRestoreOptions? Options { get; set; }

    // -- Shared parameters ---------------------------------------------------

    /// <summary>
    /// Gets or sets a value indicating whether to restore the index (staged
    /// changes) rather than the working tree. Equivalent to
    /// <c>git restore --staged</c>. When not specified, the working-tree file
    /// is restored.
    /// </summary>
    [Parameter(ParameterSetName = "Path")]
    [Parameter(ParameterSetName = "All")]
    [Parameter(ParameterSetName = "Hunk")]
    [Parameter(ParameterSetName = "InputObject")]
    public SwitchParameter Staged { get; set; }

    /// <summary>
    /// Gets or sets the tree to restore content from. When omitted, defaults
    /// to HEAD (working-tree restore) or the index (<c>--staged</c> restore).
    /// Equivalent to <c>git restore --source=&lt;tree&gt;</c>.
    /// </summary>
    [Parameter(ParameterSetName = "Path")]
    [Parameter(ParameterSetName = "All")]
    [Parameter(ParameterSetName = "InputObject")]
    [GitCommittishCompleter]
    public string? Source { get; set; }

    // ------------------------------------------------------------------------

    /// <summary>
    /// Builds a <see cref="GitRestoreOptions"/> from the current parameter set.
    /// Not applicable for the <c>Hunk</c> and <c>InputObject</c> parameter sets.
    /// </summary>
    /// <param name="currentFileSystemPath">The current working directory used when resolving the repository path.</param>
    /// <returns>A populated restore options object.</returns>
    internal GitRestoreOptions BuildOptions(string currentFileSystemPath)
    {
        if (Options is not null)
        {
            return Options;
        }

        var repositoryPath = ResolveRepositoryPath(currentFileSystemPath);

        return new GitRestoreOptions
        {
            RepositoryPath = repositoryPath,
            Paths = Path,
            All = All.IsPresent,
            Staged = Staged.IsPresent,
            Source = Source,
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
            case "Hunk" when Hunk is { Length: > 0 }:
                ProcessHunks(Hunk);
                break;

            case "InputObject" when InputObject is not null:
                AccumulateInputObject(InputObject);
                break;

            case "Options" when Options is not null:
                ExecuteRestore(Options);
                break;

            default:
                ExecutePathOrAllRestore();
                break;
        }
    }

    /// <summary>
    /// Dispatches accumulated InputObject paths (if any) as a single restore operation.
    /// </summary>
    protected override void EndProcessing()
    {
        if (inputObjectPaths.Count == 0)
        {
            return;
        }

        var options = new GitRestoreOptions
        {
            RepositoryPath = ResolveRepositoryPath(),
            Paths = inputObjectPaths,
            Staged = Staged.IsPresent,
            Source = Source,
        };

        ExecuteRestore(options);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Confirms and applies each hunk individually so the user can accept or
    /// reject every change when <c>-Confirm</c> or <c>-WhatIf</c> is active.
    /// </summary>
    private void ProcessHunks(GitDiffHunk[] hunks)
    {
        var repositoryPath = ResolveRepositoryPath();

        foreach (var hunk in hunks)
        {
            if (!ShouldProcess(repositoryPath, GitDiffHunkFormatter.FormatDescription("Restore", hunk)))
            {
                continue;
            }

            try
            {
                workingTreeService.RestoreHunks(new GitRestoreHunkOptions
                {
                    RepositoryPath = repositoryPath,
                    Hunks = [hunk],
                    Staged = Staged.IsPresent,
                });
            }
            catch (Exception exception)
            {
                WriteError(new ErrorRecord(exception, "RestoreGitItemFailed", ErrorCategory.InvalidOperation, repositoryPath));
            }
        }
    }

    /// <summary>
    /// Accumulates a path from an <see cref="InputObject"/> pipeline object.
    /// The actual restore call is deferred to <see cref="EndProcessing"/>.
    /// </summary>
    private void AccumulateInputObject(PSObject inputObject)
    {
        var path = ResolveInputObjectPath(inputObject);

        if (path is not null)
        {
            inputObjectPaths.Add(path);
        }
        else
        {
            WriteWarning(
                $"InputObject of type '{inputObject.BaseObject?.GetType().Name}' does not expose a FilePath, NewPath, or Path property and will be skipped.");
        }
    }

    /// <summary>
    /// Performs a restore operation using a pre-built <see cref="GitRestoreOptions"/>.
    /// Gates on <see cref="Cmdlet.ShouldProcess(string, string)"/> and emits a
    /// non-terminating error on failure.
    /// </summary>
    private void ExecuteRestore(GitRestoreOptions options)
    {
        if (!ShouldProcess(options.RepositoryPath, "Restore files"))
        {
            return;
        }

        try
        {
            workingTreeService.Restore(options);
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(exception, "RestoreGitItemFailed", ErrorCategory.InvalidOperation, options.RepositoryPath));
        }
    }

    /// <summary>
    /// Handles the <c>Path</c> and <c>All</c> parameter sets by building options
    /// from the current parameters and executing the restore.
    /// </summary>
    private void ExecutePathOrAllRestore()
    {
        var repoPath = ResolveRepositoryPath();
        var targetDescription = All.IsPresent ? "all files" : $"{Path?.Length ?? 0} file(s)";
        var description = Staged.IsPresent
            ? $"Restore (unstage) {targetDescription}"
            : $"Restore working-tree {targetDescription}";

        if (!ShouldProcess(repoPath, description))
        {
            return;
        }

        try
        {
            workingTreeService.Restore(new GitRestoreOptions
            {
                RepositoryPath = repoPath,
                Paths = Path,
                All = All.IsPresent,
                Staged = Staged.IsPresent,
                Source = Source,
            });
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "RestoreGitItemFailed",
                ErrorCategory.InvalidOperation,
                repoPath));
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
