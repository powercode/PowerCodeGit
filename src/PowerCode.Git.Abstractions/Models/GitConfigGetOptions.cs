namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for reading git configuration values.
/// </summary>
public sealed class GitConfigGetOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the configuration key to retrieve.
    /// When <see langword="null"/>, all entries are returned (<c>git config --list</c>).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the scope to read from.
    /// When <see langword="null"/>, git searches all scopes in priority order.
    /// </summary>
    public GitConfigScope? Scope { get; init; }

    /// <summary>
    /// Gets a value indicating whether each entry should include its origin scope
    /// (<c>--show-scope</c>).
    /// </summary>
    public bool ShowScope { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (Name is not null) parts.Add($"Name={Name}");
        if (Scope.HasValue) parts.Add($"Scope={Scope.Value}");
        if (ShowScope) parts.Add("ShowScope");
        return $"GitConfigGetOptions({string.Join(", ", parts)})";
    }
}
