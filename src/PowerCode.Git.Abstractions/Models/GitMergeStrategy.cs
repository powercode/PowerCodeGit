namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Specifies the merge strategy to use when pulling changes.
/// </summary>
public enum GitMergeStrategy
{
    /// <summary>Standard merge (creates a merge commit if needed).</summary>
    Merge,

    /// <summary>Fast-forward only; fails if not possible.</summary>
    FastForward,

    /// <summary>Rebase local commits on top of the upstream branch.</summary>
    Rebase,
}
