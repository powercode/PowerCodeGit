namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Controls how untracked files are shown in <c>git status</c> output
/// (equivalent to <c>git status -u&lt;mode&gt;</c>).
/// </summary>
public enum GitUntrackedFilesMode
{
    /// <summary>
    /// Show no untracked files (equivalent to <c>-uno</c>).
    /// </summary>
    No,

    /// <summary>
    /// Show untracked files and directories, collapsing sub-directories
    /// (equivalent to <c>-unormal</c>, the default).
    /// </summary>
    Normal,

    /// <summary>
    /// Show all untracked files, including those in sub-directories
    /// (equivalent to <c>-uall</c>).
    /// </summary>
    All,
}
