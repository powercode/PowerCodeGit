using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Formatting;

/// <summary>
/// Produces ANSI-colored strings for <see cref="GitBranchInfo"/> display,
/// matching the style of <c>git branch -vv</c>.
/// </summary>
/// <remarks>
/// Color conventions:
/// <list type="bullet">
///   <item><description>Current (HEAD) branch — bold green with <c>* </c> prefix</description></item>
///   <item><description>Remote-tracking branch — red</description></item>
///   <item><description>Other local branches — plain text</description></item>
///   <item><description>Ahead/behind counters — cyan</description></item>
/// </list>
/// </remarks>
public static class GitBranchFormatter
{
    /// <summary>
    /// Formats a branch name with a <c>* </c> prefix and color for the HEAD branch,
    /// red for remote branches, or plain for other local branches.
    /// </summary>
    /// <param name="branch">The branch info to format.</param>
    /// <returns>An ANSI-colored branch name string.</returns>
    public static string FormatBranchName(GitBranchInfo branch)
    {
        if (branch.IsHead)
        {
            return AnsiCodes.Colorize($"* {branch.Name}", AnsiCodes.BoldGreen);
        }

        if (branch.IsRemote)
        {
            return $"  {AnsiCodes.Colorize(branch.Name, AnsiCodes.Red)}";
        }

        return $"  {branch.Name}";
    }

    /// <summary>
    /// Formats the ahead/behind tracking information for a branch.
    /// </summary>
    /// <param name="branch">The branch info to format.</param>
    /// <returns>
    /// An ANSI-colored tracking string like <c>[origin/main: ahead 2, behind 1]</c>,
    /// or an empty string when no tracking branch is configured.
    /// </returns>
    public static string FormatTracking(GitBranchInfo branch)
    {
        if (branch.TrackedBranchName is null)
        {
            return string.Empty;
        }

        var ahead = branch.AheadBy ?? 0;
        var behind = branch.BehindBy ?? 0;

        if (ahead == 0 && behind == 0)
        {
            return AnsiCodes.Colorize($"[{branch.TrackedBranchName}]", AnsiCodes.Cyan);
        }

        var parts = new System.Collections.Generic.List<string>();
        if (ahead > 0)
        {
            parts.Add($"ahead {ahead}");
        }

        if (behind > 0)
        {
            parts.Add($"behind {behind}");
        }

        var tracking = string.Join(", ", parts);
        return AnsiCodes.Colorize($"[{branch.TrackedBranchName}: {tracking}]", AnsiCodes.Cyan);
    }

    /// <summary>
    /// Formats the worktree path for a branch.
    /// </summary>
    /// <param name="branch">The branch info to format.</param>
    /// <returns>
    /// A dim-colored worktree path string, or an empty string when the branch
    /// is not checked out in any worktree.
    /// </returns>
    public static string FormatWorktree(GitBranchInfo branch)
    {
        if (branch.WorktreePath is null)
        {
            return string.Empty;
        }

        return AnsiCodes.Colorize(branch.WorktreePath, AnsiCodes.Dim);
    }

    /// <summary>
    /// Formats the ahead/behind comparison against a user-specified reference branch.
    /// </summary>
    /// <param name="branch">The branch info to format.</param>
    /// <returns>
    /// A magenta-colored string like <c>(ahead 1) | (behind 3) origin/master</c>,
    /// or an empty string when no reference comparison was requested.
    /// </returns>
    public static string FormatReferenceComparison(GitBranchInfo branch)
    {
        if (branch.ReferenceComparison is null)
        {
            return string.Empty;
        }

        var c = branch.ReferenceComparison;
        return AnsiCodes.Colorize(
            $"(ahead {c.AheadBy}) | (behind {c.BehindBy}) {c.ReferenceBranchName}",
            AnsiCodes.Magenta);
    }
}
