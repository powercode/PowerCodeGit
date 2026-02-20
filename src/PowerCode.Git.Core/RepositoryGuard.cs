using System;
using LibGit2Sharp;

namespace PowerCode.Git.Core;

/// <summary>
/// Centralises common repository validation logic used across services.
/// Throws explicit, descriptive exceptions for invalid or missing arguments.
/// </summary>
internal static class RepositoryGuard
{
    /// <summary>
    /// Validates that <paramref name="repositoryPath"/> is a non-empty string
    /// pointing to a valid git repository.
    /// </summary>
    /// <param name="repositoryPath">The path to validate.</param>
    /// <param name="paramName">
    /// The caller-supplied parameter name used in exception messages.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="repositoryPath"/> is null, empty, whitespace,
    /// or does not reference a valid git repository.
    /// </exception>
    public static void ValidateRepositoryPath(string repositoryPath, string paramName)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("RepositoryPath is required.", paramName);
        }

        if (!Repository.IsValid(repositoryPath))
        {
            throw new ArgumentException(
                "RepositoryPath does not reference a valid git repository.", paramName);
        }
    }

    /// <summary>
    /// Validates that <paramref name="options"/> is not null and that its
    /// <c>RepositoryPath</c> property (obtained via <paramref name="getPath"/>)
    /// references a valid git repository.
    /// </summary>
    /// <typeparam name="T">The options type.</typeparam>
    /// <param name="options">The options object to validate.</param>
    /// <param name="getPath">A delegate that extracts the repository path from <paramref name="options"/>.</param>
    /// <param name="paramName">
    /// The caller-supplied parameter name used in exception messages.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the repository path is null, empty, whitespace,
    /// or does not reference a valid git repository.
    /// </exception>
    public static void ValidateOptions<T>(T options, Func<T, string> getPath, string paramName) where T : class
    {
        ArgumentNullException.ThrowIfNull(options, paramName);
        ValidateRepositoryPath(getPath(options), paramName);
    }

    /// <summary>
    /// Validates that <paramref name="value"/> is a non-empty string.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">
    /// The caller-supplied parameter name used in exception messages.
    /// </param>
    /// <param name="message">The exception message.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is null, empty, or whitespace.
    /// </exception>
    public static void ValidateRequiredString(string value, string paramName, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message ?? $"{paramName} is required.", paramName);
        }
    }
}
