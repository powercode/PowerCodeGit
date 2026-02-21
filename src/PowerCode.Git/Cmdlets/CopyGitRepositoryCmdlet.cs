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
/// <code>Copy-GitRepository -Url https://github.com/user/repo.git -LocalPath ./my-repo -Branch main</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Copy, "GitRepository", SupportsShouldProcess = true, DefaultParameterSetName = "Clone")]
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
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Clone")]
    [ValidateNotNullOrEmpty]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the local directory to clone into.
    /// When omitted, the directory name is derived from the URL.
    /// </summary>
    [Parameter(Position = 1, ParameterSetName = "Clone")]
    public string? LocalPath { get; set; }

    /// <summary>
    /// Gets or sets the credential for HTTP authentication.
    /// </summary>
    [Parameter(ParameterSetName = "Clone")]
    [Credential]
    public PSCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to clone only the default
    /// branch (--single-branch).
    /// </summary>
    [Parameter(ParameterSetName = "Clone")]
    public SwitchParameter SingleBranch { get; set; }

    /// <summary>
    /// Gets or sets the branch name to check out after cloning.
    /// </summary>
    [Parameter(ParameterSetName = "Clone")]
    [Alias("Branch")]
    public string? BranchName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to create a bare repository.
    /// </summary>
    [Parameter(ParameterSetName = "Clone")]
    public SwitchParameter Bare { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to recursively clone submodules.
    /// </summary>
    [Parameter(ParameterSetName = "Clone")]
    public SwitchParameter RecurseSubmodules { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitCloneOptions"/> instance.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitCloneOptions? Options { get; set; }

    /// <summary>
    /// Builds the <see cref="GitCloneOptions"/> from current parameter values.
    /// </summary>
    internal GitCloneOptions BuildOptions()
    {
        if (Options is not null)
        {
            return Options;
        }

        return new GitCloneOptions
        {
            Url = Url,
            LocalPath = LocalPath,
            CredentialUsername = Credential?.UserName,
            CredentialPassword = Credential?.GetNetworkCredential()?.Password,
            SingleBranch = SingleBranch.IsPresent,
            BranchName = BranchName,
            Bare = Bare.IsPresent,
            RecurseSubmodules = RecurseSubmodules.IsPresent,
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var urlDescription = Options?.Url ?? Url;

        if (!ShouldProcess(urlDescription, "Clone repository"))
        {
            return;
        }

        try
        {
            var cloneOptions = BuildOptions();

            var resultPath = remoteService.Clone(cloneOptions, (percent, message) =>
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
                urlDescription);

            WriteError(errorRecord);
        }
    }
}
