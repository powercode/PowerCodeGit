using System;
using System.IO;
using System.Reflection;
using PowerCodeGit.Abstractions.Services;

namespace PowerCodeGit;

/// <summary>
/// Creates and exposes the isolated dependency load context used by the module.
/// All dependencies of <c>PowerCodeGit.Core</c> (including LibGit2Sharp and its
/// native libraries) are loaded in this context. Types from
/// <c>PowerCodeGit.Abstractions</c> are shared with the default context so no
/// reflection is needed when consuming <see cref="IGitHistoryService"/>.
/// </summary>
internal static class DependencyContext
{
    private const string DependenciesFolderName = "dependencies";
    private const string CoreAssemblyFileName = "PowerCodeGit.Core.dll";
    private const string CoreServiceTypeName = "PowerCodeGit.Core.Services.GitHistoryService";
    private const string WorkingTreeServiceTypeName = "PowerCodeGit.Core.Services.GitWorkingTreeService";
    private const string BranchServiceTypeName = "PowerCodeGit.Core.Services.GitBranchService";
    private const string TagServiceTypeName = "PowerCodeGit.Core.Services.GitTagService";
    private const string PathServiceTypeName = "PowerCodeGit.Core.Services.GitPathService";
    private const string RemoteServiceTypeName = "PowerCodeGit.Core.Services.GitRemoteService";

    private static readonly object Gate = new();
    private static PowerCodeGitDependencyLoadContext? loadContext;
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

            loadContext = new PowerCodeGitDependencyLoadContext(coreAssemblyPath);
            coreAssembly = loadContext.LoadFromAssemblyPath(coreAssemblyPath);
        }
    }

    /// <summary>
    /// Creates a new <see cref="IGitHistoryService"/> instance from the isolated
    /// context. Because <c>IGitHistoryService</c> lives in the shared
    /// <c>PowerCodeGit.Abstractions</c> assembly, the returned object can be cast
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

    /// <summary>
    /// Creates a new <see cref="IGitWorkingTreeService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitWorkingTreeService"/>.</returns>
    public static IGitWorkingTreeService CreateGitWorkingTreeService()
    {
        EnsureInitialized();

        var serviceType = coreAssembly!.GetType(WorkingTreeServiceTypeName)
            ?? throw new InvalidOperationException(
                $"Type '{WorkingTreeServiceTypeName}' was not found in the loaded core assembly.");

        var instance = Activator.CreateInstance(serviceType)
            ?? throw new InvalidOperationException(
                $"Failed to create an instance of '{WorkingTreeServiceTypeName}'.");

        return (IGitWorkingTreeService)instance;
    }

    /// <summary>
    /// Creates a new <see cref="IGitBranchService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitBranchService"/>.</returns>
    public static IGitBranchService CreateGitBranchService()
    {
        EnsureInitialized();

        var serviceType = coreAssembly!.GetType(BranchServiceTypeName)
            ?? throw new InvalidOperationException(
                $"Type '{BranchServiceTypeName}' was not found in the loaded core assembly.");

        var instance = Activator.CreateInstance(serviceType)
            ?? throw new InvalidOperationException(
                $"Failed to create an instance of '{BranchServiceTypeName}'.");

        return (IGitBranchService)instance;
    }

    /// <summary>
    /// Creates a new <see cref="IGitTagService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitTagService"/>.</returns>
    public static IGitTagService CreateGitTagService()
    {
        EnsureInitialized();

        var serviceType = coreAssembly!.GetType(TagServiceTypeName)
            ?? throw new InvalidOperationException(
                $"Type '{TagServiceTypeName}' was not found in the loaded core assembly.");

        var instance = Activator.CreateInstance(serviceType)
            ?? throw new InvalidOperationException(
                $"Failed to create an instance of '{TagServiceTypeName}'.");

        return (IGitTagService)instance;
    }

    /// <summary>
    /// Creates a new <see cref="IGitPathService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitPathService"/>.</returns>
    public static IGitPathService CreateGitPathService()
    {
        EnsureInitialized();

        var serviceType = coreAssembly!.GetType(PathServiceTypeName)
            ?? throw new InvalidOperationException(
                $"Type '{PathServiceTypeName}' was not found in the loaded core assembly.");

        var instance = Activator.CreateInstance(serviceType)
            ?? throw new InvalidOperationException(
                $"Failed to create an instance of '{PathServiceTypeName}'.");

        return (IGitPathService)instance;
    }

    /// <summary>
    /// Creates a new <see cref="IGitRemoteService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitRemoteService"/>.</returns>
    public static IGitRemoteService CreateGitRemoteService()
    {
        EnsureInitialized();

        var serviceType = coreAssembly!.GetType(RemoteServiceTypeName)
            ?? throw new InvalidOperationException(
                $"Type '{RemoteServiceTypeName}' was not found in the loaded core assembly.");

        var instance = Activator.CreateInstance(serviceType)
            ?? throw new InvalidOperationException(
                $"Failed to create an instance of '{RemoteServiceTypeName}'.");

        return (IGitRemoteService)instance;
    }
}
