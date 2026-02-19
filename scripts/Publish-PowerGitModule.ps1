<#
.SYNOPSIS
    Publishes the PowerGit module to the PowerShell Gallery.
.DESCRIPTION
    Locates the versioned module directory under the given path and publishes
    the module manifest to PSGallery using the provided API key.

    This script is called by the CI publish workflow but can also be run
    interactively to publish a locally built module artifact.
.PARAMETER ModulePath
    Path to the module directory that contains the versioned sub-folder
    (e.g. './module' which contains './module/1.2.3/').
.PARAMETER NuGetApiKey
    The PSGallery NuGet API key used for authentication.
.EXAMPLE
    .\scripts\Publish-PowerGitModule.ps1 -ModulePath ./module -NuGetApiKey $ApiKey
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ModulePath,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$NuGetApiKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$VersionedDir = Get-ChildItem -Path $ModulePath -Directory | Select-Object -First 1
if (-not $VersionedDir) {
    Write-Error "No versioned module folder found in '$ModulePath'."
    return
}

$ManifestPath = Join-Path -Path $VersionedDir.FullName -ChildPath 'PowerGit.psd1'
if (-not (Test-Path -Path $ManifestPath)) {
    Write-Error "Module manifest not found at '$ManifestPath'."
    return
}

Write-Host "Publishing module from: $($VersionedDir.FullName)"

Publish-Module -Path $ManifestPath -NuGetApiKey $NuGetApiKey -Verbose
Write-Host 'Module published successfully.'
