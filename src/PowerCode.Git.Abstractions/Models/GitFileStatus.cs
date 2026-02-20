namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Describes the kind of change applied to a file.
/// </summary>
public enum GitFileStatus
{
    /// <summary>The file was added.</summary>
    Added,

    /// <summary>The file was modified.</summary>
    Modified,

    /// <summary>The file was deleted.</summary>
    Deleted,

    /// <summary>The file was renamed.</summary>
    Renamed,

    /// <summary>The file is untracked by git.</summary>
    Untracked,

    /// <summary>The file is ignored by git.</summary>
    Ignored,

    /// <summary>The file has merge conflicts.</summary>
    Conflicted,
}
