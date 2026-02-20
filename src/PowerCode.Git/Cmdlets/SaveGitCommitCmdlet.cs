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
/// </summary>
[Cmdlet(VerbsData.Save, "GitCommit", SupportsShouldProcess = true)]
[OutputType(typeof(GitCommitInfo))]
[Alias("sgc")]
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
    [Parameter(Position = 0)]
    [Alias("m")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to amend the previous commit.
    /// </summary>
    [Parameter]
    public SwitchParameter Amend { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow creating an empty commit.
    /// </summary>
    [Parameter]
    public SwitchParameter AllowEmpty { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath();
        var description = Amend.IsPresent ? "Amend previous commit" : "Create commit";

        if (!ShouldProcess(repositoryPath, description))
        {
            return;
        }

        try
        {
            var options = new GitCommitOptions
            {
                RepositoryPath = repositoryPath,
                Message = Message,
                Amend = Amend.IsPresent,
                AllowEmpty = AllowEmpty.IsPresent,
            };

            var result = historyService.Commit(options);
            WriteObject(result);
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "SaveGitCommitFailed",
                ErrorCategory.InvalidOperation,
                repositoryPath);

            WriteError(errorRecord);
        }
    }
}
