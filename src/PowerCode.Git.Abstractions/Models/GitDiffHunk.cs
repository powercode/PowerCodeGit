using System;
using System.Collections.Generic;
using System.Text;

namespace PowerCode.Git.Abstractions.Models;

// DamerauLevenshteinDistance lives in the parent namespace.
using PowerCode.Git.Abstractions;

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

    private IReadOnlyList<GitDiffLine>? _lines;
    private GitHunkChangeKind? _changeKind;

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

    /// <summary>
    /// Gets the full unified diff patch text for this hunk, including the
    /// <c>diff --git</c>, <c>---</c>, and <c>+++</c> headers.
    /// This property is calculated from the hunk metadata and content.
    /// </summary>
    /// <example>
    /// <code>
    /// diff --git a/file.txt b/file.txt
    /// --- a/file.txt
    /// +++ b/file.txt
    /// @@ -1,3 +1,4 @@
    ///  context
    /// +added line
    ///  context
    /// </code>
    /// </example>
    public string Patch
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("diff --git a/").Append(OldPath).Append(" b/").Append(FilePath).Append('\n');
            sb.Append("--- ").Append(Status == GitFileStatus.Added ? "/dev/null" : $"a/{OldPath}").Append('\n');
            sb.Append("+++ ").Append(Status == GitFileStatus.Deleted ? "/dev/null" : $"b/{FilePath}").Append('\n');
            sb.Append(Content).Append('\n');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets the parsed changed lines in this hunk, with old/new line numbers and
    /// <see cref="GitDiffLineKind"/> classification. Context lines are excluded.
    /// Adjacent remove/add pairs at the same logical position are emitted as
    /// <see cref="GitDiffLineKind.Modified"/>.
    /// This property is calculated once and cached.
    /// </summary>
    public IReadOnlyList<GitDiffLine> Lines => _lines ??= ParseLines();

    /// <summary>
    /// Gets the kind of change this hunk represents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A hunk with no deleted lines is <see cref="GitHunkChangeKind.Addition"/>;
    /// with no added lines, <see cref="GitHunkChangeKind.Deletion"/>.
    /// </para>
    /// <para>
    /// For hunks with both additions and deletions each remove/add pair is
    /// scored with a length-adaptive Damerau-Levenshtein similarity threshold:
    /// the longer the lines the higher the required similarity for the pair to
    /// count as an edit. When enough pairs pass that threshold the hunk is
    /// <see cref="GitHunkChangeKind.Change"/>; otherwise
    /// <see cref="GitHunkChangeKind.Mixed"/>.
    /// </para>
    /// <para>
    /// This property is computed once from <see cref="Lines"/> and cached.
    /// </para>
    /// </remarks>
    public GitHunkChangeKind ChangeKind => _changeKind ??= ComputeChangeKind();

    /// <summary>
    /// Returns the minimum normalized similarity required for a remove/add line
    /// pair to be classified as an edit rather than a replacement, given the
    /// maximum length of the two lines.
    /// </summary>
    /// <remarks>
    /// Uses a logarithmic curve so that short lines tolerate more relative
    /// difference (a one-character change on a two-character line is clearly
    /// still an edit) while long lines require high similarity (50 changed
    /// characters on a 100-character line is likely a rewrite).
    /// Formula: <c>min(0.8, 0.3 + 0.1 × log₂(max(1, maxLineLength)))</c>.
    /// </remarks>
    private static double EditSimilarityThreshold(int maxLineLength) =>
        Math.Min(0.8, 0.3 + 0.1 * Math.Log(Math.Max(1, maxLineLength), 2));

    /// <summary>
    /// The minimum fraction of total changed lines (added + deleted) that must
    /// be accounted for by high-similarity modified pairs for the hunk to be
    /// classified as <see cref="GitHunkChangeKind.Change"/>.
    /// </summary>
    private const double ChangePairingRatio = 0.5;

    private GitHunkChangeKind ComputeChangeKind()
    {
        if (LinesDeleted == 0)
        {
            return GitHunkChangeKind.Addition;
        }

        if (LinesAdded == 0)
        {
            return GitHunkChangeKind.Deletion;
        }

        // Count pairs whose similarity meets the length-adaptive threshold —
        // these represent genuine edits rather than content replacements.
        var highSimilarityPairs = 0;

        foreach (var line in Lines)
        {
            if (line.Kind == GitDiffLineKind.Modified &&
                line.Similarity is { } sim &&
                line.MaxLineLength is { } len &&
                sim >= EditSimilarityThreshold(len))
            {
                highSimilarityPairs++;
            }
        }

        // Each high-similarity pair accounts for one added and one deleted line,
        // so multiply by 2 when comparing against the total changed-line count.
        var totalChanged = LinesAdded + LinesDeleted;
        var ratio = highSimilarityPairs * 2.0 / totalChanged;

        return ratio >= ChangePairingRatio
            ? GitHunkChangeKind.Change
            : GitHunkChangeKind.Mixed;
    }

    private IReadOnlyList<GitDiffLine> ParseLines()
    {
        var result = new List<GitDiffLine>();
        var rawLines = Content.Split('\n');

        var oldLine = OldStart;
        var newLine = NewStart;

        // Buffer of removed lines waiting to be paired with an add.
        // Each entry is (oldLineNumber, text).
        var removedBuffer = new Queue<(int OldNo, string Text)>();

        // Skip index 0 — the @@ header line.
        for (var i = 1; i < rawLines.Length; i++)
        {
            var raw = rawLines[i].TrimEnd('\r');

            if (raw.Length == 0)
            {
                continue;
            }

            var sigil = raw[0];
            var text = raw.Length > 1 ? raw[1..] : string.Empty;

            switch (sigil)
            {
                case '-':
                    removedBuffer.Enqueue((oldLine, text));
                    oldLine++;
                    break;

                case '+':
                    if (removedBuffer.Count > 0)
                    {
                        var (removedOldNo, removedText) = removedBuffer.Dequeue();
                        // Pair as Modified. Compute similarity so downstream
                        // classification can distinguish an edit from a replacement.
                        var similarity = DamerauLevenshteinDistance.ComputeNormalized(removedText, text);
                        var maxLen = Math.Max(removedText.Length, text.Length);
                        result.Add(new GitDiffLine(removedOldNo, newLine, GitDiffLineKind.Modified, text, similarity, maxLen));
                    }
                    else
                    {
                        result.Add(new GitDiffLine(null, newLine, GitDiffLineKind.Added, text));
                    }
                    newLine++;
                    break;

                default:
                    // Context line — flush any pending removes before advancing.
                    while (removedBuffer.Count > 0)
                    {
                        var (removedOldNo, removedText) = removedBuffer.Dequeue();
                        result.Add(new GitDiffLine(removedOldNo, null, GitDiffLineKind.Removed, removedText));
                    }
                    oldLine++;
                    newLine++;
                    break;
            }
        }

        // Flush any trailing removes that were not paired with an add.
        while (removedBuffer.Count > 0)
        {
            var (removedOldNo, removedText) = removedBuffer.Dequeue();
            result.Add(new GitDiffLine(removedOldNo, null, GitDiffLineKind.Removed, removedText));
        }

        return result;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Status}: {FilePath} @@ -{OldStart},{OldLineCount} +{NewStart},{NewLineCount} @@";
}
