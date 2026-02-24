using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Creates a new worktree in a git repository.
/// <example>
/// <code>New-GitWorktree feature/my-branch</code>
/// </example>
/// <example>
/// <code>New-GitWorktree feature/my-branch -Name my-wt -Path ../my-worktree</code>
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
    /// Gets or sets the branch or committish to check out in the new worktree.
    /// When supplied as the first positional argument, <see cref="Name"/> and <see cref="Path"/>
    /// are derived automatically: name becomes <c>&lt;branch&gt;.wt</c> and path becomes
    /// <c>..&lt;reponame&gt;-&lt;branchname&gt;</c>.
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = CreateParameterSet)]
    [GitBranchCompleter]
    public string? Branch { get; set; }

    /// <summary>
    /// Gets or sets the name for the new worktree.
    /// Defaults to <c>&lt;branch&gt;.wt</c> when <see cref="Branch"/> is specified and
    /// <see cref="Name"/> is omitted.
    /// </summary>
    [Parameter(ParameterSetName = CreateParameterSet)]
    [ValidateNotNullOrEmpty]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the filesystem path where the worktree will be created.
    /// Defaults to <c>..&lt;reponame&gt;-&lt;branchname&gt;</c> (sibling of the repo root)
    /// when <see cref="Branch"/> is specified and <see cref="Path"/> is omitted.
    /// </summary>
    [Parameter(ParameterSetName = CreateParameterSet)]
    [Parameter(Position = 0, ParameterSetName = PipelineParameterSet)]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

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

        var branchInfo = options.Branch is not null ? $" from branch '{options.Branch}'" : string.Empty;
        if (!ShouldProcess(options.Path, $"Create worktree '{options.Name}'{branchInfo}"))
        {
            return;
        }

        try
        {
            var result = worktreeService.AddWorktree(options);
            WriteObject(result);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
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
        // Resolve common inputs once and capture them in a local so the switch arms stay concise.
        (string repoPath, string? wtPath) Resolve() => (
            ResolveRepositoryPath(currentFileSystemPath),
            // Resolve the worktree path to an absolute path so that LibGit2Sharp
            // does not resolve it relative to the repository root.
            Path is not null ? PathResolver?.ResolvePath(Path) ?? Path : null
        );

        return parameterSetName switch
        {
            OptionsParameterSet => Options,
            PipelineParameterSet => BuildPipelineOptions(Resolve()),
            _ => BuildCreateOptions(Resolve()),
        };
    }

    /// <summary>
    /// Builds options for the Pipeline parameter set, deriving the worktree name and default
    /// path from <see cref="InputBranch"/>.
    /// </summary>
    private GitWorktreeAddOptions BuildPipelineOptions((string repoPath, string? wtPath) resolved)
    {
        var branchName = InputBranch.Name;
        var safeBranchName = MakeSafeBranchName(branchName);

        return new GitWorktreeAddOptions
        {
            RepositoryPath = resolved.repoPath,
            Name = safeBranchName + ".wt",
            Path = resolved.wtPath ?? DeriveDefaultWorktreePath(resolved.repoPath, safeBranchName),
            Branch = branchName,
            Locked = Locked.IsPresent,
        };
    }

    /// <summary>
    /// Builds options for the Create parameter set. When <see cref="Branch"/> is set, the
    /// worktree name and path are derived automatically if not explicitly provided, mirroring
    /// the pipeline behaviour so that <c>New-GitWorktree feature/my-branch</c> just works.
    /// </summary>
    private GitWorktreeAddOptions BuildCreateOptions((string repoPath, string? wtPath) resolved)
    {
        if (Branch is not null)
        {
            var safeBranchName = MakeSafeBranchName(Branch);

            return new GitWorktreeAddOptions
            {
                RepositoryPath = resolved.repoPath,
                Name = Name ?? safeBranchName + ".wt",
                Path = resolved.wtPath ?? DeriveDefaultWorktreePath(resolved.repoPath, safeBranchName),
                Branch = Branch,
                Locked = Locked.IsPresent,
            };
        }

        return new GitWorktreeAddOptions
        {
            RepositoryPath = resolved.repoPath,
            Name = Name!,
            Path = resolved.wtPath!,
            Branch = null,
            Locked = Locked.IsPresent,
        };
    }

    /// <summary>
    /// Replaces forward slashes in a branch name with dashes to produce a filesystem-safe name.
    /// </summary>
    private static string MakeSafeBranchName(string branchName) =>
        branchName.Replace('/', '-');

    /// <summary>
    /// Returns the default worktree path: a sibling of the repository root named
    /// <c>&lt;reponame&gt;-&lt;safeBranchName&gt;</c>.
    /// </summary>
    private static string DeriveDefaultWorktreePath(string repositoryPath, string safeBranchName)
    {
        var repoDir = System.IO.Path.GetFileName(repositoryPath.TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar));
        var parentDir = System.IO.Path.GetDirectoryName(repositoryPath) ?? repositoryPath;

        return System.IO.Path.Combine(parentDir, $"{repoDir}-{safeBranchName}");
    }
}
