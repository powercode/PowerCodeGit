using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Adds a new remote to a git repository, equivalent to <c>git remote add &lt;name&gt; &lt;url&gt;</c>.
/// </summary>
/// <example>
/// <code>New-GitRemote -Name upstream -Url https://github.com/user/repo.git</code>
/// </example>
/// <example>
/// <code>New-GitRemote -Name upstream -Url https://github.com/user/repo.git -PushUrl git@github.com:user/repo.git</code>
/// </example>
[Cmdlet(VerbsCommon.New, "GitRemote", SupportsShouldProcess = true, DefaultParameterSetName = CreateParameterSet)]
[OutputType(typeof(GitRemoteInfo))]
public sealed class NewGitRemoteCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewGitRemoteCmdlet"/> class
    /// using the default service from the dependency context.
    /// </summary>
    public NewGitRemoteCmdlet()
        : this(ServiceFactory.CreateGitRemoteService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewGitRemoteCmdlet"/> class
    /// with the specified remote service for testability.
    /// </summary>
    /// <param name="remoteService">The remote service to use.</param>
    internal NewGitRemoteCmdlet(IGitRemoteService remoteService)
    {
        this.remoteService = remoteService ?? throw new ArgumentNullException(nameof(remoteService));
    }

    private readonly IGitRemoteService remoteService;
    private const string CreateParameterSet = "Create";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the name of the new remote (e.g. <c>upstream</c>).
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = CreateParameterSet)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fetch URL for the new remote.
    /// </summary>
    [Parameter(Mandatory = true, Position = 1, ParameterSetName = CreateParameterSet)]
    [ValidateNotNullOrEmpty]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the push URL for the new remote.
    /// When omitted, the push URL defaults to the fetch URL.
    /// </summary>
    [Parameter(ParameterSetName = CreateParameterSet)]
    [ValidateNotNullOrEmpty]
    public string? PushUrl { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitRemoteAddOptions"/> object for
    /// programmatic use. Mutually exclusive with individual parameters.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitRemoteAddOptions Options { get; set; } = null!;

    /// <summary>
    /// Builds the <see cref="GitRemoteAddOptions"/> from current parameter values.
    /// </summary>
    /// <param name="currentFileSystemPath">The current file-system path for repository resolution.</param>
    /// <returns>A configured <see cref="GitRemoteAddOptions"/>.</returns>
    internal GitRemoteAddOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitRemoteAddOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Url = Url,
            PushUrl = PushUrl,
        };
    }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        if (!ShouldProcess(options.RepositoryPath, $"Add remote '{options.Name}' → {options.Url}"))
        {
            return;
        }

        try
        {
            var result = remoteService.AddRemote(options);
            WriteObject(result);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "NewGitRemoteFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
