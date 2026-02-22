namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents the outcome of a <c>git rebase</c> operation.
/// </summary>
public sealed class GitRebaseResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitRebaseResult"/> class.
    /// </summary>
    /// <param name="success">Whether the rebase completed without conflicts.</param>
    /// <param name="hasConflicts">Whether the rebase stopped due to merge conflicts.</param>
    /// <param name="output">The combined stdout output from the git process.</param>
    public GitRebaseResult(bool success, bool hasConflicts, string output)
    {
        Success = success;
        HasConflicts = hasConflicts;
        Output = output;
    }

    /// <summary>
    /// Gets a value indicating whether the rebase completed successfully with no conflicts.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets a value indicating whether the rebase stopped because of merge conflicts
    /// that require manual resolution.
    /// </summary>
    public bool HasConflicts { get; }

    /// <summary>
    /// Gets the raw output from the git process (stdout).
    /// </summary>
    public string Output { get; }

    /// <summary>
    /// Returns a human-readable summary of the result.
    /// </summary>
    public override string ToString()
    {
        if (Success)
        {
            return "Rebase completed successfully.";
        }

        return HasConflicts
            ? "Rebase stopped due to conflicts. Resolve conflicts and run Resume-GitRebase, or run Stop-GitRebase to abort."
            : "Rebase failed.";
    }
}
