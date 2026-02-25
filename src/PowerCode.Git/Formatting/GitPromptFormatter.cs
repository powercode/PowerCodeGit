using System.Text;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Formatting;

/// <summary>
/// Produces a Powerline/Nerd-Font–styled prompt string from a <see cref="GitPromptStatus"/> value.
/// </summary>
/// <remarks>
/// <para>
/// The output string uses Unicode glyphs that require a Nerd Font (or compatible Powerline font)
/// to render correctly. Each segment can be suppressed individually via the options on
/// <see cref="Format(GitPromptStatus, bool, bool, bool, bool)"/>.
/// </para>
/// <para>
/// Color conventions:
/// </para>
/// <list type="bullet">
///   <item><description>Provider icon + branch — bold green when clean, bold yellow when dirty</description></item>
///   <item><description>Ahead count (↑) — bold cyan</description></item>
///   <item><description>Behind count (↓) — bold red</description></item>
///   <item><description>Staged count (+) — green</description></item>
///   <item><description>Modified count (~) — red</description></item>
///   <item><description>Untracked count (?) — dim red</description></item>
///   <item><description>Stash count (⚑) — yellow</description></item>
/// </list>
/// <example>
/// <code>
/// // Returns something like: " main ↑1↓2 +3 ~1 ?2 ⚑1"
/// var formatted = GitPromptFormatter.Format(status);
/// </code>
/// </example>
/// </remarks>
public static class GitPromptFormatter
{
    // Nerd Font / Powerline Unicode glyphs for known hosting providers.
    // Requires a Nerd Font (https://nerdfonts.com) installed in the terminal.
    private const string GlyphGit = "\ue702";        // nf-dev-git
    private const string GlyphGitHub = "\uf09b";     // nf-fa-github
    private const string GlyphGitLab = "\uf296";     // nf-fa-gitlab
    private const string GlyphBitbucket = "\uf171";  // nf-fa-bitbucket
    private const string GlyphAzureDevOps = "\uebd8"; // nf-md-microsoft_azure

    // Arrow glyphs for ahead/behind
    private const string ArrowUp = "\u2191";   // ↑
    private const string ArrowDown = "\u2193"; // ↓

    // Stash glyph
    private const string GlyphStash = "\u26d1"; // ⛑ → using flag: ⚑ U+2691
    private const string GlyphStashFlag = "\u2691"; // ⚑

    /// <summary>
    /// Builds the formatted prompt string from <paramref name="status"/>.
    /// </summary>
    /// <param name="status">The git prompt status to format.</param>
    /// <param name="noColor">
    /// When <see langword="true"/>, all ANSI color escape sequences are omitted.
    /// </param>
    /// <param name="hideUpstream">
    /// When <see langword="true"/>, the provider icon and ahead/behind counts are omitted.
    /// </param>
    /// <param name="hideCounts">
    /// When <see langword="true"/>, staged, modified, and untracked file counts are omitted.
    /// </param>
    /// <param name="hideStash">
    /// When <see langword="true"/>, the stash count indicator is omitted.
    /// </param>
    /// <returns>A prompt string ready to be embedded in a shell prompt.</returns>
    public static string Format(
        GitPromptStatus status,
        bool noColor = false,
        bool hideUpstream = false,
        bool hideCounts = false,
        bool hideStash = false)
    {
        var isDirty = status.StagedCount > 0 || status.ModifiedCount > 0 || status.UntrackedCount > 0;
        var branchColor = isDirty ? AnsiCodes.BoldYellow : AnsiCodes.BoldGreen;

        var sb = new StringBuilder();

        // Provider icon
        if (!hideUpstream)
        {
            var icon = GetProviderIcon(status.UpstreamProvider);
            sb.Append(Colorize(icon, branchColor, noColor));
            sb.Append(' ');
        }

        // Branch or SHA
        var branchLabel = status.IsDetachedHead
            ? $"({status.BranchName})"
            : status.BranchName;

        sb.Append(Colorize(branchLabel, branchColor, noColor));

        // Ahead/behind counts
        if (!hideUpstream && status.TrackedBranchName is not null)
        {
            var ahead = status.AheadBy ?? 0;
            var behind = status.BehindBy ?? 0;

            if (ahead > 0 || behind > 0)
            {
                sb.Append(' ');
                if (ahead > 0)
                {
                    sb.Append(Colorize($"{ArrowUp}{ahead}", AnsiCodes.BoldCyan, noColor));
                }

                if (behind > 0)
                {
                    sb.Append(Colorize($"{ArrowDown}{behind}", AnsiCodes.BoldRed, noColor));
                }
            }
        }

        // Working-tree counts
        if (!hideCounts)
        {
            if (status.StagedCount > 0)
            {
                sb.Append(' ');
                sb.Append(Colorize($"+{status.StagedCount}", AnsiCodes.Green, noColor));
            }

            if (status.ModifiedCount > 0)
            {
                sb.Append(' ');
                sb.Append(Colorize($"~{status.ModifiedCount}", AnsiCodes.Red, noColor));
            }

            if (status.UntrackedCount > 0)
            {
                sb.Append(' ');
                sb.Append(Colorize($"?{status.UntrackedCount}", AnsiCodes.Dim, noColor));
            }
        }

        // Stash count
        if (!hideStash && status.StashCount > 0)
        {
            sb.Append(' ');
            sb.Append(Colorize($"{GlyphStashFlag}{status.StashCount}", AnsiCodes.Yellow, noColor));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Detects the upstream hosting provider from a remote URL.
    /// Both HTTPS (e.g. <c>https://github.com/user/repo</c>) and SSH
    /// (e.g. <c>git@github.com:user/repo</c>) URL formats are supported.
    /// </summary>
    /// <param name="remoteUrl">
    /// The fetch or push URL of the remote, or <see langword="null"/> when no remote is configured.
    /// </param>
    /// <returns>The detected <see cref="GitUpstreamProvider"/>.</returns>
    public static GitUpstreamProvider DetectProvider(string? remoteUrl)
    {
        if (string.IsNullOrEmpty(remoteUrl))
        {
            return GitUpstreamProvider.Unknown;
        }

        // Normalise to lowercase for case-insensitive matching.
        var url = remoteUrl.ToLowerInvariant();

        if (url.Contains("github.com"))
        {
            return GitUpstreamProvider.GitHub;
        }

        if (url.Contains("gitlab.com") || url.Contains("gitlab."))
        {
            return GitUpstreamProvider.GitLab;
        }

        if (url.Contains("bitbucket.org") || url.Contains("bitbucket."))
        {
            return GitUpstreamProvider.Bitbucket;
        }

        if (url.Contains("dev.azure.com") || url.Contains("visualstudio.com"))
        {
            return GitUpstreamProvider.AzureDevOps;
        }

        return GitUpstreamProvider.Unknown;
    }

    /// <summary>
    /// Returns the Nerd Font glyph character for the given <paramref name="provider"/>.
    /// </summary>
    /// <param name="provider">The upstream hosting provider.</param>
    /// <returns>A single Nerd Font glyph string.</returns>
    public static string GetProviderIcon(GitUpstreamProvider provider) =>
        provider switch
        {
            GitUpstreamProvider.GitHub => GlyphGitHub,
            GitUpstreamProvider.GitLab => GlyphGitLab,
            GitUpstreamProvider.Bitbucket => GlyphBitbucket,
            GitUpstreamProvider.AzureDevOps => GlyphAzureDevOps,
            _ => GlyphGit,
        };

    private static string Colorize(string text, string colorCode, bool noColor) =>
        noColor ? text : AnsiCodes.Colorize(text, colorCode);
}
