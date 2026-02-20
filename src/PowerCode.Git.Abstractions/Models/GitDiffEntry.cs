namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents a single file change in a diff result.
/// </summary>
public sealed class GitDiffEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitDiffEntry"/> class.
    /// </summary>
    /// <param name="oldPath">The file path before the change.</param>
    /// <param name="newPath">The file path after the change.</param>
    /// <param name="status">The kind of change applied to the file.</param>
    /// <param name="linesAdded">The number of lines added.</param>
    /// <param name="linesDeleted">The number of lines deleted.</param>
    /// <param name="patch">The unified diff patch text, or <see langword="null"/> if unavailable.</param>
    public GitDiffEntry(
        string oldPath,
        string newPath,
        GitFileStatus status,
        int linesAdded,
        int linesDeleted,
        string? patch)
    {
        OldPath = oldPath;
        NewPath = newPath;
        Status = status;
        LinesAdded = linesAdded;
        LinesDeleted = linesDeleted;
        Patch = patch;
    }

    /// <summary>
    /// Gets the file path before the change.
    /// </summary>
    public string OldPath { get; }

    /// <summary>
    /// Gets the file path after the change.
    /// </summary>
    public string NewPath { get; }

    /// <summary>
    /// Gets the kind of change applied to the file.
    /// </summary>
    public GitFileStatus Status { get; }

    /// <summary>
    /// Gets the number of lines added.
    /// </summary>
    public int LinesAdded { get; }

    /// <summary>
    /// Gets the number of lines deleted.
    /// </summary>
    public int LinesDeleted { get; }

    /// <summary>
    /// Gets the unified diff patch text, or <see langword="null"/> if unavailable.
    /// </summary>
    public string? Patch { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{Status}: {NewPath} (+{LinesAdded} -{LinesDeleted})";
}
