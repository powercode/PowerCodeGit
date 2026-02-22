using System.Management.Automation;

namespace PowerCode.Git.Services;

/// <summary>
/// Resolves PowerShell provider paths via
/// <see cref="PathIntrinsics.GetUnresolvedProviderPathFromPSPath(string)"/>.
/// This implementation is used at runtime when the cmdlet is invoked
/// inside a live PowerShell pipeline.
/// </summary>
/// <param name="sessionState">
/// The <see cref="SessionState"/> of the executing cmdlet, obtained
/// from <c>PSCmdlet.SessionState</c> during <c>BeginProcessing</c>.
/// </param>
internal sealed class SessionStatePathResolver(SessionState sessionState) : IPathResolver
{
    /// <inheritdoc />
    public string ResolvePath(string psPath) =>
        sessionState.Path.GetUnresolvedProviderPathFromPSPath(psPath);
}
