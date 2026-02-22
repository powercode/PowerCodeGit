using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Lists worktrees in a git repository.
/// <example>
/// <code>Get-GitWorktree</code>
/// </example>
/// <example>
/// <code>Get-GitWorktree -RepoPath C:\repos\myproject</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitWorktree", DefaultParameterSetName = "List")]
[OutputType(typeof(GitWorktreeInfo))]
public sealed class GetGitWorktreeCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitWorktreeCmdlet"/> class.
    /// </summary>
    public GetGitWorktreeCmdlet()
        : this(ServiceFactory.CreateGitWorktreeService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitWorktreeCmdlet"/> class.
    /// </summary>
    /// <param name="worktreeService">The worktree service used by the cmdlet.</param>
    internal GetGitWorktreeCmdlet(IGitWorktreeService worktreeService)
    {
        this.worktreeService = worktreeService ?? throw new ArgumentNullException(nameof(worktreeService));
    }

    private readonly IGitWorktreeService worktreeService;

    /// <summary>
    /// Gets or sets a pre-built <see cref="GitWorktreeListOptions"/> instance.
    /// When specified, all other parameters are ignored.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Options")]
    public GitWorktreeListOptions? Options { get; set; }

    /// <summary>
    /// Builds a <see cref="GitWorktreeListOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitWorktreeListOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == "Options")
        {
            return Options!;
        }

        return new GitWorktreeListOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
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
            var worktrees = worktreeService.GetWorktrees(options);

            foreach (var worktree in worktrees)
            {
                WriteObject(worktree);
            }
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "GetGitWorktreeFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
