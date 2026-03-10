using System;
using System.Linq;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Pulls remote changes into the current branch (git pull), or — when receiving
/// a <see cref="GitBranchInfo"/> pipeline from <c>Get-GitBranch -Remote</c> —
/// creates and/or fast-forwards the corresponding local tracking branches.
/// <example>
/// <code>Receive-GitBranch</code>
/// </example>
/// <example>
/// <code>Receive-GitBranch -MergeStrategy FastForward -Prune</code>
/// </example>
/// <example>
/// <code>Receive-GitBranch -AutoStash</code>
/// </example>
/// <example>
/// <code>Get-GitBranch -Remote | Receive-GitBranch -Action Create</code>
/// </example>
/// <example>
/// <code>Get-GitBranch -Remote -Include 'origin/feature/*' | Receive-GitBranch -Action CreateOrUpdate</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommunications.Receive, "GitBranch", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = PullParameterSet)]
[OutputType(typeof(GitCommitInfo), ParameterSetName = [PullParameterSet, OptionsParameterSet])]
[OutputType(typeof(GitBranchInfo), ParameterSetName = [PipelineParameterSet])]
public sealed class ReceiveGitBranchCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiveGitBranchCmdlet"/> class.
    /// </summary>
    public ReceiveGitBranchCmdlet()
        : this(ServiceFactory.CreateGitRemoteService(), ServiceFactory.CreateGitBranchService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiveGitBranchCmdlet"/> class.
    /// </summary>
    /// <param name="remoteService">The remote service used by the cmdlet.</param>
    /// <param name="branchService">The branch service used by the pipeline parameter set.</param>
    internal ReceiveGitBranchCmdlet(IGitRemoteService remoteService, IGitBranchService branchService)
    {
        this.remoteService = remoteService ?? throw new ArgumentNullException(nameof(remoteService));
        this.branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
    }

    private const string OptionsParameterSet = "Options";
    private const string PullParameterSet = "Pull";
    private const string PipelineParameterSet = "Pipeline";
    private readonly IGitRemoteService remoteService;
    private readonly IGitBranchService branchService;

    /// <summary>
    /// Gets or sets the remote-tracking branch received from the pipeline
    /// (e.g. from <c>Get-GitBranch -Remote</c>).
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PipelineParameterSet)]
    public GitBranchInfo InputBranch { get; set; } = null!;

    /// <summary>
    /// Gets or sets the action to take for each piped remote-tracking branch.
    /// </summary>
    [Parameter(ParameterSetName = PipelineParameterSet)]
    public ReceiveBranchAction Action { get; set; } = ReceiveBranchAction.Create;

    /// <summary>
    /// Gets or sets the merge strategy. Defaults to <see cref="GitMergeStrategy.Merge"/>.
    /// </summary>
    [Parameter(ParameterSetName = PullParameterSet)]
    public GitMergeStrategy MergeStrategy { get; set; } = GitMergeStrategy.Merge;

    /// <summary>
    /// Gets or sets a value indicating whether to prune remote-tracking
    /// branches that no longer exist on the remote.
    /// </summary>
    [Parameter(ParameterSetName = PullParameterSet)]
    public SwitchParameter Prune { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically stash local changes
    /// before pulling and reapply them afterward (git pull --autostash).
    /// </summary>
    [Parameter(ParameterSetName = PullParameterSet)]
    public SwitchParameter AutoStash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to fetch all tags from the remote.
    /// </summary>
    [Parameter(ParameterSetName = PullParameterSet)]
    public SwitchParameter Tags { get; set; }

    /// <summary>
    /// Gets or sets the credential for HTTP authentication.
    /// </summary>
    [Parameter(ParameterSetName = PullParameterSet)]
    [Credential]
    public PSCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitPullOptions"/> instance.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitPullOptions? Options { get; set; }

    /// <summary>
    /// Builds the <see cref="GitPullOptions"/> from current parameter values.
    /// </summary>
    internal GitPullOptions BuildOptions(string currentFileSystemPath)
    {
        if (Options is not null)
        {
            return Options;
        }

        return new GitPullOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            MergeStrategy = MergeStrategy,
            Prune = Prune.IsPresent,
            AutoStash = AutoStash.IsPresent,
            // null = not specified, let the remote's default apply
            Tags = Tags.IsPresent ? true : null,
            CredentialUsername = Credential?.UserName,
            CredentialPassword = Credential?.GetNetworkCredential()?.Password,
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        if (ParameterSetName == PipelineParameterSet)
        {
            ProcessPipelineRecord();
            return;
        }

        try
        {
            var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

            if (!ShouldProcess(options.RepositoryPath, "Pull from remote"))
            {
                return;
            }

            using var progress = new ProgressWriter(WriteProgress, 1, "Pulling from remote");
            var result = remoteService.Pull(options, progress.AsCallback());

            WriteObject(result);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "ReceiveGitBranchFailed",
                ErrorCategory.InvalidOperation,
                RepoPath);

            WriteError(errorRecord);
        }
    }

    /// <summary>
    /// Handles the pipeline parameter set: creates and/or fast-forwards local tracking
    /// branches corresponding to piped remote-tracking branches.
    /// </summary>
    private void ProcessPipelineRecord()
    {
        if (!InputBranch.IsRemote)
        {
            WriteError(new ErrorRecord(
                new ArgumentException($"'{InputBranch.Name}' is a local branch. Only remote-tracking branches can be piped to Receive-GitBranch."),
                "ReceiveGitBranch_NotRemote",
                ErrorCategory.InvalidArgument,
                InputBranch));
            return;
        }

        try
        {
            var repoPath = ResolveRepositoryPath(SessionState.Path.CurrentFileSystemLocation.Path);
            var localName = InputBranch.LocalName;
            var branches = branchService.GetBranches(repoPath);
            var localBranch = branches.FirstOrDefault(b => !b.IsRemote && b.Name == localName);
            var localExists = localBranch is not null;

            var shouldCreate = !localExists && Action is ReceiveBranchAction.Create or ReceiveBranchAction.CreateOrUpdate;
            var shouldUpdate = localExists && Action is ReceiveBranchAction.UpdateOnly or ReceiveBranchAction.CreateOrUpdate;

            if (shouldCreate)
            {
                if (!ShouldProcess(localName, $"Create local tracking branch from '{InputBranch.Name}'"))
                {
                    return;
                }

                var result = branchService.CreateBranch(new GitBranchCreateOptions
                {
                    RepositoryPath = repoPath,
                    Name = localName,
                    StartPoint = InputBranch.Name,
                    Track = true,
                });
                WriteObject(result);
            }
            else if (shouldUpdate)
            {
                if (!ShouldProcess(localName, $"Fast-forward local branch '{localName}' to '{InputBranch.Name}'"))
                {
                    return;
                }

                // Fast-forward: advance the local ref without touching the working tree.
                // Returns null when the branch is currently checked out in a worktree.
                var result = branchService.FastForwardBranch(repoPath, localName, InputBranch.TipSha);
                if (result is null)
                {
                    WriteVerbose($"Skipping '{localName}': branch is currently checked out. Use Receive-GitBranch without pipeline input to update the current branch.");
                }
                else
                {
                    WriteObject(result);
                }
            }
            else
            {
                WriteVerbose(localExists
                    ? $"Skipping '{localName}': local branch already exists and Action is '{Action}'."
                    : $"Skipping '{InputBranch.Name}': no local branch '{localName}' and Action is '{Action}'.");
            }
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "ReceiveGitBranchPipelineFailed",
                ErrorCategory.InvalidOperation,
                InputBranch));
        }
    }
}
