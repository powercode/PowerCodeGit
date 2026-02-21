<#
.SYNOPSIS
    Generates the initial PlatyPS markdown help file for one or more cmdlets.
.DESCRIPTION
    Loads the built PowerCode.Git module and uses Microsoft.PowerShell.PlatyPS
    to create a fresh markdown help file for each specified command under
    docs/help/PowerCode.Git/.

    Only use this script to bootstrap the initial help file for a new cmdlet.
    To update an existing help file after parameter changes, use
    Update-HelpDocs.ps1 instead.

    This script must run in a clean pwsh process (or one where PowerCode.Git
    has not been imported yet) because binary modules lock their assemblies.
.PARAMETER CommandName
    One or more cmdlet names to generate help for (e.g. 'Set-GitTag').
    Tab-completes from the commands exported by the built module.
.PARAMETER Configuration
    The build configuration whose module layout to load. Defaults to 'debug'.
.PARAMETER Force
    Overwrite an existing help file. By default the script refuses to
    overwrite a file that already exists, to prevent accidental data loss.
.EXAMPLE
    .\scripts\New-CommandHelpDoc.ps1 -CommandName Set-GitTag
.EXAMPLE
    .\scripts\New-CommandHelpDoc.ps1 -CommandName Set-GitTag, Get-GitTag -Force
.EXAMPLE
    .\scripts\New-CommandHelpDoc.ps1 -CommandName Get-GitBranch -Configuration Release
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string[]]$CommandName,

    [Parameter()]
    [ValidateSet('debug', 'release')]
    [string]$Configuration = 'debug',

    [Parameter()]
    [switch]$Force
)

# Microsoft.PowerShell.PlatyPS cannot be used with Set-StrictMode v2+ because of its use of dynamic objects
$ErrorActionPreference = 'Stop'

$RepoRoot   = (Resolve-Path -Path "$PSScriptRoot/..").Path
# PlatyPS creates a <ModuleName> subfolder inside the OutputFolder, so point at
# the parent of docs/help/PowerCode.Git.
$HelpRoot   = Join-Path -Path $RepoRoot -ChildPath 'docs/help'
$HelpDocDir = Join-Path -Path $HelpRoot -ChildPath 'PowerCode.Git'

# ------------------------------------------------------------------
# Locate the built module
# ------------------------------------------------------------------
$ModuleLayoutDir = Join-Path -Path $RepoRoot -ChildPath 'artifacts/module/PowerCode.Git'
$VersionedDir    = Get-ChildItem -Path $ModuleLayoutDir -Directory -ErrorAction SilentlyContinue |
    Select-Object -First 1
if (-not $VersionedDir) {
    Write-Error -Message "No versioned module folder found under '$ModuleLayoutDir'. Build the solution first."
    return
}

$ModulePath = Join-Path -Path $VersionedDir.FullName -ChildPath 'PowerCode.Git.psd1'
if (-not (Test-Path -Path $ModulePath)) {
    Write-Error -Message "Module manifest not found at '$ModulePath'."
    return
}

# ------------------------------------------------------------------
# Ensure Microsoft.PowerShell.PlatyPS v2 (1.0.0+) is available
# ------------------------------------------------------------------
$PlatyPSModule = Get-Module -Name Microsoft.PowerShell.PlatyPS -ListAvailable |
    Where-Object { $_.Version -ge [version]'1.0.0' } |
    Select-Object -First 1
if (-not $PlatyPSModule) {
    Write-Host 'Installing Microsoft.PowerShell.PlatyPS...' -ForegroundColor Yellow
    Install-PSResource -Name Microsoft.PowerShell.PlatyPS -MinimumVersion 1.0.0 -TrustRepository -Repository PSGallery
}
Import-Module -Name Microsoft.PowerShell.PlatyPS -Force

# ------------------------------------------------------------------
# Import the module so PlatyPS can inspect its commands
# ------------------------------------------------------------------
Write-Host "Loading module from: $ModulePath" -ForegroundColor DarkGray
Import-Module -Name $ModulePath -Force

if (-not (Test-Path -Path $HelpDocDir)) {
    $null = New-Item -Path $HelpDocDir -ItemType Directory -Force
}

# ------------------------------------------------------------------
# Generate a help file for each requested command
# ------------------------------------------------------------------
foreach ($Name in $CommandName) {
    $Command = Get-Command -Name $Name -ErrorAction SilentlyContinue
    if (-not $Command) {
        Write-Warning "Command '$Name' not found in the loaded module. Skipping."
        continue
    }

    $OutputFile = Join-Path -Path $HelpDocDir -ChildPath "$Name.md"
    if ((Test-Path -Path $OutputFile) -and -not $Force) {
        Write-Warning "Help file already exists: $OutputFile`nUse -Force to overwrite, or run Update-HelpDocs.ps1 to refresh it."
        continue
    }

    if ($PSCmdlet.ShouldProcess($OutputFile, 'Create initial help file')) {
        $CommandHelp = New-CommandHelp -Command $Command
        # Export-MarkdownCommandHelp appends a <ModuleName> subfolder, so pass
        # $HelpRoot so the final path becomes $HelpDocDir\<Name>.md.
        Export-MarkdownCommandHelp -CommandHelp $CommandHelp -OutputFolder $HelpRoot -Force

        Write-Host "Created: $OutputFile" -ForegroundColor Green
    }
}
