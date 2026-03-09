using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Retrieves the working tree and index status of a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitStatus", DefaultParameterSetName = StatusParameterSet)]
[OutputType(typeof(GitStatusResult))]
public sealed class GetGitStatusCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitStatusCmdlet"/> class.
    /// </summary>
    public GetGitStatusCmdlet()
        : this(ServiceFactory.CreateGitWorkingTreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitStatusCmdlet"/> class.
    /// </summary>
    /// <param name="workingTreeService">The working tree service used by the cmdlet.</param>
    internal GetGitStatusCmdlet(IGitWorkingTreeService workingTreeService)
    {
        this.workingTreeService = workingTreeService ?? throw new ArgumentNullException(nameof(workingTreeService));
    }

    private readonly IGitWorkingTreeService workingTreeService;

    private const string StatusParameterSet = "Status";
    private const string OptionsParameterSet = "Options";

    // ── Status parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether files matched by <c>.gitignore</c>
    /// should be included in the status results.
    /// </summary>
    [Parameter(ParameterSetName = StatusParameterSet)]
    public SwitchParameter IncludeIgnored { get; set; }

    /// <summary>
    /// Gets or sets an optional array of pathspec patterns to restrict the status query to.
    /// Supports git-style globs: <c>*</c> (single segment), <c>**</c> (cross-directory),
    /// <c>?</c> (single character), and directory prefixes (e.g. <c>src/</c>).
    /// </summary>
    [Parameter(ParameterSetName = StatusParameterSet)]
    [SupportsWildcards]
    [GitPathCompleter]
    public string[]? Path { get; set; }

    /// <summary>
    /// Gets or sets a value controlling how untracked files are shown.
    /// </summary>
    [Parameter(ParameterSetName = StatusParameterSet)]
    public GitUntrackedFilesMode? UntrackedFiles { get; set; }

    // ── Options parameter set ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a pre-built options object, allowing full control over the operation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitStatusOptions Options { get; set; } = null!;

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="GitStatusOptions"/> from the current parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitStatusOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitStatusOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            IncludeIgnored = IncludeIgnored.IsPresent,
            Paths = Path,
            UntrackedFilesMode = UntrackedFiles,
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(currentFileSystemPath: SessionState.Path.CurrentFileSystemLocation.Path);

        try
        {
            var result = workingTreeService.GetStatus(options);
            WriteObject(result);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "GetGitStatusFailed",
                ErrorCategory.InvalidOperation,
                options.RepositoryPath);

            WriteError(errorRecord);
        }
    }
}
