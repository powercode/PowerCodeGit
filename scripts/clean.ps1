
[CmdletBinding(SupportsShouldProcess = $true)]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$repositoryRoot = Resolve-Path (Join-Path -Path $scriptDirectory -ChildPath "..")
$artifactsPath = Join-Path -Path $repositoryRoot -ChildPath "artifacts"

if (Test-Path -Path $artifactsPath)
{
    if ($PSCmdlet.ShouldProcess($artifactsPath, 'Remove artifacts folder'))
    {
        Write-Verbose "Removing artifacts folder: $artifactsPath"
        Remove-Item -Path $artifactsPath -Recurse -Force
    }
}
else
{
    Write-Verbose "Artifacts folder does not exist: $artifactsPath"
}

Write-Verbose "Cleanup complete."