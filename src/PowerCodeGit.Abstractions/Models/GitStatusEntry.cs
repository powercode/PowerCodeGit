namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents a single file entry in a repository status result.
/// </summary>
public sealed class GitStatusEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitStatusEntry"/> class.
    /// </summary>
    /// <param name="filePath">The repository-relative file path.</param>
    /// <param name="status">The kind of change applied to the file.</param>
    /// <param name="stagingState">Whether the change is staged or unstaged.</param>
    public GitStatusEntry(string filePath, GitFileStatus status, GitStagingState stagingState)
    {
        FilePath = filePath;
        Status = status;
        StagingState = stagingState;
    }

    /// <summary>
    /// Gets the repository-relative file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the kind of change applied to the file.
    /// </summary>
    public GitFileStatus Status { get; }

    /// <summary>
    /// Gets whether the change is staged or unstaged.
    /// </summary>
    public GitStagingState StagingState { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{StagingState}: {Status} {FilePath}";
}
