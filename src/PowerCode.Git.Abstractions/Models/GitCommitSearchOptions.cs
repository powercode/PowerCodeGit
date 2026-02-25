namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents options for a <c>Search-GitCommit</c> query.
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
    /// Gets or sets the pickaxe content search string. When set, only commits
    /// whose diff (against the first parent) contains this string in the patch
    /// output are candidates. Interpreted as a regex when
    /// <see cref="ContentSearchIsRegex"/> is <see langword="true"/>.
    /// </summary>
    public string? ContentSearch { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ContentSearch"/> is
    /// treated as a regular expression. When <see langword="false"/> (the default),
    /// a plain case-sensitive substring match is performed.
    /// </summary>
    public bool ContentSearchIsRegex { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"RepositoryPath={RepositoryPath}, From={From}, MaxCount={MaxCount}, " +
               $"ContentSearch={ContentSearch}, ContentSearchIsRegex={ContentSearchIsRegex}, " +
               $"Paths=[{string.Join(", ", Paths ?? [])}]";
    }
}
