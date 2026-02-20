using System;
using System.Reflection;
using System.Runtime.Loader;

namespace PowerCode.Git;

/// <summary>
/// Loads PowerCode.Git.Core and its transitive dependencies (including LibGit2Sharp
/// native libraries) in an isolated context. Returns <see langword="null"/> for
/// the shared <c>PowerCode.Git.Abstractions</c> assembly so the runtime falls back to
/// the default context, keeping interface and model types unified across contexts.
/// </summary>
internal sealed class PowerCodeGitDependencyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="PowerCodeGitDependencyLoadContext"/> class.
    /// </summary>
    /// <param name="coreAssemblyPath">
    /// Full path to <c>PowerCode.Git.Core.dll</c> inside the <c>dependencies</c> folder.
    /// The <see cref="AssemblyDependencyResolver"/> uses the accompanying
    /// <c>.deps.json</c> file to locate managed and native dependencies.
    /// </param>
    public PowerCodeGitDependencyLoadContext(string coreAssemblyPath)
        : base(name: "PowerCode.Git.DependencyContext", isCollectible: false)
    {
        resolver = new AssemblyDependencyResolver(coreAssemblyPath);
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // The abstractions assembly must resolve from the default (shared) context
        // so that types like IGitHistoryService are identical on both sides.
        if (string.Equals(assemblyName.Name, "PowerCode.Git.Abstractions", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);

        if (assemblyPath is null)
        {
            return null;
        }

        return LoadFromAssemblyPath(assemblyPath);
    }

    /// <inheritdoc/>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        if (libraryPath is null)
        {
            return IntPtr.Zero;
        }

        return LoadUnmanagedDllFromPath(libraryPath);
    }
}
