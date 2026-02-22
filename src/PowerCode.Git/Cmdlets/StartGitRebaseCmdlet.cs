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
/// Accepts pipeline input from <c>Get-GitBranch</c>, but stops with an error
/// if more than one branch is received.
/// <example>
/// <code>Start-GitRebase -Upstream main</code>
/// </example>
/// <example>
/// <code>Get-GitBranch -Name main | Start-GitRebase</code>
/// </example>
/// </summary>
[Cmdlet(VerbsLifecycle.Start, "GitRebase", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = "Rebase")]
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

    // Accumulates branch names received via the pipeline so we can validate
    // that exactly one branch was supplied before executing.
    private readonly List<string> pipelineNames = [];

    // ── Rebase parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the name of the upstream branch to rebase the current branch onto.
    /// Binds from the <c>Name</c> property when pipeline input comes from
    /// <c>Get-GitBranch</c>.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "Rebase")]
    [Alias("Name")]
    [ValidateNotNullOrEmpty]
    [GitBranchCompleter(IncludeRemote = true)]
    public string Upstream { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to open an interactive rebase session
    /// (<c>git rebase -i</c>).
    /// </summary>
    [Parameter(ParameterSetName = "Rebase")]
    public SwitchParameter Interactive { get; set; }

    /// <summary>
    /// Gets or sets an optional target ref for a three-way rebase
    /// (<c>git rebase --onto &lt;Onto&gt; &lt;Upstream&gt;</c>).
    /// </summary>
    [Parameter(ParameterSetName = "Rebase")]
    [GitCommittishCompleter]
    public string? Onto { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically stash uncommitted
    /// changes before the rebase and restore them afterwards
    /// (<c>git rebase --autostash</c>).
    /// </summary>
    [Parameter(ParameterSetName = "Rebase")]
    public SwitchParameter AutoStash { get; set; }

    // ── Options parameter set ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a pre-built options object, allowing full control over the rebase.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitRebaseOptions Options { get; set; } = null!;

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Collects branch names from the pipeline for validation in
    /// <see cref="EndProcessing"/>.
    /// </summary>
    protected override void ProcessRecord()
    {
        if (ParameterSetName == "Options")
        {
            // Options parameter set — execute immediately, no pipeline ambiguity.
            ExecuteRebase(Options);
            return;
        }

        pipelineNames.Add(Upstream);
    }

    /// <summary>
    /// Validates that exactly one branch was received from the pipeline, then
    /// executes the rebase.
    /// </summary>
    protected override void EndProcessing()
    {
        if (ParameterSetName == "Options")
        {
            // Already executed in ProcessRecord.
            return;
        }

        if (pipelineNames.Count > 1)
        {
            ThrowTerminatingError(new ErrorRecord(
                new InvalidOperationException(
                    $"Start-GitRebase accepts exactly one upstream branch. " +
                    $"Received {pipelineNames.Count} branches from the pipeline. " +
                    $"Pipe a single branch or specify -Upstream explicitly."),
                "StartGitRebase_MultiplePipelineInputs",
                ErrorCategory.InvalidArgument,
                pipelineNames));
            return;
        }

        if (pipelineNames.Count == 0)
        {
            // No pipeline input and no -Upstream: parameter binder would have
            // already enforced Mandatory, but guard defensively.
            return;
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
        if (ParameterSetName == "Options")
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
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

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
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "StartGitRebaseFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
