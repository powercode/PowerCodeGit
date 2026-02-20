using System;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents information about a git tag.
/// </summary>
public sealed class GitTagInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitTagInfo"/> class.
    /// </summary>
    /// <param name="name">The tag name.</param>
    /// <param name="sha">The full SHA of the tagged object.</param>
    /// <param name="isAnnotated">Whether this is an annotated tag.</param>
    /// <param name="taggerName">The tagger name, or <see langword="null"/> for lightweight tags.</param>
    /// <param name="taggerEmail">The tagger email, or <see langword="null"/> for lightweight tags.</param>
    /// <param name="tagDate">The tag date, or <see langword="null"/> for lightweight tags.</param>
    /// <param name="message">The tag message, or <see langword="null"/> for lightweight tags.</param>
    public GitTagInfo(
        string name,
        string sha,
        bool isAnnotated,
        string? taggerName,
        string? taggerEmail,
        DateTimeOffset? tagDate,
        string? message)
    {
        Name = name;
        Sha = sha;
        ShortSha = sha.Length > 7 ? sha[..7] : sha;
        IsAnnotated = isAnnotated;
        TaggerName = taggerName;
        TaggerEmail = taggerEmail;
        TagDate = tagDate;
        Message = message;
    }

    /// <summary>
    /// Gets the tag name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the full SHA of the tagged object.
    /// </summary>
    public string Sha { get; }

    /// <summary>
    /// Gets the short SHA of the tagged object.
    /// </summary>
    public string ShortSha { get; }

    /// <summary>
    /// Gets a value indicating whether this is an annotated tag.
    /// </summary>
    public bool IsAnnotated { get; }

    /// <summary>
    /// Gets the tagger name, or <see langword="null"/> for lightweight tags.
    /// </summary>
    public string? TaggerName { get; }

    /// <summary>
    /// Gets the tagger email, or <see langword="null"/> for lightweight tags.
    /// </summary>
    public string? TaggerEmail { get; }

    /// <summary>
    /// Gets the tag date, or <see langword="null"/> for lightweight tags.
    /// </summary>
    public DateTimeOffset? TagDate { get; }

    /// <summary>
    /// Gets the tag message, or <see langword="null"/> for lightweight tags.
    /// </summary>
    public string? Message { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({ShortSha})";
}
