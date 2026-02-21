namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents options for a git diff query.
/// </summary>
public sealed class GitDiffOptions
{
    /// <summary>
    /// Gets or sets the repository path to query.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to show staged (index) changes
    /// instead of unstaged (working directory) changes
    /// (equivalent to <c>git diff --staged</c>).
    /// </summary>
    public bool Staged { get; set; }

    /// <summary>
    /// Gets or sets a committish to diff the working tree against
    /// (equivalent to <c>git diff &lt;commit&gt;</c>).
    /// When set, the diff is between the given commit and the working directory.
    /// </summary>
    public string? Commit { get; set; }

    /// <summary>
    /// Gets or sets the starting committish for a range diff
    /// (equivalent to <c>git diff &lt;from&gt; &lt;to&gt;</c>).
    /// Must be used with <see cref="ToCommit"/>.
    /// </summary>
    public string? FromCommit { get; set; }

    /// <summary>
    /// Gets or sets the ending committish for a range diff.
    /// Must be used with <see cref="FromCommit"/>.
    /// </summary>
    public string? ToCommit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore whitespace changes
    /// (equivalent to <c>git diff -w</c> / <c>--ignore-all-space</c>).
    /// </summary>
    public bool IgnoreWhitespace { get; set; }

    /// <summary>
    /// Gets or sets one or more repository-relative file paths to restrict
    /// the diff output.
    /// </summary>
    public string[]? Paths { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (Staged) parts.Add("staged");
        if (Commit is not null) parts.Add($"commit={Commit}");
        if (FromCommit is not null) parts.Add($"from={FromCommit}");
        if (ToCommit is not null) parts.Add($"to={ToCommit}");
        if (IgnoreWhitespace) parts.Add("ignoreWhitespace");
        if (Paths is { Length: > 0 }) parts.Add($"paths=[{string.Join(", ", Paths)}]");
        return parts.Count > 0
            ? $"GitDiffOptions({string.Join(", ", parts)})"
            : "GitDiffOptions()";
    }
}
