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
/// <example>
/// <code>Get-GitBranch -Include main | New-GitWorktree</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.New, "GitWorktree", SupportsShouldProcess = true, DefaultParameterSetName = CreateParameterSet)]
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

    private const string CreateParameterSet = "Create";
    private const string PipelineParameterSet = "Pipeline";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the name for the new worktree.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = CreateParameterSet)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filesystem path where the worktree will be created.
    /// </summary>
    [Parameter(Mandatory = true, Position = 1, ParameterSetName = CreateParameterSet)]
    [Parameter(Position = 0, ParameterSetName = PipelineParameterSet)]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the branch or committish to check out in the new worktree.
    /// When not specified, the current HEAD is used.
    /// </summary>
    [Parameter(ParameterSetName = CreateParameterSet)]
    [GitBranchCompleter]
    public string? Branch { get; set; }

    /// <summary>
    /// Gets or sets the branch info piped from <c>Get-GitBranch</c>.
    /// The worktree name is derived as <c>&lt;branchname&gt;.wt</c> and the branch
    /// is set to the branch name.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PipelineParameterSet)]
    public GitBranchInfo InputBranch { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the worktree should be created in a locked state.
    /// </summary>
    [Parameter(ParameterSetName = CreateParameterSet)]
    [Parameter(ParameterSetName = PipelineParameterSet)]
    public SwitchParameter Locked { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object for full control over worktree creation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
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
        return BuildOptions(currentFileSystemPath, ParameterSetName);
    }

    /// <summary>
    /// Builds a <see cref="GitWorktreeAddOptions"/> from the current cmdlet parameters
    /// using an explicit parameter set name. This overload is intended for unit testing.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <param name="parameterSetName">The name of the active parameter set.</param>
    /// <returns>The resolved options object.</returns>
    internal GitWorktreeAddOptions BuildOptions(string currentFileSystemPath, string parameterSetName)
    {
        if (parameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        // Resolve the worktree path to an absolute path so that LibGit2Sharp
        // does not resolve it relative to the repository root.
        var resolvedPath = Path is not null
            ? PathResolver?.ResolvePath(Path) ?? Path
            : null;

        if (parameterSetName == PipelineParameterSet)
        {
            var repositoryPath = ResolveRepositoryPath(currentFileSystemPath);
            var branchName = InputBranch.Name;
            var safeBranchName = branchName.Replace('/', '-');
            var worktreeName = safeBranchName + ".wt";

            if (resolvedPath is null)
            {
                // Default to ../<reponame>-<branchname> next to the repository root.
                var repoDir = System.IO.Path.GetFileName(repositoryPath.TrimEnd(
                    System.IO.Path.DirectorySeparatorChar,
                    System.IO.Path.AltDirectorySeparatorChar));
                var parentDir = System.IO.Path.GetDirectoryName(repositoryPath) ?? repositoryPath;
                resolvedPath = System.IO.Path.Combine(parentDir, $"{repoDir}-{safeBranchName}");
            }

            return new GitWorktreeAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = worktreeName,
                Path = resolvedPath,
                Branch = branchName,
                Locked = Locked.IsPresent,
            };
        }

        return new GitWorktreeAddOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Path = resolvedPath!,
            Branch = Branch,
            Locked = Locked.IsPresent,
        };
    }
}
