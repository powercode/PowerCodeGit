using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Opens a git repository and executes a ScriptBlock with direct access to the
/// underlying <c>LibGit2Sharp.Repository</c> object.
/// </summary>
/// <remarks>
/// <para>
/// This is the "escape hatch" cmdlet. When no purpose-built cmdlet exists for a
/// task, use <c>Invoke-GitRepository</c> to drop down to the full LibGit2Sharp API
/// without writing C#. The repository is opened before the ScriptBlock executes
/// and disposed after it completes, regardless of errors.
/// </para>
/// <para>
/// Inside the ScriptBlock the repository is available as both <c>$repo</c>
/// (injected variable) and <c>$args[0]</c>. PowerShell's Extended Type System
/// accesses members via reflection, so property and method access such as
/// <c>$repo.Head.Tip.Sha</c> works even though the type comes from an isolated
/// AssemblyLoadContext. However, type literals such as
/// <c>[LibGit2Sharp.Signature]::new(...)</c> will not resolve — the LibGit2Sharp
/// assembly is not loaded in the default context.
/// </para>
/// <para>
/// Objects obtained from <c>$repo</c> are only valid within the ScriptBlock.
/// Do not capture them in variables outside the ScriptBlock.
/// </para>
/// </remarks>
[Cmdlet(VerbsLifecycle.Invoke, "GitRepository")]
[OutputType(typeof(PSObject))]
public sealed class InvokeGitRepositoryCmdlet : GitCmdlet
{
    /// <summary>
    /// Gets or sets the ScriptBlock to execute. The repository is passed as
    /// <c>$args[0]</c> and injected into the ScriptBlock's scope as <c>$repo</c>.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public ScriptBlock Action { get; set; } = null!;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath(SessionState.Path.CurrentFileSystemLocation.Path);
        var repo = default(object);

        try
        {
            repo = ServiceFactory.CreateRepository(repositoryPath);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "InvokeGitRepository_OpenFailed",
                ErrorCategory.OpenError,
                repositoryPath));
            return;
        }

        try
        {
            var variables = new List<PSVariable> { new PSVariable("repo", repo) };
            ICollection<PSObject> results;

            try
            {
                results = Action.InvokeWithContext(null, variables, repo);
            }
            catch (RuntimeException exception)
            {
                WriteError(exception.ErrorRecord);
                return;
            }
            catch (Exception exception) when (exception is not PipelineStoppedException)
            {
                WriteError(new ErrorRecord(
                    exception,
                    "InvokeGitRepository_ScriptBlockFailed",
                    ErrorCategory.InvalidOperation,
                    repositoryPath));
                return;
            }

            foreach (var result in results)
            {
                if (result is not null)
                {
                    // PSCustomObject stores its NoteProperties on the PSObject wrapper itself,
                    // not on BaseObject. Unwrapping via BaseObject would lose those properties,
                    // so PSCustomObject results must be written as-is. For all other types
                    // (strings, ints, LibGit2Sharp objects, etc.) we unwrap so the caller
                    // receives the underlying .NET type rather than a PSObject shell.
                    var baseObject = result.BaseObject;
                    WriteObject(baseObject is PSCustomObject ? result : (baseObject ?? result));
                }
            }
        }
        finally
        {
            ((IDisposable)repo).Dispose();
        }
    }
}
