namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Describes the nature of the change represented by a <see cref="GitDiffHunk"/>.
/// </summary>
/// <remarks>
/// Classification is computed from the hunk's line-level changes using a
/// length-adaptive similarity heuristic (see <see cref="GitDiffHunk.ChangeKind"/>).
/// </remarks>
public enum GitHunkChangeKind
{
    /// <summary>
    /// The hunk contains only added lines; no lines from the old file were
    /// removed. Typical of new blocks of code or an entirely new file.
    /// </summary>
    Addition,

    /// <summary>
    /// The hunk contains only removed lines; no new lines were added.
    /// Typical of deleted blocks of code or an entirely removed file.
    /// </summary>
    Deletion,

    /// <summary>
    /// The hunk modifies existing content: the majority of changed lines are
    /// remove/add pairs whose content is sufficiently similar to be considered
    /// targeted edits rather than wholesale rewrites.
    /// </summary>
    Change,

    /// <summary>
    /// The hunk cannot be characterised cleanly as a single edit type.
    /// Occurs when modified pairs are too dissimilar (content rewrites), when
    /// there is a large imbalance of unpaired additions or deletions alongside
    /// edits, or when both kinds of changes coexist in the same hunk.
    /// </summary>
    Mixed,
}
