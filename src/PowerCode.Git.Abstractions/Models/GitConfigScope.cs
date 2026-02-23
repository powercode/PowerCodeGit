namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Specifies the scope (storage level) of a git configuration setting.
/// </summary>
public enum GitConfigScope
{
    /// <summary>
    /// Repository-level configuration (<c>.git/config</c>).
    /// </summary>
    Local,

    /// <summary>
    /// User-level configuration (<c>~/.gitconfig</c>).
    /// </summary>
    Global,

    /// <summary>
    /// Machine-level configuration (<c>/etc/gitconfig</c>).
    /// </summary>
    System,

    /// <summary>
    /// Worktree-level configuration (<c>.git/config.worktree</c>).
    /// </summary>
    Worktree,
}
