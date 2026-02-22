using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Creates a commit from the current index (git commit).
/// <example>
/// <code>Save-GitCommit -Message "Add new feature"</code>
/// </example>
/// <example>
/// <code>Save-GitCommit -Amend</code>
/// </example>
/// <example>
/// <code>Save-GitCommit -All -Message "Track all changes"</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.Save, "GitCommit", SupportsShouldProcess = true, DefaultParameterSetName = "Commit")]
[OutputType(typeof(GitCommitInfo))]
public sealed class SaveGitCommitCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveGitCommitCmdlet"/> class.
    /// </summary>
    public SaveGitCommitCmdlet()
        : this(ServiceFactory.CreateGitHistoryService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveGitCommitCmdlet"/> class.
    /// </summary>
    /// <param name="historyService">The history service used by the cmdlet.</param>
    internal SaveGitCommitCmdlet(IGitHistoryService historyService)
    {
        this.historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
    }

    private readonly IGitHistoryService historyService;

    /// <summary>
    /// Gets or sets the commit message.
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = "Commit")]
    // git -m muscle-memory alias
    [Alias("m")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to amend the previous commit.
    /// </summary>
    [Parameter(ParameterSetName = "Commit")]
    public SwitchParameter Amend { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow creating an empty commit.
    /// </summary>
    [Parameter(ParameterSetName = "Commit")]
    public SwitchParameter AllowEmpty { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically stage all tracked modified files before committing (git commit -a).
    /// </summary>
    [Parameter(ParameterSetName = "Commit")]
    public SwitchParameter All { get; set; }

    /// <summary>
    /// Gets or sets the author in "Name &lt;email&gt;" format. Null uses the git config identity.
    /// </summary>
    [Parameter(ParameterSetName = "Commit")]
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the author/committer date override. Null uses the current time.
    /// </summary>
    [Parameter(ParameterSetName = "Commit")]
    public DateTimeOffset? Date { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitCommitOptions"/> instance.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitCommitOptions? Options { get; set; }

    /// <summary>
    /// Builds the <see cref="GitCommitOptions"/> from the current parameter values.
    /// </summary>
    /// <param name="currentFileSystemPath">The current working directory path.</param>
    internal GitCommitOptions BuildOptions(string currentFileSystemPath)
    {
        if (Options is not null)
        {
            return Options;
        }

        return new GitCommitOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Message = Message,
            Amend = Amend.IsPresent,
            AllowEmpty = AllowEmpty.IsPresent,
            All = All.IsPresent,
            Author = Author,
            Date = Date,
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var description = Amend.IsPresent ? "Amend previous commit" : "Create commit";

        try
        {
            var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

            if (!ShouldProcess(options.RepositoryPath, description))
            {
                return;
            }

            var result = historyService.Commit(options);
            WriteObject(result);
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "SaveGitCommitFailed",
                ErrorCategory.InvalidOperation,
                RepoPath);

            WriteError(errorRecord);
        }
    }
}
