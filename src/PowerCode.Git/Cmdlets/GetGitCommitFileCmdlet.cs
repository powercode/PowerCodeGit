using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Retrieves the files changed by a specific commit, comparing
/// the commit's tree against its first parent.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitCommitFile", DefaultParameterSetName = "Commit")]
[OutputType(typeof(GitDiffEntry))]
[OutputType(typeof(GitDiffHunk))]
public sealed class GetGitCommitFileCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitCommitFileCmdlet"/> class.
    /// </summary>
    public GetGitCommitFileCmdlet()
        : this(ServiceFactory.CreateGitCommitFileService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitCommitFileCmdlet"/> class.
    /// </summary>
    /// <param name="commitFileService">The commit file service used by the cmdlet.</param>
    internal GetGitCommitFileCmdlet(IGitCommitFileService commitFileService)
    {
        this.commitFileService = commitFileService ?? throw new ArgumentNullException(nameof(commitFileService));
    }

    private readonly IGitCommitFileService commitFileService;

    // ── Commit parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the commit SHA or ref to inspect. Defaults to HEAD when
    /// neither this parameter nor <see cref="InputObject"/> is specified.
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = "Commit")]
    [GitCommittishCompleter]
    public string? Commit { get; set; }

    /// <summary>
    /// Gets or sets a <see cref="GitCommitInfo"/> object, typically received
    /// from <c>Get-GitLog</c> via pipeline input.
    /// </summary>
    [Parameter(ValueFromPipeline = true, ParameterSetName = "Commit")]
    public GitCommitInfo? InputObject { get; set; }

    /// <summary>
    /// Gets or sets one or more repository-relative file paths to restrict the output.
    /// </summary>
    [Parameter(ParameterSetName = "Commit")]
    [GitPathCompleter]
    public string[]? Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore whitespace changes.
    /// </summary>
    [Parameter(ParameterSetName = "Commit")]
    public SwitchParameter IgnoreWhitespace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to emit individual
    /// <see cref="GitDiffHunk"/> objects instead of file-level
    /// <see cref="GitDiffEntry"/> objects.
    /// </summary>
    [Parameter(ParameterSetName = "Commit")]
    public SwitchParameter Hunk { get; set; }

    // ── Options parameter set ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a pre-built options object, allowing full control over the operation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitCommitFileOptions Options { get; set; } = null!;

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates commit file options from cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">The current PowerShell file system path.</param>
    /// <returns>A populated commit file options object.</returns>
    internal GitCommitFileOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == "Options")
        {
            return Options;
        }

        var repositoryPath = ResolveRepositoryPath(currentFileSystemPath);

        // Pipeline input takes precedence when -Commit is not explicitly specified.
        var commitRef = Commit ?? InputObject?.Sha;

        return new GitCommitFileOptions
        {
            RepositoryPath = repositoryPath,
            Commit = commitRef,
            Paths = Path,
            IgnoreWhitespace = IgnoreWhitespace.IsPresent,
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        try
        {
            var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);
            var entries = commitFileService.GetCommitFiles(options);

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
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "GetGitCommitFileFailed",
                ErrorCategory.InvalidOperation,
                RepoPath);

            WriteError(errorRecord);
        }
    }
}
