namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents a single git configuration entry (key–value pair with optional scope).
/// </summary>
public sealed class GitConfigEntry
{
    /// <summary>
    /// Gets the fully-qualified configuration key (e.g. <c>user.name</c>, <c>core.autocrlf</c>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the configuration value, or <see langword="null"/> when the key is unset.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Gets the scope from which this entry was read, if known.
    /// </summary>
    public GitConfigScope? Scope { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var scope = Scope.HasValue ? $"[{Scope.Value}] " : string.Empty;
        return $"{scope}{Name}={Value}";
    }
}
