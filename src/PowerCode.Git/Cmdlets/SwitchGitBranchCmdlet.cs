using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Switches the current branch of a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Switch, "GitBranch", SupportsShouldProcess = true, DefaultParameterSetName = "Switch")]
[OutputType(typeof(GitBranchInfo))]
public sealed class SwitchGitBranchCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchGitBranchCmdlet"/> class.
    /// </summary>
    public SwitchGitBranchCmdlet()
        : this(ServiceFactory.CreateGitBranchService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchGitBranchCmdlet"/> class.
    /// </summary>
    /// <param name="branchService">The branch service used by the cmdlet.</param>
    internal SwitchGitBranchCmdlet(IGitBranchService branchService)
    {
        this.branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
    }

    private readonly IGitBranchService branchService;

    // ── Switch parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the name of the branch to switch to.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Switch")]
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Create")]
    [ValidateNotNullOrEmpty]
    [GitBranchCompleter(IncludeRemote = true)]
    public string Name { get; set; } = string.Empty;

    // ── Create parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether to create the branch before switching.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Create")]
    public SwitchParameter Create { get; set; }

    /// <summary>
    /// Gets or sets an optional starting committish for the new branch.
    /// </summary>
    [Parameter(ParameterSetName = "Create")]
    [GitCommittishCompleter]
    public string? StartPoint { get; set; }

    // ── Detach parameter set ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether to detach HEAD at the given committish.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Detach")]
    public SwitchParameter Detach { get; set; }

    /// <summary>
    /// Gets or sets the committish to check out in detached HEAD mode.
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = "Detach")]
    [GitCommittishCompleter]
    public string? Committish { get; set; }

    // ── Shared optional ──────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a value indicating whether to force the checkout, discarding local changes.
    /// </summary>
    [Parameter(ParameterSetName = "Switch")]
    [Parameter(ParameterSetName = "Create")]
    [Parameter(ParameterSetName = "Detach")]
    public SwitchParameter Force { get; set; }

    // ── Options parameter set ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a pre-built options object, allowing full control over the operation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitSwitchOptions Options { get; set; } = null!;

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="GitSwitchOptions"/> from the current parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitSwitchOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == "Options")
        {
            return Options;
        }

        var repositoryPath = ResolveRepositoryPath(currentFileSystemPath);

        return ParameterSetName switch
        {
            "Create" => new GitSwitchOptions
            {
                RepositoryPath = repositoryPath,
                BranchName = Name,
                Create = true,
                StartPoint = StartPoint,
                Force = Force.IsPresent,
            },
            "Detach" => new GitSwitchOptions
            {
                RepositoryPath = repositoryPath,
                Detach = true,
                Committish = Committish,
                Force = Force.IsPresent,
            },
            _ => new GitSwitchOptions
            {
                RepositoryPath = repositoryPath,
                BranchName = Name,
                Force = Force.IsPresent,
            },
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(currentFileSystemPath: string.Empty);
        var description = options.Detach
            ? $"Detach HEAD at '{options.Committish}'"
            : options.Create
                ? $"Create and switch to branch '{options.BranchName}'"
                : $"Switch to branch '{options.BranchName}'";

        if (!ShouldProcess(options.RepositoryPath, description))
        {
            return;
        }

        try
        {
            var result = branchService.SwitchBranch(options);
            WriteObject(result);
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "SwitchGitBranchFailed",
                ErrorCategory.InvalidOperation,
                options.RepositoryPath);

            WriteError(errorRecord);
        }
    }
}
