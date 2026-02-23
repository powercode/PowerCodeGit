using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Sets a git configuration value (<c>git config &lt;name&gt; &lt;value&gt;</c>).
/// <example>
/// <code>Set-GitConfiguration -Name user.name -Value "Jane Doe"</code>
/// </example>
/// <example>
/// <code>Set-GitConfiguration -Name core.autocrlf -Value input -Scope Global</code>
/// </example>
/// <example>
/// <code>Set-GitConfiguration -Name init.defaultBranch -Value main</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Set, "GitConfiguration", SupportsShouldProcess = true, DefaultParameterSetName = ConfigParameterSet)]
[OutputType(typeof(GitConfigEntry))]
public sealed class SetGitConfigurationCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetGitConfigurationCmdlet"/> class.
    /// </summary>
    public SetGitConfigurationCmdlet()
        : this(ServiceFactory.CreateGitConfigService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SetGitConfigurationCmdlet"/> class.
    /// </summary>
    /// <param name="configService">The configuration service used by the cmdlet.</param>
    internal SetGitConfigurationCmdlet(IGitConfigService configService)
    {
        this.configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    private readonly IGitConfigService configService;

    private const string ConfigParameterSet = "Config";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the fully-qualified configuration key (e.g. <c>user.name</c>).
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = ConfigParameterSet)]
    [ValidateNotNullOrEmpty]
    [GitConfigNameCompleter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value to assign to the configuration key.
    /// </summary>
    [Parameter(Mandatory = true, Position = 1, ParameterSetName = ConfigParameterSet)]
    [ValidateNotNull]
    [GitConfigValueCompleter]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scope to write the setting into.
    /// When not specified, git's default (local) is used.
    /// </summary>
    [Parameter(ParameterSetName = ConfigParameterSet)]
    public GitConfigScope? Scope { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object for full control over configuration.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitConfigSetOptions Options { get; set; } = null!;

    /// <summary>
    /// Builds a <see cref="GitConfigSetOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path used to resolve the repository when
    /// <see cref="GitCmdlet.RepoPath"/> is not explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitConfigSetOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitConfigSetOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Value = Value,
            Scope = Scope,
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);
        var scopeLabel = options.Scope.HasValue ? $" ({options.Scope.Value})" : string.Empty;

        if (!ShouldProcess(options.RepositoryPath, $"Set config '{options.Name}' = '{options.Value}'{scopeLabel}"))
        {
            return;
        }

        try
        {
            configService.SetConfigValue(options);

            var entry = new GitConfigEntry
            {
                Name = options.Name,
                Value = options.Value,
                Scope = options.Scope,
            };

            WriteObject(entry);
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "SetGitConfigurationFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
