namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents options for a <c>Select-GitCommit</c> query.
/// </summary>
public sealed class GitCommitSearchOptions
{
    /// <summary>
    /// Gets or sets the repository path to search.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the starting ref (branch name, tag, or SHA) for the commit walk.
    /// When <see langword="null"/>, the walk starts from <c>HEAD</c>.
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of matching commits to return.
    /// When <see langword="null"/>, all matching commits are returned.
    /// </summary>
    public int? MaxCount { get; set; }

    /// <summary>
    /// Gets or sets one or more repository-relative file paths. When set,
    /// only commits that touch at least one of these paths are candidates.
    /// </summary>
    public string[]? Paths { get; set; }

    /// <summary>
    /// Gets or sets a plain-text search string. When set, only commits whose diff
    /// against the first parent contains this substring (case-sensitive, ordinal)
    /// are candidates. This is a simple substring match — no wildcards or regex.
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="Match"/>. Set at most one of the two.
    /// </remarks>
    public string? Contains { get; set; }

    /// <summary>
    /// Gets or sets a .NET regular expression. When set, only commits whose diff
    /// against the first parent contains a match for this pattern are candidates.
    /// Equivalent to <c>git log -G &lt;pattern&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="Contains"/>. Set at most one of the two.
    /// </remarks>
    public string? Match { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"RepositoryPath={RepositoryPath}, From={From}, MaxCount={MaxCount}, " +
               $"Contains={Contains}, Match={Match}, " +
               $"Paths=[{string.Join(", ", Paths ?? [])}]";
    }
}
