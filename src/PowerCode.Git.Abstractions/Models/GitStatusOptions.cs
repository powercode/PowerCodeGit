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

    /// <summary>
    /// Gets or sets an optional array of paths to restrict the status query to
    /// (equivalent to <c>git status -- &lt;pathspec&gt;…</c>).
    /// When <see langword="null"/> or empty, all tracked and untracked paths are included.
    /// </summary>
    public string[]? Paths { get; set; }

    /// <summary>
    /// Gets or sets a value controlling how untracked files are shown
    /// (equivalent to <c>git status -u&lt;mode&gt;</c>).
    /// When <see langword="null"/>, the default LibGit2Sharp behaviour is used
    /// (equivalent to <see cref="GitUntrackedFilesMode.Normal"/>).
    /// </summary>
    public GitUntrackedFilesMode? UntrackedFilesMode { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"repositoryPath={RepositoryPath}",
            $"includeIgnored={IncludeIgnored}",
        };
        if (Paths is { Length: > 0 })
            parts.Add($"paths=[{string.Join(", ", Paths)}]");
        if (UntrackedFilesMode.HasValue)
            parts.Add($"untrackedFiles={UntrackedFilesMode}");
        return $"GitStatusOptions({string.Join(", ", parts)})";
    }
}
