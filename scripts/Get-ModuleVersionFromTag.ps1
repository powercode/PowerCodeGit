<#
.SYNOPSIS
    Parses a git version tag into module version and prerelease components.
.DESCRIPTION
    Extracts the semantic version and optional prerelease label from a git tag
    in the format 'v<major>.<minor>.<patch>[-<prerelease>]'. The prerelease
    label is sanitised for PSGallery compatibility by removing dots and hyphens.

    When an OutputFile is specified (defaults to $env:GITHUB_OUTPUT) the parsed
    values are appended as key=value lines so downstream CI steps can consume them.
.PARAMETER Tag
    The git tag to parse (e.g. 'v1.2.3' or 'v1.2.3-beta.1').
.PARAMETER OutputFile
    Optional path to a file where 'module-version' and 'prerelease' key=value
    lines are appended. Defaults to $env:GITHUB_OUTPUT. Pass $null or an empty
    string to skip file output.
.OUTPUTS
    PSCustomObject with ModuleVersion and Prerelease properties.
.EXAMPLE
    .\scripts\Get-ModuleVersionFromTag.ps1 -Tag 'v1.2.3'
.EXAMPLE
    .\scripts\Get-ModuleVersionFromTag.ps1 -Tag 'v1.2.3-beta.1'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$Tag,

    [Parameter()]
    [string]$OutputFile = $env:GITHUB_OUTPUT
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Verbose "Tag: $Tag"

# Strip leading 'v'
$VersionString = $Tag -replace '^v', ''

if ($VersionString -notmatch '^(?<version>\d+\.\d+\.\d+)(-(?<prerelease>.+))?$') {
    Write-Error "Tag '$Tag' does not match expected version pattern 'v<major>.<minor>.<patch>[-<prerelease>]'."
    return
}

$ModuleVersion = $Matches['version']
$PrereleaseLabel = $Matches['prerelease']

# PSGallery prerelease strings cannot contain dots or hyphens
if ($PrereleaseLabel) {
    $PrereleaseLabel = $PrereleaseLabel -replace '[.\-]', ''
}

Write-Host "ModuleVersion: $ModuleVersion"
Write-Host "Prerelease:    $PrereleaseLabel"

# Write to output file when specified
if ($OutputFile) {
    "module-version=$ModuleVersion" | Out-File -FilePath $OutputFile -Append
    "prerelease=$PrereleaseLabel" | Out-File -FilePath $OutputFile -Append
}

[PSCustomObject]@{
    ModuleVersion = $ModuleVersion
    Prerelease    = $PrereleaseLabel
}
