<#
.SYNOPSIS
    Publishes the PowerCode.Git module to the PowerShell Gallery.
.DESCRIPTION
    Locates the versioned module directory under the given path and publishes
    the module to PSGallery using Publish-PSResource and the provided API key.

    This script is called by the CI publish workflow but can also be run
    interactively to publish a locally built module artifact.
.PARAMETER ModulePath
    Path to the module directory that contains the versioned sub-folder
    (e.g. './module' which contains './module/1.2.3/').
.PARAMETER ApiKey
    The PSGallery API key used for authentication.
.EXAMPLE
    .\scripts\Publish-PowerGitModule.ps1 -ModulePath ./module -ApiKey $ApiKey
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ModulePath,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ApiKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Install-PSResource  -Name:Microsoft.PowerShell.PSResourceGet -TrustRepository -Repository:PSGallery

$VersionedDir = Get-ChildItem -Path $ModulePath -Directory | Select-Object -First 1
if (-not $VersionedDir) {
    Write-Error "No versioned module folder found in '$ModulePath'."
    return
}

$ManifestPath = Join-Path -Path $VersionedDir.FullName -ChildPath 'PowerCode.Git.psd1'
if (-not (Test-Path -Path $ManifestPath)) {
    Write-Error "Module manifest not found at '$ManifestPath'."
    return
}

if ($PSCmdlet.ShouldProcess($VersionedDir.FullName, 'Publish module to PSGallery'))
{
    Write-Verbose "Publishing module from: $($VersionedDir.FullName)"
    Publish-PSResource -Path $VersionedDir.FullName -ApiKey $ApiKey -Repository PSGallery -Verbose
    Write-Verbose 'Module published successfully.'
}
