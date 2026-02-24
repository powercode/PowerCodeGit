namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for unsetting (removing) a git configuration key.
/// </summary>
public sealed class GitConfigUnsetOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the fully-qualified configuration key to remove (e.g. <c>user.name</c>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the scope from which to remove the setting.
    /// When <see langword="null"/>, git's default (local) is used.
    /// </summary>
    public GitConfigScope? Scope { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string> { $"Name={Name}" };
        if (Scope.HasValue) parts.Add($"Scope={Scope.Value}");
        return $"GitConfigUnsetOptions({string.Join(", ", parts)})";
    }
}
