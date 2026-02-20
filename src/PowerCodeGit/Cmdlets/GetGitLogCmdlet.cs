using System;
using System.Management.Automation;
using PowerCodeGit.Abstractions.Models;
using PowerCodeGit.Abstractions.Services;
using PowerCodeGit.Completers;

namespace PowerCodeGit.Cmdlets;

/// <summary>
/// Retrieves commit history from a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitLog")]
[OutputType(typeof(GitCommitInfo))]
public sealed class GetGitLogCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitLogCmdlet"/> class.
    /// </summary>
    public GetGitLogCmdlet()
        : this(ServiceFactory.CreateGitHistoryService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitLogCmdlet"/> class.
    /// </summary>
    /// <param name="gitHistoryService">The history service used by the cmdlet.</param>
    internal GetGitLogCmdlet(IGitHistoryService gitHistoryService)
    {
        this.gitHistoryService = gitHistoryService ?? throw new ArgumentNullException(nameof(gitHistoryService));
    }

    private readonly IGitHistoryService gitHistoryService;

    /// <summary>
    /// Gets or sets one or more repository-relative file paths to filter
    /// the log to commits that touched those files.
    /// </summary>
    [Parameter]
    [GitPathCompleter]
    public string[]? Path { get; set; }

    /// <summary>
    /// Gets or sets the branch name used for log traversal.
    /// </summary>
    [Parameter]
    [GitBranchCompleter]
    public string? Branch { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of commits to return.
    /// </summary>
    [Parameter]
    [ValidateRange(1, int.MaxValue)]
    public int? MaxCount { get; set; }

    /// <summary>
    /// Gets or sets the author filter.
    /// </summary>
    [Parameter]
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the minimum author date.
    /// </summary>
    [Parameter]
    public DateTime? Since { get; set; }

    /// <summary>
    /// Gets or sets the maximum author date.
    /// </summary>
    [Parameter]
    public DateTime? Until { get; set; }

    /// <summary>
    /// Gets or sets a commit message pattern filter.
    /// </summary>
    [Parameter]
    public string? MessagePattern { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        try
        {
            var commits = gitHistoryService.GetLog(options);

            foreach (var commit in commits)
            {
                WriteObject(commit);
            }
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "GetGitLogFailed",
                ErrorCategory.InvalidOperation,
                options.RepositoryPath);

            WriteError(errorRecord);
        }
    }

    /// <summary>
    /// Creates git log options from cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">The current PowerShell file system path.</param>
    /// <returns>A populated git log options object.</returns>
    internal GitLogOptions BuildOptions(string currentFileSystemPath)
    {
        var repositoryPath = ResolveRepositoryPath(currentFileSystemPath);

        return new GitLogOptions
        {
            RepositoryPath = repositoryPath,
            BranchName = Branch,
            MaxCount = MaxCount,
            AuthorFilter = Author,
            Since = Since,
            Until = Until,
            MessagePattern = MessagePattern,
            Paths = Path,
        };
    }
}