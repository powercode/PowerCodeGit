using System.Collections.Generic;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents options for comparing two tree-ish references in a git repository.
/// </summary>
public sealed class GitTreeCompareOptions
{
    /// <summary>
    /// Gets or sets the repository path to query.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base tree-ish reference (branch, tag, or commit SHA).
    /// </summary>
    public string Base { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comparison tree-ish reference (branch, tag, or commit SHA).
    /// </summary>
    public string Compare { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore whitespace changes
    /// (equivalent to <c>git diff -w</c> / <c>--ignore-all-space</c>).
    /// </summary>
    public bool IgnoreWhitespace { get; set; }

    /// <summary>
    /// Gets or sets one or more repository-relative file paths to restrict
    /// the comparison output.
    /// </summary>
    public string[]? Paths { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(Base)) parts.Add($"base={Base}");
        if (!string.IsNullOrEmpty(Compare)) parts.Add($"compare={Compare}");
        if (IgnoreWhitespace) parts.Add("ignoreWhitespace");
        if (Paths is { Length: > 0 }) parts.Add($"paths=[{string.Join(", ", Paths)}]");
        return parts.Count > 0
            ? $"GitTreeCompareOptions({string.Join(", ", parts)})"
            : "GitTreeCompareOptions()";
    }
}
