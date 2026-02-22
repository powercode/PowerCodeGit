using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Provides operations for starting, resuming, and aborting a git rebase.
/// </summary>
public interface IGitRebaseService
{
    /// <summary>
    /// Starts a rebase operation (<c>git rebase &lt;upstream&gt;</c>).
    /// </summary>
    /// <param name="options">Options controlling the rebase.</param>
    /// <returns>
    /// A <see cref="GitRebaseResult"/> indicating whether the rebase completed
    /// successfully or stopped due to conflicts.
    /// </returns>
    GitRebaseResult Start(GitRebaseOptions options);

    /// <summary>
    /// Resumes a paused rebase after conflicts have been resolved
    /// (<c>git rebase --continue</c>) or skips the current conflicting commit
    /// (<c>git rebase --skip</c>).
    /// </summary>
    /// <param name="options">Options controlling whether to continue or skip.</param>
    /// <returns>
    /// A <see cref="GitRebaseResult"/> indicating the final state of the rebase.
    /// </returns>
    GitRebaseResult Continue(GitRebaseContinueOptions options);

    /// <summary>
    /// Aborts the current rebase and restores the branch to its original state
    /// (<c>git rebase --abort</c>).
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    void Abort(string repositoryPath);

    /// <summary>
    /// Aborts the current rebase and restores the branch to its original state
    /// (<c>git rebase --abort</c>).
    /// </summary>
    /// <param name="options">Options identifying the repository against which to abort the rebase.</param>
    void Abort(GitStopRebaseOptions options)
        => Abort(options.RepositoryPath);
}
