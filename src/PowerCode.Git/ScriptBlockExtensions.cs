using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Extension methods that convert PowerShell <see cref="ScriptBlock"/> instances into
/// typed .NET delegates suitable for passing across the Assembly Load Context boundary
/// into <c>PowerCode.Git.Core</c> service methods.
/// </summary>
/// <remarks>
/// <para>
/// All ALC-boundary delegates use <see langword="object"/> for parameters that will carry
/// LibGit2Sharp types (e.g. <c>Commit</c>, <c>TreeEntry</c>). PowerShell's Extended Type
/// System (ETS) resolves member access via reflection, so ScriptBlocks can use
/// <c>$args[0].Author.Name</c> even though the runtime type lives in the isolated ALC.
/// </para>
/// <para>
/// The injected variable name (e.g. <c>$commit</c>) is matched to the parameter name
/// of the ScriptBlock for ergonomic usage. The value is always also available as
/// <c>$args[0]</c> for consistency with built-in PowerShell idioms.
/// </para>
/// </remarks>
internal static class ScriptBlockExtensions
{
    /// <summary>
    /// Converts a <see cref="ScriptBlock"/> into a <c>Func&lt;object, bool&gt;</c> predicate.
    /// </summary>
    /// <remarks>
    /// The argument is injected into the ScriptBlock as both <c>$_</c> (<c>$PSItem</c>) and
    /// as the named variable specified by <paramref name="variableName"/>. It is also
    /// available as <c>$args[0]</c>.
    /// Returns <see langword="true"/> when the ScriptBlock output is PowerShell-truthy
    /// (evaluated via <see cref="LanguagePrimitives.IsTrue"/>).
    /// </remarks>
    /// <param name="scriptBlock">The ScriptBlock to wrap. Must not be null.</param>
    /// <param name="variableName">
    /// The variable name injected into the ScriptBlock scope. Defaults to <c>commit</c>.
    /// </param>
    /// <returns>A delegate that invokes the ScriptBlock and coerces the result to bool.</returns>
    public static Func<object, bool> ToPredicate(this ScriptBlock scriptBlock, string variableName = "commit")
    {
        return arg =>
        {
            var variables = new List<PSVariable>
            {
                new(variableName, arg),
                new("_", arg),
            };
            var results = scriptBlock.InvokeWithContext(null, variables, arg);
            return results.Count > 0 && LanguagePrimitives.IsTrue(results.First());
        };
    }

    /// <summary>
    /// Converts a <see cref="ScriptBlock"/> into a <c>Func&lt;object, T?&gt;</c>
    /// that coerces the first output value to <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// When the ScriptBlock produces no output or the first value is <see langword="null"/>,
    /// the delegate returns the default value of <typeparamref name="T"/> (<see langword="null"/>
    /// for reference types). The coercion uses <see cref="LanguagePrimitives.ConvertTo{T}"/>.
    /// </remarks>
    /// <typeparam name="T">The target type for the coercion.</typeparam>
    /// <param name="scriptBlock">The ScriptBlock to wrap. Must not be null.</param>
    /// <param name="variableName">
    /// The variable name injected into the ScriptBlock scope. Defaults to <c>commit</c>.
    /// </param>
    /// <returns>A delegate that invokes the ScriptBlock and returns the first result as <typeparamref name="T"/>.</returns>
    public static Func<object, T?> ToTypedFunc<T>(this ScriptBlock scriptBlock, string variableName = "commit")
    {
        return arg =>
        {
            var variables = new List<PSVariable>
            {
                new(variableName, arg),
                new("_", arg),
            };
            var results = scriptBlock.InvokeWithContext(null, variables, arg);
            if (results.Count == 0)
            {
                return default;
            }

            var firstValue = results.First();
            if (firstValue is null)
            {
                return default;
            }

            return LanguagePrimitives.ConvertTo<T>(firstValue);
        };
    }

