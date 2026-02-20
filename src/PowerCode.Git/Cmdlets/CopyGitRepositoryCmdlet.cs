using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Clones a remote git repository to a local path (git clone).
/// <example>
/// <code>Copy-GitRepository -Url https://github.com/user/repo.git</code>
/// </example>
/// <example>
/// <code>Copy-GitRepository -Url https://github.com/user/repo.git -LocalPath ./my-repo</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Copy, "GitRepository", SupportsShouldProcess = true)]
[OutputType(typeof(string))]
public sealed class CopyGitRepositoryCmdlet : PSCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CopyGitRepositoryCmdlet"/> class.
    /// </summary>
    public CopyGitRepositoryCmdlet()
        : this(ServiceFactory.CreateGitRemoteService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CopyGitRepositoryCmdlet"/> class.
    /// </summary>
    /// <param name="remoteService">The remote service used by the cmdlet.</param>
    internal CopyGitRepositoryCmdlet(IGitRemoteService remoteService)
    {
        this.remoteService = remoteService ?? throw new ArgumentNullException(nameof(remoteService));
    }

    private readonly IGitRemoteService remoteService;

    /// <summary>
    /// Gets or sets the remote URL to clone from.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the local directory to clone into.
    /// When omitted, the directory name is derived from the URL.
    /// </summary>
    [Parameter(Position = 1)]
    public string? LocalPath { get; set; }

    /// <summary>
    /// Gets or sets the credential for HTTP authentication.
    /// </summary>
    [Parameter]
    [Credential]
    public PSCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to clone only the default
    /// branch (--single-branch).
    /// </summary>
    [Parameter]
    public SwitchParameter SingleBranch { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Url, "Clone repository"))
        {
            return;
        }

        try
        {
            var options = new GitCloneOptions
            {
                Url = Url,
                LocalPath = LocalPath,
                CredentialUsername = Credential?.UserName,
                CredentialPassword = Credential?.GetNetworkCredential()?.Password,
                SingleBranch = SingleBranch.IsPresent,
            };

            var resultPath = remoteService.Clone(options, (percent, message) =>
            {
                var progressRecord = new ProgressRecord(1, "Cloning repository", message)
                {
                    PercentComplete = percent,
                };
                WriteProgress(progressRecord);
            });

            WriteObject(resultPath);
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "CopyGitRepositoryFailed",
                ErrorCategory.InvalidOperation,
                Url);

            WriteError(errorRecord);
        }
    }
}
