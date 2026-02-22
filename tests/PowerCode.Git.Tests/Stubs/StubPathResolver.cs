using PowerCode.Git.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A no-op <see cref="IPathResolver"/> that returns the input path unchanged.
/// Use this stub in unit tests that need explicit control over path resolution
/// without a live PowerShell engine.
/// </summary>
internal sealed class StubPathResolver : IPathResolver
{
    /// <inheritdoc />
    public string ResolvePath(string psPath) => psPath;
}
