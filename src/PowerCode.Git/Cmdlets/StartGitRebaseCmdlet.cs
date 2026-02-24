using System;
using System.Collections.Generic;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Starts a rebase operation, replaying commits from the current branch on top
/// of the specified upstream branch (<c>git rebase &lt;upstream&gt;</c>).
/// Accepts pipeline input from <c>Get-GitBranch</c>, <c>Get-GitLog</c>, or custom objects,
/// but stops with a terminating error if more than one object is received.
/// <example>
/// <code>Start-GitRebase -Upstream main</code>
/// </example>
/// <example>
/// <code>Get-GitBranch -Pattern main | Start-GitRebase</code>
/// </example>
/// <example>
/// <code>Get-GitLog -n 1 | Start-GitRebase</code>
/// </example>
/// </summary>
[Cmdlet(VerbsLifecycle.Start, "GitRebase", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = RebaseParameterSet)]
[OutputType(typeof(GitRebaseResult))]
public sealed class StartGitRebaseCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartGitRebaseCmdlet"/> class.
    /// </summary>
    public StartGitRebaseCmdlet()
        : this(ServiceFactory.CreateGitRebaseService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StartGitRebaseCmdlet"/> class.
    /// </summary>
    /// <param name="rebaseService">The rebase service used by the cmdlet.</param>
    internal StartGitRebaseCmdlet(IGitRebaseService rebaseService)
    {
        this.rebaseService = rebaseService ?? throw new ArgumentNullException(nameof(rebaseService));
    }

    private readonly IGitRebaseService rebaseService;

    private const string RebaseParameterSet = "Rebase";
    private const string InteractiveParameterSet = "Interactive";
    private const string InputObjectParameterSet = "InputObject";
    private const string OptionsParameterSet = "Options";

    // Accumulates pipeline input objects so we can validate that exactly one was supplied.
    private readonly List<PSObject> pipelineInputs = [];

    // ── Rebase parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the name of the upstream branch to rebase the current branch onto.
    /// When invoking directly, this is a required parameter. When piping objects, use the
    /// <c>InputObject</c> parameter instead, which resolves the upstream from
    /// <see cref="GitBranchInfo.Name"/>, <see cref="GitCommitInfo.Sha"/>, or duck-typed properties.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = RebaseParameterSet)]
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = InteractiveParameterSet)]
    [ValidateNotNullOrEmpty]
    [GitCommittishCompleter(IncludeBranches = true, IncludeRemoteBranches = true, IncludeRelativeRefs = true)]
    public string Upstream { get; set; } = string.Empty;

    // ── Interactive parameter set ────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether to open an interactive rebase session
    /// (<c>git rebase -i</c>). When set in the <c>Interactive</c> parameter set,
    /// this is mandatory. When used with <c>InputObject</c>, it is optional.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = InteractiveParameterSet)]
    [Parameter(ParameterSetName = InputObjectParameterSet)]
    public SwitchParameter Interactive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically apply <c>fixup!</c>
    /// and <c>squash!</c> commits when creating the interactive todo list
    /// (<c>git rebase -i --autosquash</c>).
    /// </summary>
    [Parameter(ParameterSetName = InteractiveParameterSet)]
    [Parameter(ParameterSetName = InputObjectParameterSet)]
    public SwitchParameter AutoSquash { get; set; }

    /// <summary>
    /// Gets or sets a shell command to run after each rebased commit
    /// (<c>git rebase -i --exec &lt;cmd&gt;</c>).
    /// An <c>exec</c> line is inserted after every <c>pick</c> line in the todo list.
    /// </summary>
    [Parameter(ParameterSetName = InteractiveParameterSet)]
    [Parameter(ParameterSetName = InputObjectParameterSet)]
    [ValidateNotNullOrEmpty]
    public string? Exec { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to recreate merge commits rather than
    /// linearising history (<c>git rebase --rebase-merges</c>).
    /// </summary>
    [Parameter(ParameterSetName = InteractiveParameterSet)]
    [Parameter(ParameterSetName = InputObjectParameterSet)]
    public SwitchParameter RebaseMerges { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically update any branch refs
    /// that point to commits being rebased — useful for stacked branches
    /// (<c>git rebase --update-refs</c>).
    /// </summary>
    [Parameter(ParameterSetName = InteractiveParameterSet)]
    [Parameter(ParameterSetName = InputObjectParameterSet)]
    public SwitchParameter UpdateRefs { get; set; }

    // ── InputObject parameter set ────────────────────────────────────────────

    /// <summary>
    /// Gets or sets an object from which to resolve the upstream ref.
    /// Supports <see cref="GitBranchInfo"/> (uses <c>Name</c>),
    /// <see cref="GitCommitInfo"/> (uses <c>Sha</c>), <see cref="string"/>,
    /// or <see cref="PSCustomObject"/> with properties <c>Upstream</c>, <c>BranchName</c>,
    /// <c>Name</c>, or <c>Sha</c> (checked in that order).
    /// Only one object may be piped; multiple objects produce a terminating error.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = InputObjectParameterSet)]
    public PSObject? InputObject { get; set; }

    // ── Shared optional (Rebase + Interactive + InputObject) ─────────────────

    /// <summary>
    /// Gets or sets an optional target ref for a three-way rebase
    /// (<c>git rebase --onto &lt;Onto&gt; &lt;Upstream&gt;</c>).
    /// </summary>
    [Parameter(ParameterSetName = RebaseParameterSet)]
    [Parameter(ParameterSetName = InteractiveParameterSet)]
    [Parameter(ParameterSetName = InputObjectParameterSet)]
    [GitCommittishCompleter]
    public string? Onto { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically stash uncommitted
    /// changes before the rebase and restore them afterwards
    /// (<c>git rebase --autostash</c>).
    /// </summary>
    [Parameter(ParameterSetName = RebaseParameterSet)]
    [Parameter(ParameterSetName = InteractiveParameterSet)]
    [Parameter(ParameterSetName = InputObjectParameterSet)]
    public SwitchParameter AutoStash { get; set; }

    // ── Options parameter set ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a pre-built options object, allowing full control over the rebase.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitRebaseOptions Options { get; set; } = null!;

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Collects pipeline input objects for validation in <see cref="EndProcessing"/>.
    /// </summary>
    protected override void ProcessRecord()
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            // Options parameter set — execute immediately, no pipeline ambiguity.
            ExecuteRebase(Options);
            return;
        }

        if (ParameterSetName == InputObjectParameterSet && InputObject is not null)
        {
            pipelineInputs.Add(InputObject);
        }
    }

    /// <summary>
    /// Validates pipeline input (if any), resolves the upstream ref from
    /// <see cref="InputObject"/>, then executes the rebase.
    /// </summary>
    protected override void EndProcessing()
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            // Already executed in ProcessRecord.
            return;
        }

        if (ParameterSetName == InputObjectParameterSet)
        {
            ValidateSinglePipelineInput();

            if (pipelineInputs.Count == 0)
            {
                // No pipeline input: parameter binder would have enforced Mandatory.
                return;
            }

            // Resolve upstream from the single pipeline object.
            Upstream = ResolveUpstreamFromInputObject(pipelineInputs[0]);
        }

        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);
        ExecuteRebase(options);
    }

    /// <summary>
    /// Builds a <see cref="GitRebaseOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitRebaseOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitRebaseOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Upstream = Upstream,
            Onto = Onto,
            Interactive = Interactive.IsPresent,
            AutoStash = AutoStash.IsPresent,
            AutoSquash = AutoSquash.IsPresent,
            Exec = Exec,
            RebaseMerges = RebaseMerges.IsPresent,
            UpdateRefs = UpdateRefs.IsPresent,
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Validates that exactly one object was received from the pipeline.
    /// Throws a terminating error if multiple objects were received.
    /// </summary>
    private void ValidateSinglePipelineInput()
    {
        if (pipelineInputs.Count > 1)
        {
            ThrowTerminatingError(new ErrorRecord(
                new InvalidOperationException(
                    $"Start-GitRebase accepts exactly one pipeline object. " +
                    $"Received {pipelineInputs.Count} objects. " +
                    $"Pipe a single object or specify -Upstream explicitly."),
                "StartGitRebase_MultiplePipelineInputs",
                ErrorCategory.InvalidArgument,
                pipelineInputs));
        }
    }

    /// <summary>
    /// Resolves the upstream ref from a <see cref="PSObject"/> by inspecting its
    /// <see cref="PSObject.BaseObject"/> type or duck-typing well-known property names.
    /// </summary>
    /// <param name="obj">The pipeline input object.</param>
    /// <returns>The resolved upstream ref string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the object type is unsupported or no suitable property is found.
    /// </exception>
    private string ResolveUpstreamFromInputObject(PSObject obj)
    {
        var resolved = GitPSObjectHelper.ResolveInputObjectUpstream(obj);

        if (resolved is null)
        {
            var displayType = obj.BaseObject?.GetType().Name ?? "PSCustomObject";
            ThrowTerminatingError(new ErrorRecord(
                new InvalidOperationException(
                    $"Cannot resolve upstream from object of type '{displayType}'. " +
                    $"Supported types: GitBranchInfo, GitCommitInfo, string. " +
                    $"For custom objects, ensure one of these properties exists: " +
                    $"Upstream, BranchName, Name, Sha."),
                "StartGitRebase_CannotResolveUpstream",
                ErrorCategory.InvalidArgument,
                obj));
        }

        return resolved;
    }

    private void ExecuteRebase(GitRebaseOptions options)
    {
        if (!ShouldProcess(options.RepositoryPath, $"Rebase current branch onto '{options.Upstream}'"))
        {
            return;
        }

        try
        {
            var result = rebaseService.Start(options);
            WriteObject(result);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "StartGitRebaseFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
