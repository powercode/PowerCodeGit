namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for setting a git configuration value.
/// </summary>
public sealed class GitConfigSetOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the fully-qualified configuration key (e.g. <c>user.name</c>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the value to assign to the configuration key.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the scope to write the setting into.
    /// When <see langword="null"/>, git's default (local) is used.
    /// </summary>
    public GitConfigScope? Scope { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string> { $"Name={Name}", $"Value={Value}" };
        if (Scope.HasValue) parts.Add($"Scope={Scope.Value}");
        return $"GitConfigSetOptions({string.Join(", ", parts)})";
    }
}
