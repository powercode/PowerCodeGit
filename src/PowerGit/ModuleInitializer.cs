using System.Management.Automation;

namespace PowerGit;

/// <summary>
/// Bootstraps the isolated dependency load context when the module is imported.
/// This ensures LibGit2Sharp and its native libraries are properly loaded before
/// any cmdlet attempts to use them.
/// </summary>
public sealed class ModuleInitializer : IModuleAssemblyInitializer
{
    /// <inheritdoc/>
    public void OnImport()
    {
        DependencyContext.EnsureInitialized();
    }
}
