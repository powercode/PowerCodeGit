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
[Cmdlet(VerbsData.Restore, "GitItem", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = PathParameterSet)]
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

    private const string PathParameterSet = "Path";
    private const string AllParameterSet = "All";
    private const string HunkParameterSet = "Hunk";
    private const string InputObjectParameterSet = "InputObject";
    private const string OptionsParameterSet = "Options";

    // Paths accumulated from InputObject pipeline calls, dispatched in EndProcessing.
    private readonly List<string> inputObjectPaths = [];

    // -- Path parameter set --------------------------------------------------

    /// <summary>
    /// Gets or sets one or more repository-relative file paths to restore.
    /// Mutually exclusive with <see cref="All"/> and <see cref="Hunk"/>.
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = PathParameterSet)]
    [ValidateNotNullOrEmpty]
    [Alias("FilePath")]
    [GitModifiedPathCompleter(StagedParameterName = nameof(Staged))]
    public string[]? Path { get; set; }

    // -- All parameter set ---------------------------------------------------

    /// <summary>
    /// Gets or sets a value indicating whether to restore all files in the
    /// repository. Mutually exclusive with <see cref="Path"/> and <see cref="Hunk"/>.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = AllParameterSet)]
    public SwitchParameter All { get; set; }

    // -- Hunk parameter set --------------------------------------------------

    /// <summary>
    /// Gets or sets one or more diff hunks to revert. Accepts pipeline input
    /// from <c>Get-GitDiff -Hunk</c>. Applies an inverted patch via
    /// <c>git apply -R</c>. Mutually exclusive with <see cref="Path"/> and
    /// <see cref="All"/>.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = HunkParameterSet)]
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
    [Parameter(Mandatory = true, ParameterSetName = InputObjectParameterSet)]
    public PSObject? InputObject { get; set; }

    // -- Options parameter set -----------------------------------------------

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitRestoreOptions"/> instance.
    /// When specified, all other parameters are ignored.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitRestoreOptions? Options { get; set; }

    // -- Shared parameters ---------------------------------------------------

    /// <summary>
    /// Gets or sets a value indicating whether to restore the index (staged
    /// changes) rather than the working tree. Equivalent to
    /// <c>git restore --staged</c>. When not specified, the working-tree file
    /// is restored.
    /// </summary>
    [Parameter(ParameterSetName = PathParameterSet)]
    [Parameter(ParameterSetName = AllParameterSet)]
    [Parameter(ParameterSetName = HunkParameterSet)]
    [Parameter(ParameterSetName = InputObjectParameterSet)]
    public SwitchParameter Staged { get; set; }

    /// <summary>
    /// Gets or sets the tree to restore content from. When omitted, defaults
    /// to HEAD (working-tree restore) or the index (<c>--staged</c> restore).
    /// Equivalent to <c>git restore --source=&lt;tree&gt;</c>.
    /// </summary>
    [Parameter(ParameterSetName = PathParameterSet)]
    [Parameter(ParameterSetName = AllParameterSet)]
    [Parameter(ParameterSetName = InputObjectParameterSet)]
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

        return BuildRestoreOptions(ResolveRepositoryPath(currentFileSystemPath));
    }

    /// <summary>
    /// Executes the cmdlet operation by dispatching to the handler for the
    /// active parameter set.
    /// </summary>
    protected override void ProcessRecord()
    {
        switch (ParameterSetName)
        {
            case HunkParameterSet when Hunk is { Length: > 0 }:
                ProcessHunks(Hunk);
                break;

            case InputObjectParameterSet when InputObject is not null:
                AccumulateInputObject(InputObject);
                break;

            case OptionsParameterSet when Options is not null:
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

        ExecuteRestore(BuildOptionsForPaths(inputObjectPaths, ResolveRepositoryPath()));
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
            ProcessHunk(hunk, repositoryPath);
        }
    }

    /// <summary>
    /// Confirms and applies a single hunk restore operation.
    /// </summary>
    private void ProcessHunk(GitDiffHunk hunk, string repositoryPath)
    {
        if (ConfirmHunkRestore(hunk, repositoryPath))
        {
            ExecuteHunkRestore(hunk, repositoryPath);
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when the user confirms the hunk restore
    /// via <c>ShouldProcess</c>.
    /// </summary>
    private bool ConfirmHunkRestore(GitDiffHunk hunk, string repositoryPath)
        => ShouldProcess(repositoryPath, GitDiffHunkFormatter.FormatDescription("Restore", hunk));

    /// <summary>
    /// Calls the service to restore a single hunk and emits a non-terminating
    /// error on failure.
    /// </summary>
    private void ExecuteHunkRestore(GitDiffHunk hunk, string repositoryPath)
    {
        try
        {
            workingTreeService.RestoreHunks(new GitRestoreHunkOptions
            {
                RepositoryPath = repositoryPath,
                Hunks = [hunk],
                Staged = Staged.IsPresent,
            });
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(exception, "RestoreGitItemFailed", ErrorCategory.InvalidOperation, repositoryPath));
        }
    }

    /// <summary>
    /// Accumulates a path from an <see cref="InputObject"/> pipeline object.
    /// The actual restore call is deferred to <see cref="EndProcessing"/>.
    /// </summary>
    private void AccumulateInputObject(PSObject inputObject)
    {
        var path = GitPSObjectHelper.ResolveInputObjectPath(inputObject);

        if (path is not null)
        {
            inputObjectPaths.Add(path);
        }
        else
        {
            WarnUnresolvableInputObject(inputObject);
        }
    }

    /// <summary>
    /// Emits a warning when an <see cref="InputObject"/> does not expose a
    /// recognisable path property.
    /// </summary>
    private void WarnUnresolvableInputObject(PSObject inputObject)
        => WriteWarning(
            $"InputObject of type '{inputObject.BaseObject?.GetType().Name}' does not expose a FilePath, NewPath, or Path property and will be skipped.");

    /// <summary>
    /// Confirms and performs a restore operation using a pre-built
    /// <see cref="GitRestoreOptions"/>. Used by the <c>Options</c> and
    /// <c>InputObject</c> parameter sets where the description is generic.
    /// Gates on <see cref="Cmdlet.ShouldProcess(string, string)"/> and emits a
    /// non-terminating error on failure.
    /// </summary>
    private void ExecuteRestore(GitRestoreOptions options)
    {
        var target = options.Paths is { Count: > 0 }
            ? string.Join(", ", options.Paths)
            : options.RepositoryPath;
        var action = options.Staged ? "Restore (unstage)" : "Restore working-tree";
        if (!ShouldProcess(target, action))
        {
            return;
        }

        PerformRestore(options);
    }

    /// <summary>
    /// Handles the <c>Path</c> and <c>All</c> parameter sets by building options
    /// from the current parameters and executing the restore with a descriptive
    /// <c>ShouldProcess</c> prompt.
    /// </summary>
    private void ExecutePathOrAllRestore()
    {
        var repoPath = ResolveRepositoryPath();
        var target = All.IsPresent ? repoPath : string.Join(", ", Path ?? []);
        var action = Staged.IsPresent ? "Restore (unstage)" : "Restore working-tree";
        if (All.IsPresent)
        {
            action += " all files";
        }

        if (!ShouldProcess(target, action))
        {
            return;
        }

        PerformRestore(BuildRestoreOptions(repoPath));
    }

    /// <summary>
    /// Calls the working-tree service to restore files and emits a
    /// non-terminating error on failure. Does not gate on
    /// <see cref="Cmdlet.ShouldProcess(string, string)"/>; callers are
    /// responsible for confirmation before invoking.
    /// </summary>
    private void PerformRestore(GitRestoreOptions options)
    {
        try
        {
            workingTreeService.Restore(options);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(exception, "RestoreGitItemFailed", ErrorCategory.InvalidOperation, options.RepositoryPath));
        }
    }



    /// <summary>
    /// Maps the current cmdlet parameters to a <see cref="GitRestoreOptions"/> instance.
    /// </summary>
    /// <param name="repositoryPath">The resolved repository root path.</param>
    private GitRestoreOptions BuildRestoreOptions(string repositoryPath)
        => new()
        {
            RepositoryPath = repositoryPath,
            Paths = Path,
            All = All.IsPresent,
            Staged = Staged.IsPresent,
            Source = Source,
        };

    /// <summary>
    /// Builds a <see cref="GitRestoreOptions"/> for a fixed set of accumulated
    /// paths, as gathered from <c>InputObject</c> pipeline input.
    /// </summary>
    /// <param name="paths">The file paths to restore.</param>
    /// <param name="repositoryPath">The resolved repository root path.</param>
    private GitRestoreOptions BuildOptionsForPaths(IReadOnlyList<string> paths, string repositoryPath)
        => new()
        {
            RepositoryPath = repositoryPath,
            Paths = paths,
            Staged = Staged.IsPresent,
            Source = Source,
        };

}

