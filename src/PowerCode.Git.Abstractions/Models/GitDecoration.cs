namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// A single ref-name decoration attached to a commit (branch, tag, or HEAD).
/// </summary>
/// <param name="Name">The friendly reference name (e.g. <c>main</c>, <c>origin/main</c>, <c>v1.0</c>).</param>
/// <param name="Type">The kind of reference.</param>
/// <example>
/// <code>
/// var decoration = new GitDecoration("main", GitDecorationType.LocalBranch);
/// </code>
/// </example>
public sealed record GitDecoration(string Name, GitDecorationType Type);
