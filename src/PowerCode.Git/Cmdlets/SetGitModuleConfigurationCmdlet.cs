using System.Management.Automation;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Updates one or more default parameter values in the PowerCode.Git module
/// configuration. Values are kept in-process for the lifetime of the module
/// and are not persisted to disk.
/// </summary>
/// <example>
/// <code>
/// Set-GitModuleConfiguration -LogMaxCount 50
/// </code>
/// </example>
[Cmdlet(VerbsCommon.Set, "GitModuleConfiguration")]
public sealed class SetGitModuleConfigurationCmdlet : PSCmdlet
{
    /// <summary>
    /// Gets or sets the default maximum number of commits returned by
    /// <c>Get-GitLog</c>. Use <c>0</c> or <c>$null</c> to clear the default.
    /// </summary>
    [Parameter]
    [ValidateRange(1, int.MaxValue)]
    [ArgumentCompletions("10", "25", "50", "100", "250", "500", "1000")]
    public int? LogMaxCount { get; set; }

    /// <summary>
    /// Gets or sets the default number of context lines shown by
    /// <c>Get-GitDiff</c>. Use <c>$null</c> to clear the default.
    /// </summary>
    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int? DiffContext { get; set; }

    /// <summary>
    /// Gets or sets the default reference branch used by
    /// <c>Get-GitBranch</c>. Use <c>$null</c> or an empty string to clear.
    /// </summary>
    [Parameter]
    [GitBranchCompleter]
    public string? BranchReferenceBranch { get; set; }

    /// <summary>
    /// When specified, resets all configuration values to their initial defaults
    /// before applying any other parameter values.
    /// </summary>
    [Parameter]
    public SwitchParameter Reset { get; set; }

    /// <summary>
    /// Applies the specified configuration changes.
    /// </summary>
    protected override void ProcessRecord()
    {
        var config = ModuleConfiguration.Current;

        if (Reset.IsPresent)
        {
            config.Reset();
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(LogMaxCount)))
        {
            config.LogMaxCount = LogMaxCount;
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(DiffContext)))
        {
            config.DiffContext = DiffContext;
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(BranchReferenceBranch)))
        {
            config.BranchReferenceBranch = string.IsNullOrEmpty(BranchReferenceBranch) ? null : BranchReferenceBranch;
        }
    }
}
