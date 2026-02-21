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
[Cmdlet(VerbsCommon.Get, "GitWorktree")]
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
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        try
        {
            var repositoryPath = ResolveRepositoryPath(SessionState.Path.CurrentFileSystemLocation.Path);
            var worktrees = worktreeService.GetWorktrees(repositoryPath);

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
