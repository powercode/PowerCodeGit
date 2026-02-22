using System;
using System.Text;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Formatting;

/// <summary>
/// Produces human-readable descriptions for <see cref="GitDiffHunk"/> instances,
/// primarily for <c>ShouldProcess</c> (<c>-WhatIf</c> / <c>-Confirm</c>) output.
/// </summary>
public static class GitDiffHunkFormatter
{
    /// <summary>
    /// Formats a description that includes a content preview of the specified
    /// hunk, showing only changed lines (<c>+</c>/<c>-</c>) so the user can
    /// make an informed decision.
    /// </summary>
    /// <param name="verb">The action verb (e.g. "Stage" or "Restore").</param>
    /// <param name="hunk">The hunk to describe.</param>
    /// <param name="maxPreviewLines">Maximum number of changed lines to show.</param>
    /// <returns>A multi-line description string.</returns>
    public static string FormatDescription(string verb, GitDiffHunk hunk, int maxPreviewLines = 5)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{verb} hunk in {hunk.FilePath} {hunk.Header}");

        var contentLines = hunk.Content.Split('\n');
        var shown = 0;
        var hasMore = false;

        // Skip index 0 — the @@ header line. Only show changed lines (+/-)
        // so the user sees the actual modifications, not surrounding context.
        for (var i = 1; i < contentLines.Length; i++)
        {
            var line = contentLines[i].TrimEnd('\r');

            if (line.Length == 0 || line[0] is not ('+' or '-'))
            {
                continue;
            }

            if (shown >= maxPreviewLines)
            {
                hasMore = true;
                break;
            }

            var display = line.Length > 80 ? line[..80] + "\u2026" : line;
            sb.AppendLine($"  {display}");
            shown++;
        }

        if (hasMore)
        {
            sb.AppendLine("  \u2026");
        }

        return sb.ToString().TrimEnd();
    }
}
