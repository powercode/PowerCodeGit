namespace PowerCodeGit.Abstractions.Models;

/// <summary>
/// Represents information about a git remote.
/// </summary>
public sealed class GitRemoteInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitRemoteInfo"/> class.
    /// </summary>
    /// <param name="name">The remote name (e.g. <c>origin</c>).</param>
    /// <param name="fetchUrl">The fetch URL of the remote.</param>
    /// <param name="pushUrl">The push URL of the remote.</param>
    public GitRemoteInfo(string name, string fetchUrl, string pushUrl)
    {
        Name = name;
        FetchUrl = fetchUrl;
        PushUrl = pushUrl;
    }

    /// <summary>
    /// Gets the remote name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the fetch URL.
    /// </summary>
    public string FetchUrl { get; }

    /// <summary>
    /// Gets the push URL.
    /// </summary>
    public string PushUrl { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({FetchUrl})";
}
