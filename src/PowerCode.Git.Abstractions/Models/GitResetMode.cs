namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Specifies the reset mode for a git reset operation.
/// </summary>
public enum GitResetMode
{
    /// <summary>Resets the index but not the working tree (default).</summary>
    Mixed,

    /// <summary>Resets only HEAD, leaving index and working tree unchanged.</summary>
    Soft,

    /// <summary>Resets index and working tree. Discards all changes.</summary>
    Hard,
}
