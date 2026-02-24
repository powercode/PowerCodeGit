using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Removes one or more git configuration keys (<c>git config --unset &lt;name&gt;</c>).
/// <example>
/// <code>Clear-GitConfiguration -Name user.name</code>
/// </example>
/// <example>
/// <code>Clear-GitConfiguration -Name core.autocrlf -Scope Global</code>
/// </example>
/// <example>
/// <code>Clear-GitConfiguration -Name user.name, user.email</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Clear, "GitConfiguration", SupportsShouldProcess = true)]
public sealed class ClearGitConfigurationCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClearGitConfigurationCmdlet"/> class.
    /// </summary>
    public ClearGitConfigurationCmdlet()
        : this(ServiceFactory.CreateGitConfigService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClearGitConfigurationCmdlet"/> class.
    /// </summary>
    /// <param name="configService">The configuration service used by the cmdlet.</param>
    internal ClearGitConfigurationCmdlet(IGitConfigService configService)
    {
        this.configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    private readonly IGitConfigService configService;

    /// <summary>
    /// Gets or sets the fully-qualified configuration key(s) to remove (e.g. <c>user.name</c>).
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    [GitConfigNameCompleter]
    public string[] Name { get; set; } = [];

    /// <summary>
    /// Gets or sets the scope from which to remove the setting.
    /// When not specified, git's default (local) is used.
    /// </summary>
    [Parameter]
    public GitConfigScope? Scope { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath(SessionState.Path.CurrentFileSystemLocation.Path);
        var scopeLabel = Scope.HasValue ? $" ({Scope.Value})" : string.Empty;

        foreach (var name in Name)
        {
            if (!ShouldProcess(repositoryPath, $"Clear config '{name}'{scopeLabel}"))
            {
                continue;
            }

            try
            {
                configService.UnsetConfigValue(new GitConfigUnsetOptions
                {
                    RepositoryPath = repositoryPath,
                    Name = name,
                    Scope = Scope,
                });
            }
            catch (Exception exception) when (exception is not PipelineStoppedException)
            {
                WriteError(new ErrorRecord(
                    exception,
                    "ClearGitConfigurationFailed",
                    ErrorCategory.InvalidOperation,
                    name));
            }
        }
    }
}
