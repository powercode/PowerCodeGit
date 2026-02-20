using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Formatting;

/// <summary>
/// Produces ANSI-colored strings for <see cref="GitCommitInfo"/> display,
/// matching the style of <c>git log --oneline --decorate</c>.
/// </summary>
/// <remarks>
/// Colors follow standard git conventions:
/// <list type="bullet">
///   <item><description>SHA — yellow</description></item>
///   <item><description>HEAD — bold cyan</description></item>
///   <item><description>Local branch — bold green</description></item>
///   <item><description>Remote branch — bold red</description></item>
///   <item><description>Tag — bold yellow</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var line = GitLogFormatter.FormatOneline(commitInfo);
/// // "abc1234 (HEAD -> main, tag: v1.0) Initial commit"
/// </code>
/// </example>
public static class GitLogFormatter
{
    /// <summary>
    /// Formats a short SHA in yellow, matching <c>git log --oneline</c>.
    /// </summary>
    /// <param name="shortSha">The abbreviated commit hash (typically 7 characters).</param>
    /// <returns>An ANSI-colored short SHA string.</returns>
    public static string FormatShortSha(string shortSha) =>
        AnsiCodes.Colorize(shortSha, AnsiCodes.Yellow);

    /// <summary>
    /// Formats a full SHA in yellow for detailed/list views.
    /// </summary>
    /// <param name="sha">The full 40-character commit hash.</param>
    /// <returns>An ANSI-colored full SHA string.</returns>
    public static string FormatFullSha(string sha) =>
        AnsiCodes.Colorize(sha, AnsiCodes.Yellow);

    /// <summary>
    /// Formats the decoration list (branches, tags, HEAD) for a commit.
    /// Produces output like <c>(HEAD -&gt; main, tag: v1.0, origin/main)</c>.
    /// </summary>
    /// <param name="decorations">The decorations to format. May be empty or <see langword="null"/>.</param>
    /// <returns>
    /// A parenthesized, ANSI-colored decoration string, or an empty string
    /// when there are no decorations.
    /// </returns>
    public static string FormatDecorations(IReadOnlyList<GitDecoration>? decorations)
    {
        if (decorations is null || decorations.Count == 0)
        {
            return string.Empty;
        }

        // Build ordered list: HEAD -> branch first, then remaining branches, then tags.
        var parts = new List<string>();

        string? headBranch = null;
        var hasHead = false;

        foreach (var d in decorations)
        {
            if (d.Type is GitDecorationType.Head)
            {
                hasHead = true;
            }
        }

        if (hasHead)
        {
            // Find the local branch HEAD points to
            foreach (var d in decorations)
            {
                if (d.Type is GitDecorationType.LocalBranch)
                {
                    headBranch = d.Name;
                    break;
                }
            }

            if (headBranch is not null)
            {
                parts.Add(
                    $"{AnsiCodes.BoldCyan}HEAD{AnsiCodes.Reset}" +
                    $"{AnsiCodes.Yellow} -> {AnsiCodes.Reset}" +
                    $"{AnsiCodes.BoldGreen}{headBranch}{AnsiCodes.Reset}");
            }
            else
            {
                // Detached HEAD
                parts.Add(AnsiCodes.Colorize("HEAD", AnsiCodes.BoldCyan));
            }
        }

        // Remaining decorations (skip HEAD and the branch HEAD points to)
        foreach (var decoration in decorations)
        {
            if (decoration.Type is GitDecorationType.Head)
            {
                continue;
            }

            if (decoration.Type is GitDecorationType.LocalBranch && decoration.Name == headBranch)
            {
                continue;
            }

            parts.Add(FormatSingleDecoration(decoration));
        }

        var separator = AnsiCodes.Colorize(", ", AnsiCodes.Yellow);
        var inner = string.Join(separator, parts);

        return $"{AnsiCodes.Colorize("(", AnsiCodes.Yellow)}{inner}{AnsiCodes.Colorize(")", AnsiCodes.Yellow)}";
    }

    /// <summary>
    /// Formats a complete one-line log entry: <c>{sha} {decorations} {message}</c>.
    /// </summary>
    /// <param name="commit">The commit to format.</param>
    /// <returns>A single-line ANSI-colored string.</returns>
    public static string FormatOneline(GitCommitInfo commit)
    {
        var sha = FormatShortSha(commit.ShortSha);
        var decorations = FormatDecorations(commit.Decorations);
        var separator = decorations.Length > 0 ? " " : string.Empty;
        return $"{sha} {decorations}{separator}{commit.MessageShort}";
    }

    private static string FormatSingleDecoration(GitDecoration decoration)
    {
        var colorCode = decoration.Type switch
        {
            GitDecorationType.Head => AnsiCodes.BoldCyan,
            GitDecorationType.LocalBranch => AnsiCodes.BoldGreen,
            GitDecorationType.RemoteBranch => AnsiCodes.BoldRed,
            GitDecorationType.Tag => AnsiCodes.BoldYellow,
            _ => AnsiCodes.Reset,
        };

        return AnsiCodes.Colorize(decoration.Name, colorCode);
    }
}
