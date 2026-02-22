using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Parses unified diff patch text from a <see cref="GitDiffEntry"/> into
/// individual <see cref="GitDiffHunk"/> objects.
/// </summary>
public static partial class DiffHunkParser
{
    // Matches @@ -oldStart[,oldCount] +newStart[,newCount] @@ [optional header context]
    private static readonly Regex HunkHeaderPattern = HunkPattern();

    /// <summary>
    /// Parses the <see cref="GitDiffEntry.Patch"/> text into individual hunks.
    /// </summary>
    /// <param name="entry">The diff entry whose patch text to parse.</param>
    /// <returns>
    /// A list of hunks extracted from the patch. Returns an empty list when
    /// <see cref="GitDiffEntry.Patch"/> is <see langword="null"/> or empty.
    /// </returns>
    public static IReadOnlyList<GitDiffHunk> Parse(GitDiffEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrEmpty(entry.Patch))
        {
            return [];
        }

        var matches = HunkHeaderPattern.Matches(entry.Patch);

        if (matches.Count == 0)
        {
            return [];
        }

        var hunks = new List<GitDiffHunk>(matches.Count);

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var hunkStart = match.Index;
            var hunkEnd = i + 1 < matches.Count
                ? matches[i + 1].Index
                : entry.Patch.Length;

            var content = entry.Patch[hunkStart..hunkEnd].TrimEnd('\n', '\r');
            var header = match.Value;

            var oldStart = int.Parse(match.Groups[1].Value);
            var oldLineCount = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;
            var newStart = int.Parse(match.Groups[3].Value);
            var newLineCount = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 1;

            var (linesAdded, linesDeleted) = CountChangedLines(content);

            hunks.Add(new GitDiffHunk(
                filePath: entry.NewPath,
                oldPath: entry.OldPath,
                status: entry.Status,
                oldStart: oldStart,
                oldLineCount: oldLineCount,
                newStart: newStart,
                newLineCount: newLineCount,
                header: header,
                content: content,
                linesAdded: linesAdded,
                linesDeleted: linesDeleted));
        }

        return hunks;
    }

    private static (int Added, int Deleted) CountChangedLines(string content)
    {
        var added = 0;
        var deleted = 0;
        var lines = content.Split('\n');

        // Skip the first line (the @@ header itself)
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];

            if (line.Length == 0)
            {
                continue;
            }

            switch (line[0])
            {
                case '+':
                    added++;
                    break;
                case '-':
                    deleted++;
                    break;
            }
        }

        return (added, deleted);
    }

    [GeneratedRegex(@"^@@\s+-(\d+)(?:,(\d+))?\s+\+(\d+)(?:,(\d+))?\s+@@(.*)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex HunkPattern();
}
