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
/// </summary>
[Cmdlet(VerbsCommunications.Receive, "GitBranch")]
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

    private readonly IGitRemoteService remoteService;

    /// <summary>
    /// Gets or sets the merge strategy. Defaults to <see cref="GitMergeStrategy.Merge"/>.
    /// </summary>
    [Parameter]
    public GitMergeStrategy MergeStrategy { get; set; } = GitMergeStrategy.Merge;

    /// <summary>
    /// Gets or sets a value indicating whether to prune remote-tracking
    /// branches that no longer exist on the remote.
    /// </summary>
    [Parameter]
    public SwitchParameter Prune { get; set; }

    /// <summary>
    /// Gets or sets the credential for HTTP authentication.
    /// </summary>
    [Parameter]
    [Credential]
    public PSCredential? Credential { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath();

        try
        {
            var options = new GitPullOptions
            {
                RepositoryPath = repositoryPath,
                MergeStrategy = MergeStrategy,
                Prune = Prune.IsPresent,
                CredentialUsername = Credential?.UserName,
                CredentialPassword = Credential?.GetNetworkCredential()?.Password,
            };

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
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "ReceiveGitBranchFailed",
                ErrorCategory.InvalidOperation,
                repositoryPath);

            WriteError(errorRecord);
        }
    }
}
