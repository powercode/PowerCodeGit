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
/// <example>
/// <code>Send-GitBranch -Force</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommunications.Send, "GitBranch", SupportsShouldProcess = true, DefaultParameterSetName = "Push")]
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
    [Parameter(Position = 0, ParameterSetName = "Push")]
    [GitRemoteCompleter]
    public string Remote { get; set; } = "origin";

    /// <summary>
    /// Gets or sets the branch name to push. When omitted, pushes the current
    /// HEAD branch.
    /// </summary>
    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "Push")]
    [GitBranchCompleter]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to set the upstream tracking
    /// reference (git push -u).
    /// </summary>
    [Parameter(ParameterSetName = "Push")]
    [Alias("u")]
    public SwitchParameter SetUpstream { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to force-push the branch.
    /// </summary>
    [Parameter(ParameterSetName = "Push")]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to force-push only if the remote tip matches
    /// the local expectation (--force-with-lease).
    /// </summary>
    [Parameter(ParameterSetName = "Push")]
    public SwitchParameter ForceWithLease { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to delete the branch on the remote.
    /// </summary>
    [Parameter(ParameterSetName = "Push")]
    public SwitchParameter Delete { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to push all tags.
    /// </summary>
    [Parameter(ParameterSetName = "Push")]
    public SwitchParameter Tags { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to push all branches.
    /// </summary>
    [Parameter(ParameterSetName = "Push")]
    public SwitchParameter All { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to perform a dry run without actually pushing.
    /// </summary>
    [Parameter(ParameterSetName = "Push")]
    public SwitchParameter DryRun { get; set; }

    /// <summary>
    /// Gets or sets the credential for HTTP authentication.
    /// </summary>
    [Parameter(ParameterSetName = "Push")]
    [Credential]
    public PSCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitPushOptions"/> instance.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitPushOptions? Options { get; set; }

    /// <summary>
    /// Builds the <see cref="GitPushOptions"/> from current parameter values.
    /// </summary>
    internal GitPushOptions BuildOptions(string currentFileSystemPath)
    {
        if (Options is not null)
        {
            return Options;
        }

        return new GitPushOptions
        {
            RepositoryPath = currentFileSystemPath,
            RemoteName = Remote,
            BranchName = Name,
            SetUpstream = SetUpstream.IsPresent,
            Force = Force.IsPresent,
            ForceWithLease = ForceWithLease.IsPresent,
            Delete = Delete.IsPresent,
            Tags = Tags.IsPresent,
            All = All.IsPresent,
            DryRun = DryRun.IsPresent,
            CredentialUsername = Credential?.UserName,
            CredentialPassword = Credential?.GetNetworkCredential()?.Password,
        };
    }

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
            var options = BuildOptions(repositoryPath);

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
