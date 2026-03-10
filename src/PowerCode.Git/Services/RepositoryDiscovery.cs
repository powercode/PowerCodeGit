using System;
using System.IO;

namespace PowerCode.Git.Services;

/// <summary>
/// Discovers the root of a git repository by walking up the directory tree
/// from a given starting path, looking for a <c>.git</c> directory or file.
/// This is a lightweight equivalent of <c>git rev-parse --show-toplevel</c>
/// that does not require LibGit2Sharp.
/// </summary>
internal static class RepositoryDiscovery
{
    /// <summary>
    /// Walks up from <paramref name="startPath"/> to find the nearest
    /// directory that contains a <c>.git</c> entry (directory or file).
    /// Returns the discovered repository root, or <paramref name="startPath"/>
    /// unchanged when no <c>.git</c> entry is found.
    /// </summary>
    /// <param name="startPath">
    /// An absolute path to start the search from. May be the repository root,
    /// a subdirectory inside the working tree, or even a file path.
    /// </param>
    /// <returns>
    /// The absolute path of the repository root directory, or
    /// <paramref name="startPath"/> as a fallback.
    /// </returns>
    internal static string ResolveRoot(string startPath)
    {
        // Use span-based directory walking to avoid DirectoryInfo allocations.
        // Path.GetDirectoryName(ReadOnlySpan<char>) returns a slice — zero allocation per level.
        ReadOnlySpan<char> dir = Directory.Exists(startPath)
            ? startPath.AsSpan()
            : Path.GetDirectoryName(startPath.AsSpan());

        while (!dir.IsEmpty)
        {
            // Path.Join allocates a single string for the Exists probe (required on net8.0).
            var gitPath = Path.Join(dir, ".git".AsSpan());

            // .git is a directory for normal repos, a file for worktrees/submodules.
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return dir.ToString();
            }

            dir = Path.GetDirectoryName(dir);
        }

        return startPath;
    }
}