    /// <summary>
    /// Converts a <see cref="ScriptBlock"/> into a <c>Func&lt;object, PSObject?&gt;</c>
    /// that returns the first output value as a <see cref="PSObject"/>, or
    /// <see langword="null"/> when the ScriptBlock produces no output or returns $null.
    /// </summary>
    /// <remarks>
    /// This overload is used when the ScriptBlock output is a structured value
    /// (e.g. a <c>Hashtable</c> with commit-rewrite overrides) that requires
    /// interpretation by the calling layer before crossing the ALC boundary.
    /// </remarks>
    /// <param name="scriptBlock">The ScriptBlock to wrap. Must not be null.</param>
    /// <param name="variableName">
    /// The variable name injected into the ScriptBlock scope. Defaults to <c>commit</c>.
    /// </param>
    /// <returns>A delegate that invokes the ScriptBlock and returns the first result.</returns>
    public static Func<object, PSObject?> ToObjectFunc(this ScriptBlock scriptBlock, string variableName = "commit")
    {
        return arg =>
        {
            var variables = new List<PSVariable>
            {
                new(variableName, arg),
                new("_", arg),
            };
            var results = scriptBlock.InvokeWithContext(null, variables, arg);
            return results.Count == 0 ? null : results.First();
        };
    }

    /// <summary>
    /// Converts a <see cref="ScriptBlock"/> into a delegate suitable for the
    /// LibGit2Sharp <c>TagNameRewriter</c> callback, which receives three arguments:
    /// old tag name, whether the tag is annotated, and the old target identifier.
    /// </summary>
    /// <remarks>
    /// The three arguments are available as <c>$args[0]</c>, <c>$args[1]</c>, and
    /// <c>$args[2]</c> in the ScriptBlock. The result is the new tag name as a string,
    /// or <see langword="null"/> to keep the original name.
    /// </remarks>
    /// <param name="scriptBlock">The ScriptBlock to wrap. Must not be null.</param>
    /// <returns>
    /// A delegate that invokes the ScriptBlock with three string arguments and returns
    /// the result as a nullable string.
    /// </returns>
    public static Func<string, bool, string, string?> ToTagNameRewriter(this ScriptBlock scriptBlock)
    {
        return (oldName, isAnnotated, oldTarget) =>
        {
            var results = scriptBlock.InvokeWithContext(null, null, oldName, isAnnotated, oldTarget);
            if (results.Count == 0 || results.First() is null)
            {
                return null;
            }

            return results.First()?.BaseObject?.ToString();
        };
    }

    /// <summary>
    /// Converts a <see cref="ScriptBlock"/> into a delegates that returns an enumerable
    /// of parent SHA strings, or <see langword="null"/> when the ScriptBlock returns nothing
    /// (meaning "keep the original parents").
    /// </summary>
    /// <remarks>
    /// The ScriptBlock may return:
    /// <list type="bullet">
    ///   <item><description>String values — treated directly as SHA hashes.</description></item>
    ///   <item><description>Objects with a <c>Sha</c> property — the property value is used.</description></item>
    ///   <item><description>Empty or no output — <see langword="null"/> is returned, meaning "unchanged".</description></item>
    /// </list>
    /// </remarks>
    /// <param name="scriptBlock">The ScriptBlock to wrap. Must not be null.</param>
    /// <returns>A delegate that invokes the ScriptBlock and returns parent SHAs as strings.</returns>
    public static Func<object, IEnumerable<string>?> ToParentsRewriter(this ScriptBlock scriptBlock)
    {
        return commit =>
        {
            var variables = new List<PSVariable>
            {
                new("commit", commit),
                new("_", commit),
            };
            var results = scriptBlock.InvokeWithContext(null, variables, commit);
            if (results.Count == 0)
            {
                return null;
            }

            var shas = results
                .Where(r => r is not null)
                .Select(r => r!.BaseObject)
                .Where(o => o is not null)
                .Select(o => o is string s ? s : PSObject.AsPSObject(o).Properties["Sha"]?.Value?.ToString())
                .Where(s => s is not null)
                .Select(s => s!)
                .ToList();

            return shas.Count == 0 ? null : shas;
        };
    }
}
