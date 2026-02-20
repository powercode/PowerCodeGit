namespace PowerCodeGit.Abstractions.Models;

/// <summary>
/// Indicates whether a file change is staged (in the index) or unstaged
/// (in the working directory).
/// </summary>
public enum GitStagingState
{
    /// <summary>The change is staged in the index.</summary>
    Staged,

    /// <summary>The change is in the working directory and has not been staged.</summary>
    Unstaged,
}
