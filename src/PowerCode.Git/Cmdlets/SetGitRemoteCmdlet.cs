using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Modifies an existing remote in a git repository. Supports renaming the remote
/// (<c>git remote rename</c>), changing the fetch URL (<c>git remote set-url</c>),
/// and changing the push URL (<c>git remote set-url --push</c>). Multiple changes
/// can be applied in a single call.
/// </summary>
/// <example>
/// <code>Set-GitRemote -Name origin -Url https://new-host.com/repo.git</code>
/// </example>
/// <example>
/// <code>Set-GitRemote -Name origin -NewName upstream</code>
/// </example>
/// <example>
/// <code>Set-GitRemote -Name origin -PushUrl git@github.com:user/repo.git</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "GitRemote", SupportsShouldProcess = true, DefaultParameterSetName = RemoteParameterSet)]
[OutputType(typeof(GitRemoteInfo))]
public sealed class SetGitRemoteCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetGitRemoteCmdlet"/> class
    /// using the default service from the dependency context.
    /// </summary>
    public SetGitRemoteCmdlet()
        : this(ServiceFactory.CreateGitRemoteService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SetGitRemoteCmdlet"/> class
    /// with the specified remote service for testability.
    /// </summary>
    /// <param name="remoteService">The remote service to use.</param>
    internal SetGitRemoteCmdlet(IGitRemoteService remoteService)
    {
        this.remoteService = remoteService ?? throw new ArgumentNullException(nameof(remoteService));
    }

    private readonly IGitRemoteService remoteService;
    private const string RemoteParameterSet = "Remote";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the current name of the remote to modify.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = RemoteParameterSet)]
    [ValidateNotNullOrEmpty]
    [GitRemoteCompleter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new fetch URL. Equivalent to <c>git remote set-url &lt;name&gt; &lt;url&gt;</c>.
    /// </summary>
    [Parameter(ParameterSetName = RemoteParameterSet)]
    [ValidateNotNullOrEmpty]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the new push URL. Equivalent to <c>git remote set-url --push &lt;name&gt; &lt;url&gt;</c>.
    /// </summary>
    [Parameter(ParameterSetName = RemoteParameterSet)]
    [ValidateNotNullOrEmpty]
    public string? PushUrl { get; set; }

    /// <summary>
    /// Gets or sets the new name for the remote. Equivalent to <c>git remote rename &lt;old&gt; &lt;new&gt;</c>.
    /// Can be combined with <see cref="Url"/> and <see cref="PushUrl"/> in a single call.
    /// </summary>
    [Parameter(ParameterSetName = RemoteParameterSet)]
    [ValidateNotNullOrEmpty]
    public string? NewName { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitRemoteUpdateOptions"/> object for
    /// programmatic use. Mutually exclusive with individual parameters.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitRemoteUpdateOptions Options { get; set; } = null!;

    /// <summary>
    /// Builds the <see cref="GitRemoteUpdateOptions"/> from current parameter values.
    /// </summary>
    /// <param name="currentFileSystemPath">The current file-system path for repository resolution.</param>
    /// <returns>A configured <see cref="GitRemoteUpdateOptions"/>.</returns>
    internal GitRemoteUpdateOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitRemoteUpdateOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Url = Url,
            PushUrl = PushUrl,
            NewName = NewName,
        };
    }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        try
        {
            // Track the working name as it may change after a rename
            var workingName = options.Name;
            GitRemoteInfo? result = null;

            // Rename first so subsequent URL updates target the new name
            if (options.NewName is not null)
            {
                if (!ShouldProcess(options.RepositoryPath, $"Rename remote '{options.Name}' to '{options.NewName}'"))
                {
                    return;
                }

                result = remoteService.RenameRemote(options.RepositoryPath, options.Name, options.NewName);
                workingName = options.NewName;
            }

            // Apply URL changes if requested
            if (options.Url is not null || options.PushUrl is not null)
            {
                var urlOptions = new GitRemoteUpdateOptions
                {
                    RepositoryPath = options.RepositoryPath,
                    Name = workingName,
                    Url = options.Url,
                    PushUrl = options.PushUrl,
                };

                if (!ShouldProcess(options.RepositoryPath, BuildUrlChangeDescription(urlOptions)))
                {
                    return;
                }

                result = remoteService.UpdateRemoteUrl(urlOptions);
            }

            if (result is not null)
            {
                WriteObject(result);
            }
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "SetGitRemoteFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    private static string BuildUrlChangeDescription(GitRemoteUpdateOptions options)
    {
        if (options.Url is not null && options.PushUrl is not null)
        {
            return $"Set fetch URL '{options.Url}' and push URL '{options.PushUrl}' on remote '{options.Name}'";
        }

        if (options.Url is not null)
        {
            return $"Set fetch URL '{options.Url}' on remote '{options.Name}'";
        }

        return $"Set push URL '{options.PushUrl}' on remote '{options.Name}'";
    }
}
