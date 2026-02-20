using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Pushes a branch to a remote (git push).
/// <example>
/// <code>Send-GitBranch</code>
/// </example>
/// <example>
/// <code>Send-GitBranch -Remote origin -SetUpstream</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommunications.Send, "GitBranch", SupportsShouldProcess = true)]
[OutputType(typeof(GitBranchInfo))]
public sealed class SendGitBranchCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendGitBranchCmdlet"/> class.
    /// </summary>
    public SendGitBranchCmdlet()
        : this(ServiceFactory.CreateGitRemoteService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SendGitBranchCmdlet"/> class.
    /// </summary>
    /// <param name="remoteService">The remote service used by the cmdlet.</param>
    internal SendGitBranchCmdlet(IGitRemoteService remoteService)
    {
        this.remoteService = remoteService ?? throw new ArgumentNullException(nameof(remoteService));
    }

    private readonly IGitRemoteService remoteService;

    /// <summary>
    /// Gets or sets the name of the remote to push to. Defaults to "origin".
    /// </summary>
    [Parameter(Position = 0)]
    [GitRemoteCompleter]
    public string Remote { get; set; } = "origin";

    /// <summary>
    /// Gets or sets the branch name to push. When omitted, pushes the current
    /// HEAD branch.
    /// </summary>
    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [GitBranchCompleter]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to set the upstream tracking
    /// reference (git push -u).
    /// </summary>
    [Parameter]
    [Alias("u")]
    public SwitchParameter SetUpstream { get; set; }

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
        var branchDescription = Name ?? "current branch";

        if (!ShouldProcess(repositoryPath, $"Push '{branchDescription}' to '{Remote}'"))
        {
            return;
        }

        try
        {
            var options = new GitPushOptions
            {
                RepositoryPath = repositoryPath,
                RemoteName = Remote,
                BranchName = Name,
                SetUpstream = SetUpstream.IsPresent,
                CredentialUsername = Credential?.UserName,
                CredentialPassword = Credential?.GetNetworkCredential()?.Password,
            };

            var result = remoteService.Push(options, (percent, message) =>
            {
                var progressRecord = new ProgressRecord(1, "Pushing to remote", message)
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
                "SendGitBranchFailed",
                ErrorCategory.InvalidOperation,
                repositoryPath);

            WriteError(errorRecord);
        }
    }
}
