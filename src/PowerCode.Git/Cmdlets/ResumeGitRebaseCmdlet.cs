using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Resumes a paused rebase after resolving conflicts
/// (<c>git rebase --continue</c>) or skips the current conflicting commit
/// (<c>git rebase --skip</c>).
/// <example>
/// <code>Resume-GitRebase</code>
/// </example>
/// <example>
/// <code>Resume-GitRebase -Skip</code>
/// </example>
/// </summary>
[Cmdlet(VerbsLifecycle.Resume, "GitRebase", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = ContinueParameterSet)]
[OutputType(typeof(GitRebaseResult))]
public sealed class ResumeGitRebaseCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResumeGitRebaseCmdlet"/> class.
    /// </summary>
    public ResumeGitRebaseCmdlet()
        : this(ServiceFactory.CreateGitRebaseService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResumeGitRebaseCmdlet"/> class.
    /// </summary>
    /// <param name="rebaseService">The rebase service used by the cmdlet.</param>
    internal ResumeGitRebaseCmdlet(IGitRebaseService rebaseService)
    {
        this.rebaseService = rebaseService ?? throw new ArgumentNullException(nameof(rebaseService));
    }

    private readonly IGitRebaseService rebaseService;

    private const string ContinueParameterSet = "Continue";
    private const string OptionsParameterSet = "Options";

    // ── Continue parameter set ───────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether to skip the current conflicting
    /// commit instead of resuming normally. Equivalent to <c>git rebase --skip</c>.
    /// When not specified, <c>git rebase --continue</c> is used.
    /// </summary>
    [Parameter(ParameterSetName = ContinueParameterSet)]
    public SwitchParameter Skip { get; set; }

    // ── Options parameter set ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a pre-built options object, allowing full control over the
    /// continue/skip decision.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitRebaseContinueOptions Options { get; set; } = null!;

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        var action = options.Skip ? "--skip" : "--continue";
        if (!ShouldProcess(options.RepositoryPath, $"Resume rebase (git rebase {action})"))
        {
            return;
        }

        try
        {
            var result = rebaseService.Continue(options);
            WriteObject(result);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "ResumeGitRebaseFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    /// <summary>
    /// Builds a <see cref="GitRebaseContinueOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitRebaseContinueOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitRebaseContinueOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Skip = Skip.IsPresent,
        };
    }
}
