using System;
using System.Text;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Formatting;

/// <summary>
/// Produces ANSI-colored, whitespace-annotated patch strings for <see cref="GitDiffEntry"/> display.
/// </summary>
/// <remarks>
/// Leading and trailing whitespace on each diff content line is rendered using
/// visible placeholder characters so that accidental whitespace changes are
/// immediately apparent:
/// <list type="bullet">
///   <item><description>Space → <c>·</c> (middle dot, U+00B7)</description></item>
///   <item><description>Tab   → <c>→</c> (rightwards arrow, U+2192)</description></item>
///   <item><description>CR    → <c>←</c> (leftwards arrow, U+2190)</description></item>
/// </list>
/// Handled line types and their colors:
/// <list type="bullet">
///   <item><description><c>+++</c> / <c>---</c> / <c>diff </c> headers — bold</description></item>
///   <item><description>Added lines (<c>+</c>) — green</description></item>
///   <item><description>Removed lines (<c>-</c>) — red</description></item>
///   <item><description>Hunk headers (<c>@@</c>) — cyan</description></item>
///   <item><description>Context lines — no color</description></item>
/// </list>
/// </remarks>
public static class GitDiffFormatter
{
    /// <summary>Middle dot — replaces a space in leading/trailing whitespace positions (U+00B7).</summary>
    internal const char VisibleSpace = '·';

    /// <summary>Rightwards arrow — replaces a tab in leading/trailing whitespace positions (U+2192).</summary>
    internal const char VisibleTab = '→';

    /// <summary>Leftwards arrow — replaces a carriage return in leading/trailing whitespace positions (U+2190).</summary>
    internal const char VisibleCr = '←';

    /// <summary>
    /// Formats a unified diff <paramref name="patch"/> string with ANSI colors and
    /// visible whitespace markers for leading and trailing whitespace on each diff content line.
    /// </summary>
    /// <param name="patch">
    /// The raw unified diff text, as returned by LibGit2Sharp.
    /// May be <see langword="null"/> or empty, in which case an empty string is returned.
    /// </param>
    /// <returns>
    /// A multi-line ANSI-colored string suitable for terminal display.
    /// The trailing newline (if any) is preserved.
    /// </returns>
    /// <example>
    /// <code>
    /// var formatted = GitDiffFormatter.FormatPatch(entry.Patch);
    /// Console.WriteLine(formatted);
    /// </code>
    /// </example>
    public static string FormatPatch(string? patch)
    {
        if (string.IsNullOrEmpty(patch))
        {
            return string.Empty;
        }

        var lines = patch.Split('\n');
        var sb = new StringBuilder(patch.Length + lines.Length * 4);

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];

            // Strip a '\r' that is a split artifact from CRLF line endings in the
            // patch stream itself — not CR content within the diffed file.
            if (raw.Length > 0 && raw[^1] == '\r')
            {
                raw = raw[..^1];
            }

            if (raw.Length == 0)
            {
                if (i < lines.Length - 1)
                {
                    sb.Append('\n');
                }

                continue;
            }

            var color = GetLineColor(raw);
            var annotated = IsContentLine(raw) ? AnnotateContent(raw) : raw;

            if (color is not null)
            {
                sb.Append(color);
                sb.Append(annotated);
                sb.Append(AnsiCodes.Reset);
            }
            else
            {
                sb.Append(annotated);
            }

            if (i < lines.Length - 1)
            {
                sb.Append('\n');
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the ANSI color/style sequence for <paramref name="line"/>,
    /// or <see langword="null"/> for uncolored context lines.
    /// </summary>
    private static string? GetLineColor(string line) =>
        line switch
        {
            _ when line.StartsWith("+++", StringComparison.Ordinal) => AnsiCodes.Bold,
            _ when line.StartsWith("---", StringComparison.Ordinal) => AnsiCodes.Bold,
            _ when line.StartsWith("diff ", StringComparison.Ordinal) => AnsiCodes.Bold,
            _ when line[0] == '+' => AnsiCodes.Green,
            _ when line[0] == '-' => AnsiCodes.Red,
            _ when line.StartsWith("@@", StringComparison.Ordinal) => AnsiCodes.Cyan,
            _ => null,
        };

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="line"/> is a diff content
    /// line (added, removed, or context) that carries actual file content — as
    /// opposed to patch metadata headers (<c>diff</c>, <c>@@</c>, <c>index</c>, etc.).
    /// </summary>
    private static bool IsContentLine(string line) =>
        line.Length > 0 && line[0] is '+' or '-' or ' ';

    /// <summary>
    /// Annotates the content portion of a diff <paramref name="line"/> (everything
    /// after the single-character diff marker) by replacing whitespace characters
    /// at the leading and trailing ends with visible glyphs.
    /// </summary>
    /// <param name="line">A single raw diff line including its prefix character.</param>
    /// <returns>
    /// The line with leading and trailing whitespace in the content part replaced
    /// by <see cref="VisibleSpace"/>, <see cref="VisibleTab"/>, or <see cref="VisibleCr"/>.
    /// </returns>
    internal static string AnnotateContent(string line)
    {
        if (line.Length <= 1)
        {
            return line;
        }

        // Content starts after the single-character diff marker (+, -, or space).
        var content = line.AsSpan(1);

        // Find the extent of leading whitespace in the content.
        var leadEnd = 0;
        while (leadEnd < content.Length && IsAnnotatedWhitespace(content[leadEnd]))
        {
            leadEnd++;
        }

        // Find the start of trailing whitespace in the content.
        var trailStart = content.Length;
        while (trailStart > leadEnd && IsAnnotatedWhitespace(content[trailStart - 1]))
        {
            trailStart--;
        }

        // Fast path: no annotatable whitespace borders.
        if (leadEnd == 0 && trailStart == content.Length)
        {
            return line;
        }

        var sb = new StringBuilder(line.Length);
        sb.Append(line[0]); // diff marker

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            sb.Append((i < leadEnd || i >= trailStart) ? ToVisible(c) : c);
        }

        return sb.ToString();
    }

    private static bool IsAnnotatedWhitespace(char c) => c is ' ' or '\t' or '\r';

    private static char ToVisible(char c) =>
        c switch
        {
            ' ' => VisibleSpace,
            '\t' => VisibleTab,
            '\r' => VisibleCr,
            _ => c,
        };
}
