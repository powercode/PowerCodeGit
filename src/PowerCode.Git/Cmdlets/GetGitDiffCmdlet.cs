using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Retrieves diff entries for working tree or staged changes in a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitDiff", DefaultParameterSetName = "WorkingTree")]
[OutputType(typeof(GitDiffEntry))]
public sealed class GetGitDiffCmdlet : GitCmdlet
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

    // ── WorkingTree parameter set ────────────────────────────────────────────

    /// <summary>
    /// Gets or sets one or more repository-relative file paths to restrict the diff output.
    /// </summary>
    [Parameter(ParameterSetName = "WorkingTree")]
    [Parameter(ParameterSetName = "Staged")]
    [Parameter(ParameterSetName = "Commit")]
    [Parameter(ParameterSetName = "Range")]
    [GitPathCompleter]
    public string[]? Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore whitespace changes.
    /// </summary>
    [Parameter(ParameterSetName = "WorkingTree")]
    [Parameter(ParameterSetName = "Staged")]
    [Parameter(ParameterSetName = "Commit")]
    [Parameter(ParameterSetName = "Range")]
    public SwitchParameter IgnoreWhitespace { get; set; }

    // ── Staged parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether to show staged (index) changes.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Staged")]
    public SwitchParameter Staged { get; set; }

    // ── Commit parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a committish to diff the working tree against.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Commit")]
    [GitCommittishCompleter]
    public string? Commit { get; set; }

    // ── Range parameter set ──────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the starting committish for a range diff.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Range")]
    [GitCommittishCompleter]
    public string? FromCommit { get; set; }

    /// <summary>
    /// Gets or sets the ending committish for a range diff.
    /// </summary>
    [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Range")]
    [GitCommittishCompleter]
    public string? ToCommit { get; set; }

    // ── Options parameter set ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a pre-built options object, allowing full control over the operation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitDiffOptions Options { get; set; } = null!;

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates diff options from cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">The current PowerShell file system path.</param>
    /// <returns>A populated diff options object.</returns>
    internal GitDiffOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == "Options")
        {
            return Options;
        }

        var repositoryPath = ResolveRepositoryPath(currentFileSystemPath);

        return new GitDiffOptions
        {
            RepositoryPath = repositoryPath,
            Staged = Staged.IsPresent,
            Commit = Commit,
            FromCommit = FromCommit,
            ToCommit = ToCommit,
            IgnoreWhitespace = IgnoreWhitespace.IsPresent,
            Paths = Path,
        };
    }

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
}
