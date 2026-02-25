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
    private const string ConfigServiceTypeName = "PowerCode.Git.Core.Services.GitConfigService";
    private const string CommitSearchServiceTypeName = "PowerCode.Git.Core.Services.GitCommitSearchService";

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
    /// Creates a <c>LibGit2Sharp.Repository</c> instance loaded from the isolated
    /// AssemblyLoadContext and opens the repository at <paramref name="repositoryPath"/>.
    /// </summary>
    /// <remarks>
    /// This method intentionally breaks the ALC abstraction boundary: the returned
    /// <see cref="object"/> is a <c>LibGit2Sharp.Repository</c> from the isolated
    /// context, not a shared-Abstractions type. It exists exclusively for
    /// <c>Invoke-GitRepository</c>, which is the "escape hatch" cmdlet that exposes
    /// the raw LibGit2Sharp API to PowerShell ScriptBlocks. PowerShell's ETS accesses
    /// members via reflection, which works across ALCs, so <c>$repo.Head.Tip.Sha</c>
    /// and similar expressions work as expected inside the ScriptBlock.
    /// The caller is responsible for disposing the returned object via
    /// <see cref="IDisposable.Dispose"/> (BCL interface, shared across ALCs).
    /// </remarks>
    /// <param name="repositoryPath">
    /// Path to the git repository (must point to a valid git repo).
    /// </param>
    /// <returns>
    /// A <c>LibGit2Sharp.Repository</c> instance as <see cref="object"/>.
    /// Cast to <see cref="IDisposable"/> to dispose.
    /// </returns>
    internal static object CreateRepository(string repositoryPath)
    {
        EnsureInitialized();
        var libgit2Assembly = loadContext!.LoadFromAssemblyName(new AssemblyName("LibGit2Sharp"));
        var repositoryType = libgit2Assembly.GetType("LibGit2Sharp.Repository")
            ?? throw new InvalidOperationException("Could not locate LibGit2Sharp.Repository type in the isolated assembly.");
        return Activator.CreateInstance(repositoryType, new object[] { repositoryPath })
            ?? throw new InvalidOperationException("Failed to create a LibGit2Sharp.Repository instance.");
    }

    internal static T CreateService<T>()
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
            var t when t == typeof(IGitConfigService) => ConfigServiceTypeName,
            var t when t == typeof(IGitCommitSearchService) => CommitSearchServiceTypeName,
            _ => throw new NotSupportedException($"No mapping for service type '{typeof(T).FullName}'")
        };
    }
}
