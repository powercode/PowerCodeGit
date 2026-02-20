namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents options for a <c>Get-GitStatus</c> query.
/// </summary>
public sealed class GitStatusOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether files matched by <c>.gitignore</c>
    /// should be included in the status results.
    /// When <see langword="false"/> (the default), ignored files are excluded,
    /// matching the behaviour of plain <c>git status</c>.
    /// </summary>
    public bool IncludeIgnored { get; set; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"GitStatusOptions(repositoryPath={RepositoryPath}, includeIgnored={IncludeIgnored})";
}
