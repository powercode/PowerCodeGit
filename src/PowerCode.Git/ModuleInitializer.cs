using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;

namespace PowerCode.Git;

/// <summary>
/// Bootstraps the isolated dependency load context when the module is imported
/// and registers PowerShell type accelerators for commonly-needed LibGit2Sharp types.
/// </summary>
/// <remarks>
/// <para>
/// LibGit2Sharp lives in an isolated <see cref="System.Runtime.Loader.AssemblyLoadContext"/>
/// so PowerShell cannot resolve its type literals (e.g. <c>[LibGit2Sharp.Signature]</c>)
/// by default. Type accelerators map short names to <see cref="Type"/> objects — and because
/// a <see cref="Type"/> carries its assembly identity, the accelerator works even when the
/// assembly lives in a different ALC.
/// </para>
/// <para>
/// After registration, users can write <c>[GitSignature]::new(...)</c> in ScriptBlocks
/// (e.g. inside <c>Invoke-GitRepository</c>) without needing to know about the ALC boundary.
/// </para>
/// </remarks>
public sealed class ModuleInitializer : IModuleAssemblyInitializer, IModuleAssemblyCleanup
{
    /// <summary>
    /// Mapping of PowerShell type accelerator names to LibGit2Sharp fully-qualified type names.
    /// </summary>
    private static readonly IReadOnlyList<(string AcceleratorName, string LibGit2SharpTypeName)> TypeAcceleratorMappings =
    [
        ("GitSignature", "LibGit2Sharp.Signature"),
        ("GitIdentity", "LibGit2Sharp.Identity"),
        ("GitObjectId", "LibGit2Sharp.ObjectId"),
        ("GitMergeOptions", "LibGit2Sharp.MergeOptions"),
        ("GitCheckoutOptions", "LibGit2Sharp.CheckoutOptions"),
        ("GitStageOptions", "LibGit2Sharp.StageOptions"),
    ];

    /// <summary>
    /// Tracks successfully registered accelerator names so cleanup can remove exactly
    /// what was added, even if some registrations failed.
    /// </summary>
    private static readonly List<string> RegisteredAccelerators = [];

    /// <summary>
    /// Cached reference to the <c>TypeAccelerators.Add(string, Type)</c> method.
    /// </summary>
    private static MethodInfo? addMethod;

    /// <summary>
    /// Cached reference to the <c>TypeAccelerators.Remove(string)</c> method.
    /// </summary>
    private static MethodInfo? removeMethod;

    /// <inheritdoc/>
    public void OnImport()
    {
        DependencyContext.EnsureInitialized();
        RegisterTypeAccelerators();
    }

    /// <inheritdoc/>
    public void OnRemove(PSModuleInfo module)
    {
        UnregisterTypeAccelerators();
    }

    /// <summary>
    /// Registers type accelerators for LibGit2Sharp types from the isolated ALC.
    /// Failures are silently ignored — the module remains fully functional, users
    /// just cannot use the type-literal shortcuts.
    /// </summary>
    private static void RegisterTypeAccelerators()
    {
        try
        {
            var acceleratorsType = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");

            if (acceleratorsType is null)
            {
                return;
            }

            addMethod = acceleratorsType.GetMethod("Add", [typeof(string), typeof(Type)]);
            removeMethod = acceleratorsType.GetMethod("Remove", [typeof(string)]);

            if (addMethod is null)
            {
                return;
            }

            var libgit2Assembly = DependencyContext.LoadLibGit2SharpAssembly();

            foreach (var (acceleratorName, typeName) in TypeAcceleratorMappings)
            {
                var type = libgit2Assembly.GetType(typeName);

                if (type is null)
                {
                    continue;
                }

                try
                {
                    addMethod.Invoke(null, [acceleratorName, type]);
                    RegisteredAccelerators.Add(acceleratorName);
                }
                catch
                {
                    // Another module may have already registered this name.
                    // Swallow and continue — the module works without accelerators.
                }
            }
        }
        catch
        {
            // TypeAccelerators is an internal API. If the reflection fails on a
            // future PowerShell version, degrade gracefully.
        }
    }

    /// <summary>
    /// Removes all type accelerators that were successfully registered during import.
    /// </summary>
    private static void UnregisterTypeAccelerators()
    {
        if (removeMethod is null)
        {
            return;
        }

        foreach (var name in RegisteredAccelerators)
        {
            try
            {
                removeMethod.Invoke(null, [name]);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        RegisteredAccelerators.Clear();
    }
}
