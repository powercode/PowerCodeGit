using System.Collections.Generic;

namespace PowerGit.Abstractions.Models;

/// <summary>
/// Represents the full working-tree and index status of a git repository.
/// </summary>
public sealed class GitStatusResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitStatusResult"/> class.
    /// </summary>
    /// <param name="repositoryPath">The repository root path.</param>
    /// <param name="currentBranch">The name of the current branch.</param>
    /// <param name="entries">The individual file status entries.</param>
    /// <param name="stagedCount">The number of staged changes.</param>
    /// <param name="modifiedCount">The number of unstaged modifications.</param>
    /// <param name="untrackedCount">The number of untracked files.</param>
    public GitStatusResult(
        string repositoryPath,
        string currentBranch,
        IReadOnlyList<GitStatusEntry> entries,
        int stagedCount,
        int modifiedCount,
        int untrackedCount)
    {
        RepositoryPath = repositoryPath;
        CurrentBranch = currentBranch;
        Entries = entries;
        StagedCount = stagedCount;
        ModifiedCount = modifiedCount;
        UntrackedCount = untrackedCount;
    }

    /// <summary>
    /// Gets the repository root path.
    /// </summary>
    public string RepositoryPath { get; }

    /// <summary>
    /// Gets the name of the current branch.
    /// </summary>
    public string CurrentBranch { get; }

    /// <summary>
    /// Gets the individual file status entries.
    /// </summary>
    public IReadOnlyList<GitStatusEntry> Entries { get; }

    /// <summary>
    /// Gets the number of staged changes.
    /// </summary>
    public int StagedCount { get; }

    /// <summary>
    /// Gets the number of unstaged modifications (excludes untracked files).
    /// </summary>
    public int ModifiedCount { get; }

    /// <summary>
    /// Gets the number of untracked files.
    /// </summary>
    public int UntrackedCount { get; }
}
