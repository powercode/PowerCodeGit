namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents a single hunk from a unified diff, corresponding to one
/// <c>@@ … @@</c> block within a file's patch output.
/// </summary>
public sealed class GitDiffHunk
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitDiffHunk"/> class.
    /// </summary>
    /// <param name="filePath">The repository-relative file path after the change.</param>
    /// <param name="oldPath">The repository-relative file path before the change.</param>
    /// <param name="status">The kind of change applied to the file.</param>
    /// <param name="oldStart">The starting line number in the old file.</param>
    /// <param name="oldLineCount">The number of lines from the old file.</param>
    /// <param name="newStart">The starting line number in the new file.</param>
    /// <param name="newLineCount">The number of lines in the new file.</param>
    /// <param name="header">The raw <c>@@ … @@</c> header line.</param>
    /// <param name="content">The full hunk text including the header and all diff lines.</param>
    /// <param name="linesAdded">The number of added lines in this hunk.</param>
    /// <param name="linesDeleted">The number of deleted lines in this hunk.</param>
    public GitDiffHunk(
        string filePath,
        string oldPath,
        GitFileStatus status,
        int oldStart,
        int oldLineCount,
        int newStart,
        int newLineCount,
        string header,
        string content,
        int linesAdded,
        int linesDeleted)
    {
        FilePath = filePath;
        OldPath = oldPath;
        Status = status;
        OldStart = oldStart;
        OldLineCount = oldLineCount;
        NewStart = newStart;
        NewLineCount = newLineCount;
        Header = header;
        Content = content;
        LinesAdded = linesAdded;
        LinesDeleted = linesDeleted;
    }

    /// <summary>
    /// Gets the repository-relative file path after the change.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the repository-relative file path before the change.
    /// </summary>
    public string OldPath { get; }

    /// <summary>
    /// Gets the kind of change applied to the file.
    /// </summary>
    public GitFileStatus Status { get; }

    /// <summary>
    /// Gets the starting line number in the old file.
    /// </summary>
    public int OldStart { get; }

    /// <summary>
    /// Gets the number of lines from the old file.
    /// </summary>
    public int OldLineCount { get; }

    /// <summary>
    /// Gets the starting line number in the new file.
    /// </summary>
    public int NewStart { get; }

    /// <summary>
    /// Gets the number of lines in the new file.
    /// </summary>
    public int NewLineCount { get; }

    /// <summary>
    /// Gets the raw <c>@@ … @@</c> header line.
    /// </summary>
    public string Header { get; }

    /// <summary>
    /// Gets the full hunk text including the header and all diff lines.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the number of added lines in this hunk.
    /// </summary>
    public int LinesAdded { get; }

    /// <summary>
    /// Gets the number of deleted lines in this hunk.
    /// </summary>
    public int LinesDeleted { get; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Status}: {FilePath} @@ -{OldStart},{OldLineCount} +{NewStart},{NewLineCount} @@";
}
