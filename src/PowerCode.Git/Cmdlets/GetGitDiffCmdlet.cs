using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Retrieves diff entries for working tree or staged changes in a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitDiff", DefaultParameterSetName = WorkingTreeParameterSet)]
[OutputType(typeof(GitDiffEntry))]
[OutputType(typeof(GitDiffHunk))]
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

    private const string WorkingTreeParameterSet = "WorkingTree";
    private const string StagedParameterSet = "Staged";
    private const string CommitParameterSet = "Commit";
    private const string RangeParameterSet = "Range";
    private const string OptionsParameterSet = "Options";

    // ── WorkingTree parameter set ────────────────────────────────────────────

    /// <summary>
    /// Gets or sets one or more repository-relative file paths to restrict the diff output.
    /// </summary>
    [Parameter(ParameterSetName = WorkingTreeParameterSet)]
    [Parameter(ParameterSetName = StagedParameterSet)]
    [Parameter(ParameterSetName = CommitParameterSet)]
    [Parameter(ParameterSetName = RangeParameterSet)]
    [GitModifiedPathCompleter(StagedParameterName = nameof(Staged))]
    public string[]? Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore whitespace changes.
    /// </summary>
    [Parameter(ParameterSetName = WorkingTreeParameterSet)]
    [Parameter(ParameterSetName = StagedParameterSet)]
    [Parameter(ParameterSetName = CommitParameterSet)]
    [Parameter(ParameterSetName = RangeParameterSet)]
    public SwitchParameter IgnoreWhitespace { get; set; }

    /// <summary>
    /// Gets or sets the number of context lines surrounding each change.
    /// Equivalent to <c>git diff -U&lt;n&gt;</c> / <c>--unified=&lt;n&gt;</c>.
    /// When omitted the library default (3 lines) is used.
    /// </summary>
    [Parameter(ParameterSetName = WorkingTreeParameterSet)]
    [Parameter(ParameterSetName = StagedParameterSet)]
    [Parameter(ParameterSetName = CommitParameterSet)]
    [Parameter(ParameterSetName = RangeParameterSet)]
    [ValidateRange(0, int.MaxValue)]
    public int Context { get; set; }

    // ── Staged parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether to show staged (index) changes.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = StagedParameterSet)]
    public SwitchParameter Staged { get; set; }

    // ── Commit parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a committish to diff the working tree against.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = CommitParameterSet)]
    [GitCommittishCompleter]
    public string? Commit { get; set; }

    // ── Range parameter set ──────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the starting committish for a range diff.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = RangeParameterSet)]
    [GitCommittishCompleter]
    public string? FromCommit { get; set; }

    /// <summary>
    /// Gets or sets the ending committish for a range diff.
    /// </summary>
    [Parameter(Mandatory = true, Position = 1, ParameterSetName = RangeParameterSet)]
    [GitCommittishCompleter]
    public string? ToCommit { get; set; }

    // ── Options parameter set ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether to emit individual
    /// <see cref="GitDiffHunk"/> objects instead of file-level
    /// <see cref="GitDiffEntry"/> objects.
    /// </summary>
    [Parameter(ParameterSetName = WorkingTreeParameterSet)]
    [Parameter(ParameterSetName = StagedParameterSet)]
    [Parameter(ParameterSetName = CommitParameterSet)]
    [Parameter(ParameterSetName = RangeParameterSet)]
    public SwitchParameter Hunk { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object, allowing full control over the operation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitDiffOptions Options { get; set; } = null!;

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates diff options from cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">The current PowerShell file system path.</param>
    /// <returns>A populated diff options object.</returns>
    internal GitDiffOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        var repositoryPath = ResolveRepositoryPath(currentFileSystemPath);
        int? contextLines = MyInvocation.BoundParameters.ContainsKey(nameof(Context)) ? Context : null;
        return new GitDiffOptions
        {
            RepositoryPath = repositoryPath,
            Staged = Staged.IsPresent,
            Commit = Commit,
            FromCommit = FromCommit,
            ToCommit = ToCommit,
            IgnoreWhitespace = IgnoreWhitespace.IsPresent,
            ContextLines = contextLines,
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

            if (Hunk.IsPresent)
            {
                foreach (var entry in entries)
                {
                    var hunks = DiffHunkParser.Parse(entry);

                    foreach (var hunk in hunks)
                    {
                        WriteObject(hunk);
                    }
                }
            }
            else
            {
                foreach (var entry in entries)
                {
                    WriteObject(entry);
                }
            }
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
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
