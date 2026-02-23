using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Pulls remote changes into the current branch (git pull).
/// <example>
/// <code>Receive-GitBranch</code>
/// </example>
/// <example>
/// <code>Receive-GitBranch -MergeStrategy FastForward -Prune</code>
/// </example>
/// <example>
/// <code>Receive-GitBranch -AutoStash</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommunications.Receive, "GitBranch", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = PullParameterSet)]
[OutputType(typeof(GitCommitInfo))]
public sealed class ReceiveGitBranchCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiveGitBranchCmdlet"/> class.
    /// </summary>
    public ReceiveGitBranchCmdlet()
        : this(ServiceFactory.CreateGitRemoteService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiveGitBranchCmdlet"/> class.
    /// </summary>
    /// <param name="remoteService">The remote service used by the cmdlet.</param>
    internal ReceiveGitBranchCmdlet(IGitRemoteService remoteService)
    {
        this.remoteService = remoteService ?? throw new ArgumentNullException(nameof(remoteService));
    }

    private const string OptionsParameterSet = "Options";
    private const string PullParameterSet = "Pull";
    private readonly IGitRemoteService remoteService;

    /// <summary>
    /// Gets or sets the merge strategy. Defaults to <see cref="GitMergeStrategy.Merge"/>.
    /// </summary>
    [Parameter(ParameterSetName = PullParameterSet)]
    public GitMergeStrategy MergeStrategy { get; set; } = GitMergeStrategy.Merge;

    /// <summary>
    /// Gets or sets a value indicating whether to prune remote-tracking
    /// branches that no longer exist on the remote.
    /// </summary>
    [Parameter(ParameterSetName = PullParameterSet)]
    public SwitchParameter Prune { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically stash local changes
    /// before pulling and reapply them afterward (git pull --autostash).
    /// </summary>
    [Parameter(ParameterSetName = PullParameterSet)]
    public SwitchParameter AutoStash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to fetch all tags from the remote.
    /// </summary>
    [Parameter(ParameterSetName = PullParameterSet)]
    public SwitchParameter Tags { get; set; }

    /// <summary>
    /// Gets or sets the credential for HTTP authentication.
    /// </summary>
    [Parameter(ParameterSetName = PullParameterSet)]
    [Credential]
    public PSCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitPullOptions"/> instance.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitPullOptions? Options { get; set; }

    /// <summary>
    /// Builds the <see cref="GitPullOptions"/> from current parameter values.
    /// </summary>
    internal GitPullOptions BuildOptions(string currentFileSystemPath)
    {
        if (Options is not null)
        {
            return Options;
        }

        return new GitPullOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            MergeStrategy = MergeStrategy,
            Prune = Prune.IsPresent,
            AutoStash = AutoStash.IsPresent,
            // null = not specified, let the remote's default apply
            Tags = Tags.IsPresent ? true : null,
            CredentialUsername = Credential?.UserName,
            CredentialPassword = Credential?.GetNetworkCredential()?.Password,
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

            if (!ShouldProcess(options.RepositoryPath, "Pull from remote"))
            {
                return;
            }

            var result = remoteService.Pull(options, (percent, message) =>
            {
                var progressRecord = new ProgressRecord(1, "Pulling from remote", message)
                {
                    PercentComplete = percent,
                };
                WriteProgress(progressRecord);
            });

            WriteObject(result);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "ReceiveGitBranchFailed",
                ErrorCategory.InvalidOperation,
                RepoPath);

            WriteError(errorRecord);
        }
    }
}
