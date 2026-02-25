using System;
using System.Linq;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Formatting;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Gets a Powerline-styled string representing the current git repository status,
/// suitable for embedding in an interactive shell prompt.
/// </summary>
/// <remarks>
/// <para>
/// The returned <see cref="GitPromptStatus"/> object's <see cref="GitPromptStatus.ToString()"/>
/// method produces the formatted prompt string, so it can be used directly in PowerShell
/// string interpolation:
/// </para>
/// <example>
/// <code>
/// function prompt { "$(Get-GitPromptStatus) > " }
/// </code>
/// </example>
/// <para>
/// The formatted string uses Nerd Font glyphs for known upstream providers
/// (GitHub, GitLab, Bitbucket, Azure DevOps). A Nerd Font must be installed and
/// configured as the terminal font for the glyphs to render correctly.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "GitPromptStatus", DefaultParameterSetName = PromptParameterSet)]
[OutputType(typeof(GitPromptStatus))]
public sealed class GetGitPromptStatusCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitPromptStatusCmdlet"/> class.
    /// </summary>
    public GetGitPromptStatusCmdlet()
        : this(ServiceFactory.CreateGitWorkingTreeService(), ServiceFactory.CreateGitRemoteService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitPromptStatusCmdlet"/> class
    /// with explicit service dependencies for unit testing.
    /// </summary>
    /// <param name="workingTreeService">The working tree service.</param>
    /// <param name="remoteService">The remote service.</param>
    internal GetGitPromptStatusCmdlet(
        IGitWorkingTreeService workingTreeService,
        IGitRemoteService remoteService)
    {
        this.workingTreeService = workingTreeService ?? throw new ArgumentNullException(nameof(workingTreeService));
        this.remoteService = remoteService ?? throw new ArgumentNullException(nameof(remoteService));
    }

    private readonly IGitWorkingTreeService workingTreeService;
    private readonly IGitRemoteService remoteService;

    private const string PromptParameterSet = "Prompt";
    private const string OptionsParameterSet = "Options";

    // ── Prompt parameter set ──────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether the upstream provider icon
    /// and ahead/behind counts should be omitted from the prompt string.
    /// </summary>
    [Parameter(ParameterSetName = PromptParameterSet)]
    public SwitchParameter HideUpstream { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the staged, modified, and
    /// untracked file counts should be omitted from the prompt string.
    /// </summary>
    [Parameter(ParameterSetName = PromptParameterSet)]
    public SwitchParameter HideCounts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the stash count indicator
    /// should be omitted from the prompt string.
    /// </summary>
    [Parameter(ParameterSetName = PromptParameterSet)]
    public SwitchParameter HideStash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ANSI color escape sequences
    /// should be stripped from the prompt string.
    /// </summary>
    [Parameter(ParameterSetName = PromptParameterSet)]
    public SwitchParameter NoColor { get; set; }

    // ── Options parameter set ─────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a pre-built options object that provides full control
    /// over the prompt generation parameters.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitPromptStatusOptions Options { get; set; } = null!;

    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="GitPromptStatusOptions"/> from the current parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/>
    /// when not explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitPromptStatusOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        var config = ModuleConfiguration.Current;

        return new GitPromptStatusOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            HideUpstream = HideUpstream.IsPresent || (config.PromptHideUpstream ?? false),
            HideCounts = HideCounts.IsPresent || (config.PromptHideCounts ?? false),
            HideStash = HideStash.IsPresent || (config.PromptHideStash ?? false),
            NoColor = NoColor.IsPresent || (config.PromptNoColor ?? false),
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(currentFileSystemPath: SessionState.Path.CurrentFileSystemLocation.Path);

        try
        {
            var promptInfo = workingTreeService.GetPromptInfo(options.RepositoryPath);

            var provider = ResolveProvider(options.RepositoryPath, promptInfo.TrackedBranchName);

            var status = new GitPromptStatus(
                options.RepositoryPath,
                promptInfo.BranchName,
                provider,
                promptInfo.TrackedBranchName,
                promptInfo.AheadBy,
                promptInfo.BehindBy,
                promptInfo.StagedCount,
                promptInfo.ModifiedCount,
                promptInfo.UntrackedCount,
                promptInfo.StashCount,
                promptInfo.IsDetachedHead);

            status.FormattedString = GitPromptFormatter.Format(
                status,
                noColor: options.NoColor,
                hideUpstream: options.HideUpstream,
                hideCounts: options.HideCounts,
                hideStash: options.HideStash);

            WriteObject(status);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "GetGitPromptStatusFailed",
                ErrorCategory.InvalidOperation,
                options.RepositoryPath);

            WriteError(errorRecord);
        }
    }

    /// <summary>
    /// Resolves the upstream hosting provider by looking up the remote URL for the
    /// tracking branch remote (preferring <c>origin</c> as a fallback).
    /// </summary>
    private GitUpstreamProvider ResolveProvider(string repositoryPath, string? trackedBranchName)
    {
        try
        {
            var remotes = remoteService.GetRemotes(repositoryPath);

            if (remotes.Count == 0)
            {
                return GitUpstreamProvider.Unknown;
            }

            // Prefer the remote that matches the tracked branch (e.g. "origin" from "origin/main").
            string? preferredRemoteName = null;
            if (trackedBranchName is not null)
            {
                var slashIndex = trackedBranchName.IndexOf('/');
                if (slashIndex > 0)
                {
                    preferredRemoteName = trackedBranchName[..slashIndex];
                }
            }

            var remote = preferredRemoteName is not null
                ? remotes.FirstOrDefault(r => string.Equals(r.Name, preferredRemoteName, StringComparison.OrdinalIgnoreCase))
                    ?? remotes.FirstOrDefault(r => string.Equals(r.Name, "origin", StringComparison.OrdinalIgnoreCase))
                    ?? remotes[0]
                : remotes.FirstOrDefault(r => string.Equals(r.Name, "origin", StringComparison.OrdinalIgnoreCase))
                    ?? remotes[0];

            return GitPromptFormatter.DetectProvider(remote.FetchUrl);
        }
        catch
        {
            // Provider detection is best-effort; a failure here must not break the prompt.
            return GitUpstreamProvider.Unknown;
        }
    }
}
