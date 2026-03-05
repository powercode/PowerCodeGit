using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Lists configured remotes in a git repository, equivalent to <c>git remote -v</c>.
/// </summary>
/// <example>
/// <code>Get-GitRemote</code>
/// </example>
/// <example>
/// <code>Get-GitRemote -Name origin</code>
/// </example>
[Cmdlet(VerbsCommon.Get, "GitRemote", DefaultParameterSetName = RemoteParameterSet)]
[OutputType(typeof(GitRemoteInfo))]
public sealed class GetGitRemoteCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitRemoteCmdlet"/> class
    /// using the default service from the dependency context.
    /// </summary>
    public GetGitRemoteCmdlet()
        : this(ServiceFactory.CreateGitRemoteService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitRemoteCmdlet"/> class
    /// with the specified remote service for testability.
    /// </summary>
    /// <param name="remoteService">The remote service to use.</param>
    internal GetGitRemoteCmdlet(IGitRemoteService remoteService)
    {
        this.remoteService = remoteService ?? throw new ArgumentNullException(nameof(remoteService));
    }

    private readonly IGitRemoteService remoteService;
    private const string RemoteParameterSet = "Remote";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the name of a specific remote to retrieve.
    /// When omitted, all remotes are listed.
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = RemoteParameterSet)]
    [ValidateNotNullOrEmpty]
    [GitRemoteCompleter]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitRemoteListOptions"/> object for
    /// programmatic use. Mutually exclusive with individual parameters.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitRemoteListOptions Options { get; set; } = null!;

    /// <summary>
    /// Builds the <see cref="GitRemoteListOptions"/> from current parameter values.
    /// </summary>
    /// <param name="currentFileSystemPath">The current file-system path for repository resolution.</param>
    /// <returns>A configured <see cref="GitRemoteListOptions"/>.</returns>
    internal GitRemoteListOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitRemoteListOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
        };
    }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        try
        {
            var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);
            var remotes = remoteService.GetRemotes(options);

            foreach (var remote in remotes)
            {
                WriteObject(remote);
            }
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "GetGitRemoteFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
