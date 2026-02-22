namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Describes how a single line changed within a <see cref="GitDiffHunk"/>.
/// </summary>
public enum GitDiffLineKind
{
    /// <summary>The line was removed from the old file.</summary>
    Removed,

    /// <summary>The line was added in the new file.</summary>
    Added,

    /// <summary>
    /// The line content changed — it was both removed from the old file and
    /// replaced by a new line at the same logical position.
    /// </summary>
    Modified,
}
