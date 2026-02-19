using System;
using System.IO;
using System.Reflection;
using PowerGit.Abstractions.Services;

namespace PowerGit;

/// <summary>
/// Creates and exposes the isolated dependency load context used by the module.
/// All dependencies of <c>PowerGit.Core</c> (including LibGit2Sharp and its
/// native libraries) are loaded in this context. Types from
/// <c>PowerGit.Abstractions</c> are shared with the default context so no
/// reflection is needed when consuming <see cref="IGitHistoryService"/>.
/// </summary>
internal static class DependencyContext
{
    private const string DependenciesFolderName = "dependencies";
    private const string CoreAssemblyFileName = "PowerGit.Core.dll";
    private const string CoreServiceTypeName = "PowerGit.Core.Services.GitHistoryService";

    private static readonly object Gate = new();
    private static PowerGitDependencyLoadContext? loadContext;
    private static Assembly? coreAssembly;

    /// <summary>
    /// Ensures the isolated context has been initialized.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (loadContext is not null)
        {
            return;
        }

        lock (Gate)
        {
            if (loadContext is not null)
            {
                return;
            }

            var moduleDirectory = Path.GetDirectoryName(typeof(DependencyContext).Assembly.Location)
                ?? throw new InvalidOperationException("Unable to determine module directory.");
            var coreAssemblyPath = Path.Combine(moduleDirectory, DependenciesFolderName, CoreAssemblyFileName);

            if (!File.Exists(coreAssemblyPath))
            {
                throw new FileNotFoundException(
                    $"Expected dependency assembly was not found at '{coreAssemblyPath}'.",
                    coreAssemblyPath);
            }

            loadContext = new PowerGitDependencyLoadContext(coreAssemblyPath);
            coreAssembly = loadContext.LoadFromAssemblyPath(coreAssemblyPath);
        }
    }

    /// <summary>
    /// Creates a new <see cref="IGitHistoryService"/> instance from the isolated
    /// context. Because <c>IGitHistoryService</c> lives in the shared
    /// <c>PowerGit.Abstractions</c> assembly, the returned object can be cast
    /// directly — no reflection needed for subsequent calls.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitHistoryService"/>.</returns>
    public static IGitHistoryService CreateGitHistoryService()
    {
        EnsureInitialized();

        var serviceType = coreAssembly!.GetType(CoreServiceTypeName)
            ?? throw new InvalidOperationException(
                $"Type '{CoreServiceTypeName}' was not found in the loaded core assembly.");

        var instance = Activator.CreateInstance(serviceType)
            ?? throw new InvalidOperationException(
                $"Failed to create an instance of '{CoreServiceTypeName}'.");

        return (IGitHistoryService)instance;
    }
}
