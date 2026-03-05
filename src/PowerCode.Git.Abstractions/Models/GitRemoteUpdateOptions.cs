namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for modifying an existing remote in a git repository.
/// Covers <c>git remote set-url</c> (fetch and push URLs) and
/// <c>git remote rename</c> in a single Options object.
/// </summary>
public sealed class GitRemoteUpdateOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the current name of the remote to modify.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the new fetch URL to set on the remote.
    /// When <see langword="null"/>, the fetch URL is unchanged.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Gets the new push URL to set on the remote.
    /// When <see langword="null"/>, the push URL is unchanged.
    /// </summary>
    public string? PushUrl { get; init; }

    /// <summary>
    /// Gets the new name to assign to the remote.
    /// When <see langword="null"/>, the remote is not renamed.
    /// </summary>
    public string? NewName { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string> { $"Name={Name}" };

        if (NewName is not null)
        {
            parts.Add($"NewName={NewName}");
        }

        if (Url is not null)
        {
            parts.Add($"Url={Url}");
        }

        if (PushUrl is not null)
        {
            parts.Add($"PushUrl={PushUrl}");
        }

        return $"GitRemoteUpdateOptions({string.Join(", ", parts)})";
    }
}
