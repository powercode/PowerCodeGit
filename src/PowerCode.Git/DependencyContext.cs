using System;
using System.IO;
using System.Reflection;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git;

/// <summary>
/// Creates and exposes the isolated dependency load context used by the module.
/// All dependencies of <c>PowerCode.Git.Core</c> (including LibGit2Sharp and its
/// native libraries) are loaded in this context. Types from
/// <c>PowerCode.Git.Abstractions</c> are shared with the default context so no
/// reflection is needed when consuming the services, like <see cref="IGitHistoryService"/>.
/// </summary>
internal static class DependencyContext
{
    private const string DependenciesFolderName = "dependencies";
    private const string CoreAssemblyFileName = "PowerCode.Git.Core.dll";
    // These fully-qualified type name strings cross an Assembly Load Context (ALC) boundary:
    // types are instantiated inside the isolated load context and cast to the shared interface
    // from PowerCode.Git.Abstractions. Using typeof(TService).FullName here would resolve against
    // the wrong ALC identity and produce a cast failure, so string constants are required.
    private const string CoreServiceTypeName = "PowerCode.Git.Core.Services.GitHistoryService";
    private const string WorkingTreeServiceTypeName = "PowerCode.Git.Core.Services.GitWorkingTreeService";
    private const string BranchServiceTypeName = "PowerCode.Git.Core.Services.GitBranchService";
    private const string TagServiceTypeName = "PowerCode.Git.Core.Services.GitTagService";
    private const string PathServiceTypeName = "PowerCode.Git.Core.Services.GitPathService";
    private const string RemoteServiceTypeName = "PowerCode.Git.Core.Services.GitRemoteService";
    private const string WorktreeServiceTypeName = "PowerCode.Git.Core.Services.GitWorktreeService";
    private const string RebaseServiceTypeName = "PowerCode.Git.Core.Services.GitRebaseService";
    private const string CommitFileServiceTypeName = "PowerCode.Git.Core.Services.GitCommitFileService";

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
    /// <c>PowerCode.Git.Abstractions</c> assembly, the returned object can be cast
    /// directly — no reflection needed for subsequent calls.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitHistoryService"/>.</returns>
    public static IGitHistoryService CreateGitHistoryService() => CreateService<IGitHistoryService>();

    /// <summary>
    /// Creates a new <see cref="IGitWorkingTreeService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitWorkingTreeService"/>.</returns>
    public static IGitWorkingTreeService CreateGitWorkingTreeService() => CreateService<IGitWorkingTreeService>();

    /// <summary>
    /// Creates a new <see cref="IGitBranchService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitBranchService"/>.</returns>
    public static IGitBranchService CreateGitBranchService() => CreateService<IGitBranchService>();

    /// <summary>
    /// Creates a new <see cref="IGitTagService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitTagService"/>.</returns>
    public static IGitTagService CreateGitTagService() => CreateService<IGitTagService>();

    /// <summary>
    /// Creates a new <see cref="IGitPathService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitPathService"/>.</returns>
    public static IGitPathService CreateGitPathService() => CreateService<IGitPathService>();

    /// <summary>
    /// Creates a new <see cref="IGitRemoteService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitRemoteService"/>.</returns>
    public static IGitRemoteService CreateGitRemoteService() => CreateService<IGitRemoteService>();

    /// <summary>
    /// Creates a new <see cref="IGitWorktreeService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitWorktreeService"/>.</returns>
    public static IGitWorktreeService CreateGitWorktreeService() => CreateService<IGitWorktreeService>();

    /// <summary>
    /// Creates a new <see cref="IGitRebaseService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitRebaseService"/>.</returns>
    public static IGitRebaseService CreateGitRebaseService() => CreateService<IGitRebaseService>();

    /// <summary>
    /// Creates a new <see cref="IGitCommitFileService"/> instance from the isolated context.
    /// </summary>
    /// <returns>A strongly-typed <see cref="IGitCommitFileService"/>.</returns>
    public static IGitCommitFileService CreateGitCommitFileService() => CreateService<IGitCommitFileService>();

    private static T CreateService<T>()
    {
        EnsureInitialized();
        var typeName = GetServiceTypeName<T>();
        var serviceType = coreAssembly!.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' was not found in the loaded core assembly.");
        var instance = Activator.CreateInstance(serviceType)
            ?? throw new InvalidOperationException($"Failed to create an instance of '{typeName}'.");
        return (T)instance;
    }

    private static string GetServiceTypeName<T>()
    {
        return typeof(T) switch
        {
            var t when t == typeof(IGitHistoryService) => CoreServiceTypeName,
            var t when t == typeof(IGitWorkingTreeService) => WorkingTreeServiceTypeName,
            var t when t == typeof(IGitBranchService) => BranchServiceTypeName,
            var t when t == typeof(IGitTagService) => TagServiceTypeName,
            var t when t == typeof(IGitPathService) => PathServiceTypeName,
            var t when t == typeof(IGitRemoteService) => RemoteServiceTypeName,
            var t when t == typeof(IGitWorktreeService) => WorktreeServiceTypeName,
            var t when t == typeof(IGitRebaseService) => RebaseServiceTypeName,
            var t when t == typeof(IGitCommitFileService) => CommitFileServiceTypeName,
            _ => throw new NotSupportedException($"No mapping for service type '{typeof(T).FullName}'")
        };
    }
}
