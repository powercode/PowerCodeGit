using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Reads git configuration values (<c>git config --list</c> or <c>git config --get</c>).
/// <example>
/// <code>Get-GitConfiguration</code>
/// </example>
/// <example>
/// <code>Get-GitConfiguration -Name user.name</code>
/// </example>
/// <example>
/// <code>Get-GitConfiguration -Scope Global</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitConfiguration", DefaultParameterSetName = "List")]
[OutputType(typeof(GitConfigEntry))]
public sealed class GetGitConfigurationCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitConfigurationCmdlet"/> class.
    /// </summary>
    public GetGitConfigurationCmdlet()
        : this(ServiceFactory.CreateGitConfigService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitConfigurationCmdlet"/> class.
    /// </summary>
    /// <param name="configService">The configuration service used by the cmdlet.</param>
    internal GetGitConfigurationCmdlet(IGitConfigService configService)
    {
        this.configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    private readonly IGitConfigService configService;

    /// <summary>
    /// Gets or sets the configuration key to retrieve.
    /// When omitted, all configuration entries are returned.
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = "List")]
    [GitConfigNameCompleter]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the scope to read from.
    /// When not specified, git searches all scopes in priority order.
    /// </summary>
    [Parameter(ParameterSetName = "List")]
    public GitConfigScope? Scope { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether each entry should include its
    /// origin scope in the output.
    /// </summary>
    [Parameter(ParameterSetName = "List")]
    public SwitchParameter ShowScope { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitConfigGetOptions"/> instance.
    /// When specified, all other parameters are ignored.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitConfigGetOptions? Options { get; set; }

    /// <summary>
    /// Builds a <see cref="GitConfigGetOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path used to resolve the repository when
    /// <see cref="GitCmdlet.RepoPath"/> is not explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitConfigGetOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == "Options")
        {
            return Options!;
        }

        return new GitConfigGetOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Scope = Scope,
            ShowScope = ShowScope.IsPresent,
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        try
        {
            var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

            if (!string.IsNullOrWhiteSpace(options.Name))
            {
                var entry = configService.GetConfigValue(options);

                if (entry is not null)
                {
                    WriteObject(entry);
                }
            }
            else
            {
                var entries = configService.GetConfigEntries(options);

                foreach (var entry in entries)
                {
                    WriteObject(entry);
                }
            }
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "GetGitConfigurationFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
