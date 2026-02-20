<#
.SYNOPSIS
    Patches the PowerGit module manifest with the specified version.
.DESCRIPTION
    Updates the module manifest inside the given module directory with the
    provided ModuleVersion and optional Prerelease label. After patching, it
    validates the manifest and renames the versioned directory to match the
    final module version when the names differ.

    This script is called by the CI publish workflow but can also be run
    interactively after downloading or building the module artifact.
.PARAMETER ModulePath
    Path to the module directory that contains the versioned sub-folder
    (e.g. './module' which contains './module/0.1.0/').
.PARAMETER ModuleVersion
    The three-part version string to set (e.g. '1.2.3').
.PARAMETER Prerelease
    Optional prerelease label (e.g. 'beta1'). Must already be sanitised
    for PSGallery (no dots or hyphens).
.EXAMPLE
    .\scripts\Update-PowerGitManifest.ps1 -ModulePath ./module -ModuleVersion '1.2.3'
.EXAMPLE
    .\scripts\Update-PowerGitManifest.ps1 -ModulePath ./module -ModuleVersion '1.2.3' -Prerelease 'beta1'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ModulePath,

    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$ModuleVersion,

    [Parameter()]
    [string]$Prerelease
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Find the versioned module directory
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

Write-Host "Updating manifest at: $ManifestPath"
Write-Host "  ModuleVersion: $ModuleVersion"
Write-Host "  Prerelease:    $Prerelease"

# Directly patch the psd1 content instead of using Update-ModuleManifest,
# which enforces that the folder name matches the version and fails when
# the directory has not been renamed yet.
$Content = Get-Content -Path $ManifestPath -Raw

# Replace the ModuleVersion value
$Content = $Content -replace "(?<=ModuleVersion\s*=\s*')[^']*", $ModuleVersion

if ($Prerelease) {
    # Inject a PrivateData / PSData / Prerelease block if one does not exist
    if ($Content -match 'Prerelease\s*=') {
        $Content = $Content -replace "(?<=Prerelease\s*=\s*')[^']*", $Prerelease
    }
    else {
        # Insert a PrivateData block before the closing brace of the root hashtable
        $PsdataBlock = @"

    PrivateData = @{
        PSData = @{
            Prerelease = '$Prerelease'
        }
    }
"@
        $LastBrace = $Content.LastIndexOf('}')
        $Content = $Content.Substring(0, $LastBrace) + $PsdataBlock + [System.Environment]::NewLine + '}'
    }
}

Set-Content -Path $ManifestPath -Value $Content -NoNewline
Write-Host 'Manifest updated successfully.'

# Rename directory to match the final module version
if ($VersionedDir.Name -ne $ModuleVersion) {
    $null = Rename-Item -Path $VersionedDir.FullName -NewName $ModuleVersion
    Write-Host "Renamed module directory from '$($VersionedDir.Name)' to '$ModuleVersion'."
    $ManifestPath = Join-Path -Path $ModulePath -ChildPath "$ModuleVersion/PowerGit.psd1"
}

# Validate the manifest after renaming so folder name matches version
$null = Test-ModuleManifest -Path $ManifestPath -ErrorAction Stop
Write-Host 'Manifest validation passed.'
