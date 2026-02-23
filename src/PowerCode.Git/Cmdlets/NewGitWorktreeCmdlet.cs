using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Creates a new worktree in a git repository.
/// <example>
/// <code>New-GitWorktree -Name feature -Path ../feature-worktree</code>
/// </example>
/// <example>
/// <code>New-GitWorktree -Name hotfix-wt -Path ../hotfix-worktree -Branch hotfix/p1</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.New, "GitWorktree", SupportsShouldProcess = true, DefaultParameterSetName = "Create")]
[OutputType(typeof(GitWorktreeInfo))]
public sealed class NewGitWorktreeCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewGitWorktreeCmdlet"/> class.
    /// </summary>
    public NewGitWorktreeCmdlet()
        : this(ServiceFactory.CreateGitWorktreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewGitWorktreeCmdlet"/> class.
    /// </summary>
    /// <param name="worktreeService">The worktree service used by the cmdlet.</param>
    internal NewGitWorktreeCmdlet(IGitWorktreeService worktreeService)
    {
        this.worktreeService = worktreeService ?? throw new ArgumentNullException(nameof(worktreeService));
    }

    private readonly IGitWorktreeService worktreeService;

    /// <summary>
    /// Gets or sets the name for the new worktree.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Create")]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filesystem path where the worktree will be created.
    /// </summary>
    [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Create")]
    [ValidateNotNullOrEmpty]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the branch or committish to check out in the new worktree.
    /// When not specified, the current HEAD is used.
    /// </summary>
    [Parameter(ParameterSetName = "Create")]
    [GitBranchCompleter]
    public string? Branch { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the worktree should be created in a locked state.
    /// </summary>
    [Parameter(ParameterSetName = "Create")]
    public SwitchParameter Locked { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object for full control over worktree creation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitWorktreeAddOptions Options { get; set; } = null!;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        if (!ShouldProcess(options.RepositoryPath, $"Create worktree '{options.Name}' at '{options.Path}'"))
        {
            return;
        }

        try
        {
            var result = worktreeService.AddWorktree(options);
            WriteObject(result);
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "NewGitWorktreeFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    /// <summary>
    /// Builds a <see cref="GitWorktreeAddOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitWorktreeAddOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == "Options")
        {
            return Options;
        }

        // Resolve the worktree path to an absolute path so that LibGit2Sharp
        // does not resolve it relative to the repository root.
        var resolvedPath = PathResolver?.ResolvePath(Path) ?? Path;

        return new GitWorktreeAddOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Path = resolvedPath,
            Branch = Branch,
            Locked = Locked.IsPresent,
        };
    }
}
