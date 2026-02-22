namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents options for querying files changed by a specific commit.
/// </summary>
public sealed class GitCommitFileOptions
{
    /// <summary>
    /// Gets or sets the repository path to query.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the commit SHA or ref to inspect.
    /// When <see langword="null"/>, defaults to HEAD.
    /// </summary>
    public string? Commit { get; set; }

    /// <summary>
    /// Gets or sets one or more repository-relative file paths to restrict
    /// the output to.
    /// </summary>
    public string[]? Paths { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore whitespace changes
    /// (equivalent to <c>git diff -w</c> / <c>--ignore-all-space</c>).
    /// </summary>
    public bool IgnoreWhitespace { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (Commit is not null) parts.Add($"commit={Commit}");
        if (IgnoreWhitespace) parts.Add("ignoreWhitespace");
        if (Paths is { Length: > 0 }) parts.Add($"paths=[{string.Join(", ", Paths)}]");

        return parts.Count > 0
            ? $"GitCommitFileOptions({string.Join(", ", parts)})"
            : "GitCommitFileOptions()";
    }
}
