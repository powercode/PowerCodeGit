using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents commit data returned from the git history layer.
/// </summary>
public sealed class GitCommitInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitCommitInfo"/> class.
    /// </summary>
    /// <param name="sha">The full commit SHA.</param>
    /// <param name="authorName">The author name.</param>
    /// <param name="authorEmail">The author email.</param>
    /// <param name="authorDate">The author date.</param>
    /// <param name="committerName">The committer name.</param>
    /// <param name="committerEmail">The committer email.</param>
    /// <param name="commitDate">The commit date.</param>
    /// <param name="messageShort">The first line of the commit message.</param>
    /// <param name="message">The full commit message.</param>
    /// <param name="parentShas">The parent commit SHAs.</param>
    /// <param name="decorations">Optional ref-name decorations (branches, tags, HEAD) pointing at this commit.</param>
    public GitCommitInfo(
        string sha,
        string authorName,
        string authorEmail,
        DateTimeOffset authorDate,
        string committerName,
        string committerEmail,
        DateTimeOffset commitDate,
        string messageShort,
        string message,
        IReadOnlyList<string> parentShas,
        IReadOnlyList<GitDecoration>? decorations = null)
    {
        Sha = sha;
        ShortSha = sha.Length > 7 ? sha[..7] : sha;
        AuthorName = authorName;
        AuthorEmail = authorEmail;
        AuthorDate = authorDate;
        CommitterName = committerName;
        CommitterEmail = committerEmail;
        CommitDate = commitDate;
        MessageShort = messageShort;
        Message = message;
        ParentShas = parentShas;
        Decorations = decorations ?? [];
    }

    /// <summary>
    /// Gets the full commit SHA.
    /// </summary>
    public string Sha { get; }

    /// <summary>
    /// Gets the short commit SHA.
    /// </summary>
    public string ShortSha { get; }

    /// <summary>
    /// Gets the author name.
    /// </summary>
    public string AuthorName { get; }

    /// <summary>
    /// Gets the author email.
    /// </summary>
    public string AuthorEmail { get; }

    /// <summary>
    /// Gets the author date.
    /// </summary>
    public DateTimeOffset AuthorDate { get; }

    /// <summary>
    /// Gets the committer name.
    /// </summary>
    public string CommitterName { get; }

    /// <summary>
    /// Gets the committer email.
    /// </summary>
    public string CommitterEmail { get; }

    /// <summary>
    /// Gets the commit date.
    /// </summary>
    public DateTimeOffset CommitDate { get; }

    /// <summary>
    /// Gets the first line of the commit message.
    /// </summary>
    public string MessageShort { get; }

    /// <summary>
    /// Gets the full commit message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the parent commit SHAs.
    /// </summary>
    public IReadOnlyList<string> ParentShas { get; }

    /// <summary>
    /// Gets the ref-name decorations (branches, tags, HEAD) that point at this commit.
    /// </summary>
    public IReadOnlyList<GitDecoration> Decorations { get; }

    /// <summary>
    /// Gets a value indicating whether this commit has any decorations.
    /// </summary>
    public bool HasDecorations => Decorations.Count > 0;

    /// <inheritdoc/>
    public override string ToString()
    {
        if (!HasDecorations)
        {
            return $"{ShortSha} {MessageShort}";
        }

        var refs = string.Join(", ", Decorations.Select(d => d.Name));
        return $"{ShortSha} ({refs}) {MessageShort}";
    }
}
