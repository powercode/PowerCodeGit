using System.Management.Automation;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Returns the current PowerCode.Git module configuration.
/// </summary>
/// <example>
/// <code>
/// $config = Get-GitModuleConfiguration
/// $config.LogMaxCount   # current default for Get-GitLog -MaxCount
/// </code>
/// </example>
[Cmdlet(VerbsCommon.Get, "GitModuleConfiguration")]
[OutputType(typeof(ModuleConfiguration))]
public sealed class GetGitModuleConfigurationCmdlet : PSCmdlet
{
    /// <summary>
    /// Writes the current <see cref="ModuleConfiguration"/> to the pipeline.
    /// </summary>
    protected override void ProcessRecord()
    {
        WriteObject(ModuleConfiguration.Current);
    }
}
