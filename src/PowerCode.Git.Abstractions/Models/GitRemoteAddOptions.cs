namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for adding a new remote to a git repository,
/// equivalent to <c>git remote add &lt;name&gt; &lt;url&gt;</c>.
/// </summary>
public sealed class GitRemoteAddOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the new remote (e.g. <c>upstream</c>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the fetch URL for the new remote.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the push URL for the new remote.
    /// When <see langword="null"/>, the push URL defaults to the fetch URL.
    /// </summary>
    public string? PushUrl { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"Name={Name}",
            $"Url={Url}",
        };

        if (PushUrl is not null)
        {
            parts.Add($"PushUrl={PushUrl}");
        }

        return $"GitRemoteAddOptions({string.Join(", ", parts)})";
    }
}
