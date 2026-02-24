namespace PowerCode.Git;

/// <summary>
/// Stores user-configurable default values for PowerCode.Git cmdlet parameters.
/// Cmdlets consult these defaults when the user does not explicitly bind a parameter.
/// </summary>
/// <remarks>
/// <para>
/// Users can change defaults at any time via <c>Set-GitModuleConfiguration</c>.
/// The configuration is held in-process for the lifetime of the module and is
/// not persisted to disk.
/// </para>
/// <example>
/// <code>
/// Set-GitModuleConfiguration -LogMaxCount 50
/// Get-GitLog   # now returns at most 50 commits by default
/// </code>
/// </example>
/// </remarks>
public sealed class ModuleConfiguration
{
    private static readonly ModuleConfiguration instance = new();

    /// <summary>
    /// Gets the current module-wide configuration instance.
    /// </summary>
    public static ModuleConfiguration Current => instance;

    /// <summary>
    /// Gets or sets the default maximum number of commits returned by
    /// <c>Get-GitLog</c> when the <c>-MaxCount</c> parameter is not specified.
    /// When <c>null</c>, no limit is applied.
    /// </summary>
    public int? LogMaxCount { get; set; }

    /// <summary>
    /// Gets or sets the default number of context lines shown by
    /// <c>Get-GitDiff</c> when the <c>-Context</c> parameter is not specified.
    /// When <c>null</c>, the service default (typically 3) is used.
    /// </summary>
    public int? DiffContext { get; set; }

    /// <summary>
    /// Gets or sets the default reference branch used by
    /// <c>Get-GitBranch</c> when the <c>-ReferenceBranch</c> parameter is not specified.
    /// When <c>null</c>, no reference branch is applied.
    /// </summary>
    public string? BranchReferenceBranch { get; set; }

    /// <summary>
    /// Gets or sets the default value for <c>-IncludeDescription</c> on
    /// <c>Get-GitBranch</c>. When <c>true</c>, branch descriptions are
    /// included by default. When <c>null</c>, descriptions are not included
    /// unless the parameter is explicitly specified.
    /// </summary>
    public bool? BranchIncludeDescription { get; set; }

    /// <summary>
    /// Resets all configuration values to their initial defaults.
    /// </summary>
    public void Reset()
    {
        LogMaxCount = null;
        DiffContext = null;
        BranchReferenceBranch = null;
        BranchIncludeDescription = null;
    }
}
