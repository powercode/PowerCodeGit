using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Configures an existing local git branch by setting its upstream remote,
/// upstream merge ref, and/or description. Equivalent to
/// <c>git branch --set-upstream-to</c> and <c>git branch --edit-description</c>.
/// </summary>
/// <example>
/// <code>Set-GitBranch -Name feature/login -Remote origin -Description "Login feature"</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "GitBranch", SupportsShouldProcess = true, DefaultParameterSetName = BranchParameterSet)]
[OutputType(typeof(GitBranchInfo))]
public sealed class SetGitBranchCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetGitBranchCmdlet"/> class
    /// using the default service from the dependency context.
    /// </summary>
    public SetGitBranchCmdlet()
        : this(ServiceFactory.CreateGitBranchService()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SetGitBranchCmdlet"/> class
    /// with the specified branch service for testability.
    /// </summary>
    /// <param name="branchService">The branch service to use.</param>
    internal SetGitBranchCmdlet(IGitBranchService branchService)
    {
        this.branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
    }

    private readonly IGitBranchService branchService;
    private const string BranchParameterSet = "Branch";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the name of the local branch to configure.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = BranchParameterSet)]
    [ValidateNotNullOrEmpty]
    [GitBranchCompleter]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remote name to set as <c>branch.&lt;name&gt;.remote</c>.
    /// </summary>
    [Parameter(ParameterSetName = BranchParameterSet)]
    [ValidateNotNullOrEmpty]
    [GitRemoteCompleter]
    public string? Remote { get; set; }

    /// <summary>
    /// Gets or sets the upstream branch name to set as <c>branch.&lt;name&gt;.merge</c>.
    /// A short name like <c>main</c> is stored as <c>refs/heads/main</c>.
    /// </summary>
    [Parameter(ParameterSetName = BranchParameterSet)]
    [ValidateNotNullOrEmpty]
    [GitBranchCompleter(IncludeRemote = true)]
    public string? Upstream { get; set; }

    /// <summary>
    /// Gets or sets the branch description to set as <c>branch.&lt;name&gt;.description</c>.
    /// </summary>
    [Parameter(ParameterSetName = BranchParameterSet)]
    [ValidateNotNull]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitBranchSetOptions"/> object for full
    /// programmatic control. Mutually exclusive with individual parameters.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitBranchSetOptions Options { get; set; } = null!;

    /// <summary>
    /// Translates cmdlet parameters into a <see cref="GitBranchSetOptions"/> instance.
    /// </summary>
    /// <param name="currentFileSystemPath">The current file-system path for repository resolution.</param>
    /// <returns>A configured <see cref="GitBranchSetOptions"/>.</returns>
    internal GitBranchSetOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitBranchSetOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Remote = Remote,
            Upstream = Upstream,
            Description = Description,
        };
    }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        var changes = BuildChangeDescription(options);
        if (!ShouldProcess(options.RepositoryPath, $"Configure branch '{options.Name}': {changes}"))
        {
            return;
        }

        try
        {
            var result = branchService.SetBranch(options);
            WriteObject(result);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception, "SetGitBranchFailed", ErrorCategory.InvalidOperation, RepoPath));
        }
    }

    private static string BuildChangeDescription(GitBranchSetOptions options)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (options.Remote is not null) parts.Add($"remote={options.Remote}");
        if (options.Upstream is not null) parts.Add($"upstream={options.Upstream}");
        if (options.Description is not null) parts.Add($"description={options.Description}");
        return parts.Count > 0 ? string.Join(", ", parts) : "no changes";
    }
}
