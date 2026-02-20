using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Formatting;

/// <summary>
/// Produces ANSI-colored strings for <see cref="GitStatusResult"/> and
/// <see cref="GitStatusEntry"/> display, matching the style of <c>git status --short</c>.
/// </summary>
/// <remarks>
/// Color conventions:
/// <list type="bullet">
///   <item><description>Branch name — bold green</description></item>
///   <item><description>Staged files — green</description></item>
///   <item><description>Modified (unstaged) files — red</description></item>
///   <item><description>Untracked files — red (dim)</description></item>
///   <item><description>Section headers — bold color matching the section</description></item>
/// </list>
/// </remarks>
public static class GitStatusFormatter
{
    /// <summary>
    /// Formats a branch name in bold green.
    /// </summary>
    /// <param name="branchName">The branch name to format.</param>
    /// <returns>An ANSI-colored branch name.</returns>
    public static string FormatBranch(string branchName) =>
        AnsiCodes.Colorize(branchName, AnsiCodes.BoldGreen);

    /// <summary>
    /// Formats a staged-file count in green when non-zero.
    /// </summary>
    /// <param name="count">The staged file count.</param>
    /// <returns>An ANSI-colored count string, or plain "0" when zero.</returns>
    public static string FormatStagedCount(int count) =>
        count > 0 ? AnsiCodes.Colorize(count.ToString(), AnsiCodes.Green) : "0";

    /// <summary>
    /// Formats a modified-file count in red when non-zero.
    /// </summary>
    /// <param name="count">The modified file count.</param>
    /// <returns>An ANSI-colored count string, or plain "0" when zero.</returns>
    public static string FormatModifiedCount(int count) =>
        count > 0 ? AnsiCodes.Colorize(count.ToString(), AnsiCodes.Red) : "0";

    /// <summary>
    /// Formats an untracked-file count in dim red when non-zero.
    /// </summary>
    /// <param name="count">The untracked file count.</param>
    /// <returns>An ANSI-colored count string, or plain "0" when zero.</returns>
    public static string FormatUntrackedCount(int count) =>
        count > 0 ? AnsiCodes.Colorize(count.ToString(), AnsiCodes.Red) : "0";

    /// <summary>
    /// Formats a single status entry with a two-character status indicator and colored path,
    /// matching <c>git status --short</c> style (e.g. <c>M  src/file.cs</c>).
    /// </summary>
    /// <param name="entry">The status entry to format.</param>
    /// <returns>A single-line ANSI-colored status string.</returns>
    public static string FormatEntry(GitStatusEntry entry)
    {
        var indicator = GetStatusIndicator(entry.Status);

        return entry.StagingState switch
        {
            GitStagingState.Staged =>
                $"{AnsiCodes.Green}{indicator}  {entry.FilePath}{AnsiCodes.Reset}",
            _ =>
                $"{AnsiCodes.Red} {indicator} {entry.FilePath}{AnsiCodes.Reset}",
        };
    }

    /// <summary>
    /// Formats only the two-character status indicator portion of a status entry,
    /// suitable for use as a table column value alongside a separate path column.
    /// Staged entries show the indicator in green in the left position (<c>X </c>);
    /// unstaged entries show it in red in the right position (<c> X</c>).
    /// </summary>
    /// <param name="entry">The status entry to format.</param>
    /// <returns>An ANSI-colored two-character status indicator string.</returns>
    public static string FormatEntryStatus(GitStatusEntry entry)
    {
        var indicator = GetStatusIndicator(entry.Status);

        return entry.StagingState switch
        {
            GitStagingState.Staged =>
                $"{AnsiCodes.Green}{indicator} {AnsiCodes.Reset}",
            _ =>
                $"{AnsiCodes.Red} {indicator}{AnsiCodes.Reset}",
        };
    }

    /// <summary>
    /// Formats all status entries grouped by staging state, with colored section headers,
    /// suitable for the detailed (list) view of <see cref="GitStatusResult"/>.
    /// </summary>
    /// <param name="entries">The status entries to format.</param>
    /// <returns>A multi-line ANSI-colored string with section headers and entries.</returns>
    public static string FormatEntries(IReadOnlyList<GitStatusEntry> entries)
    {
        if (entries.Count == 0)
        {
            return AnsiCodes.Colorize("nothing to commit, working tree clean", AnsiCodes.Green);
        }

        var sb = new StringBuilder();

        var staged = entries.Where(e => e.StagingState is GitStagingState.Staged).ToList();
        var unstaged = entries.Where(e =>
            e.StagingState is GitStagingState.Unstaged && e.Status is not GitFileStatus.Untracked).ToList();
        var untracked = entries.Where(e => e.Status is GitFileStatus.Untracked).ToList();

        if (staged.Count > 0)
        {
            sb.AppendLine(AnsiCodes.Colorize("Changes to be committed:", AnsiCodes.BoldGreen));
            foreach (var entry in staged)
            {
                sb.AppendLine($"  {FormatEntry(entry)}");
            }
        }

        if (unstaged.Count > 0)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine(AnsiCodes.Colorize("Changes not staged for commit:", AnsiCodes.BoldRed));
            foreach (var entry in unstaged)
            {
                sb.AppendLine($"  {FormatEntry(entry)}");
            }
        }

        if (untracked.Count > 0)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine(AnsiCodes.Colorize("Untracked files:", AnsiCodes.BoldRed));
            foreach (var entry in untracked)
            {
                sb.AppendLine($"  {FormatEntry(entry)}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Returns a single-character indicator for a <see cref="GitFileStatus"/> value.
    /// </summary>
    private static char GetStatusIndicator(GitFileStatus status)
    {
        return status switch
        {
            GitFileStatus.Added => 'A',
            GitFileStatus.Modified => 'M',
            GitFileStatus.Deleted => 'D',
            GitFileStatus.Renamed => 'R',
            GitFileStatus.Untracked => '?',
            GitFileStatus.Ignored => '!',
            GitFileStatus.Conflicted => 'U',
            _ => ' ',
        };
    }
}
