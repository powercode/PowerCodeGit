using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Removes a remote from a git repository, equivalent to <c>git remote remove &lt;name&gt;</c>.
/// All remote-tracking branches and configuration settings for the remote are deleted.
/// </summary>
/// <example>
/// <code>Remove-GitRemote -Name staging</code>
/// </example>
/// <example>
/// <code>Get-GitRemote -Name staging | Remove-GitRemote</code>
/// </example>
[Cmdlet(VerbsCommon.Remove, "GitRemote", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = DeleteParameterSet)]
public sealed class RemoveGitRemoteCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveGitRemoteCmdlet"/> class
    /// using the default service from the dependency context.
    /// </summary>
    public RemoveGitRemoteCmdlet()
        : this(ServiceFactory.CreateGitRemoteService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveGitRemoteCmdlet"/> class
    /// with the specified remote service for testability.
    /// </summary>
    /// <param name="remoteService">The remote service to use.</param>
    internal RemoveGitRemoteCmdlet(IGitRemoteService remoteService)
    {
        this.remoteService = remoteService ?? throw new ArgumentNullException(nameof(remoteService));
    }

    private readonly IGitRemoteService remoteService;
    private const string DeleteParameterSet = "Delete";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the name of the remote to remove.
    /// Accepts pipeline input by property name, enabling <c>Get-GitRemote | Remove-GitRemote</c>.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = DeleteParameterSet)]
    [ValidateNotNullOrEmpty]
    [GitRemoteCompleter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitRemoteRemoveOptions"/> object for
    /// programmatic use. Mutually exclusive with individual parameters.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitRemoteRemoveOptions Options { get; set; } = null!;

    /// <summary>
    /// Builds the <see cref="GitRemoteRemoveOptions"/> from current parameter values.
    /// </summary>
    /// <param name="currentFileSystemPath">The current file-system path for repository resolution.</param>
    /// <returns>A configured <see cref="GitRemoteRemoveOptions"/>.</returns>
    internal GitRemoteRemoveOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitRemoteRemoveOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
        };
    }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        if (!ShouldProcess(options.RepositoryPath, $"Remove remote '{options.Name}'"))
        {
            return;
        }

        try
        {
            remoteService.RemoveRemote(options);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "RemoveGitRemoteFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
