<#
.SYNOPSIS
    Updates the PlatyPS markdown help files from the built module.
.DESCRIPTION
    Loads the built PowerCode.Git module and uses Microsoft.PowerShell.PlatyPS
    (v2) to create or update the markdown help documentation under
    docs/help/PowerCode.Git/.

    New cmdlets get a fresh markdown file. Existing files are updated with
    any parameter or syntax changes while preserving hand-written descriptions
    and examples.

    This script must run in a clean pwsh process (or one where PowerCode.Git
    has not been imported yet) because binary modules lock their assemblies.
.PARAMETER Configuration
    The build configuration whose module layout to load. Defaults to 'debug'.
.EXAMPLE
    .\scripts\Update-HelpDocs.ps1
.EXAMPLE
    .\scripts\Update-HelpDocs.ps1 -Configuration Release
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('debug', 'release')]
    [string]$Configuration = 'debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot    = (Resolve-Path -Path "$PSScriptRoot/..").Path
# PlatyPS appends a <ModuleName> subfolder, so point at the parent.
$HelpRoot    = Join-Path -Path $RepoRoot -ChildPath 'docs/help'
$HelpDocsPath = Join-Path -Path $HelpRoot -ChildPath 'PowerCode.Git'

# Locate the built module
$ModuleLayoutDir = Join-Path -Path $RepoRoot -ChildPath 'artifacts/module/PowerCode.Git'
$VersionedDir = Get-ChildItem -Path $ModuleLayoutDir -Directory -ErrorAction SilentlyContinue |
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

# Ensure Microsoft.PowerShell.PlatyPS v2 (1.0.0+) is available
$PlatyPSModule = Get-Module -Name Microsoft.PowerShell.PlatyPS -ListAvailable |
    Where-Object { $_.Version -ge [version]'1.0.0' } |
    Select-Object -First 1
if (-not $PlatyPSModule) {
    Write-Host 'Installing Microsoft.PowerShell.PlatyPS...' -ForegroundColor Yellow
    Install-PSResource -Name Microsoft.PowerShell.PlatyPS -MinimumVersion 1.0.0 -TrustRepository -Repository PSGallery
}

Import-Module -Name Microsoft.PowerShell.PlatyPS -Force

# Import the module so PlatyPS can inspect its commands
Write-Host "Loading module from: $ModulePath" -ForegroundColor DarkGray
Import-Module -Name $ModulePath -Force

$ModuleName = 'PowerCode.Git'
$Commands = Get-Command -Module $ModuleName

Write-Host "Found $($Commands.Count) command(s) in $ModuleName." -ForegroundColor Cyan

if (-not (Test-Path -Path $HelpDocsPath)) {
    $null = New-Item -Path $HelpDocsPath -ItemType Directory -Force
}

# Determine which commands already have markdown files
$ExistingFiles = Get-ChildItem -Path $HelpDocsPath -Filter '*.md' -File |
    ForEach-Object { $_.BaseName }

$NewCommands      = @($Commands | Where-Object { $_.Name -notin $ExistingFiles })
$ExistingCommands = @($Commands | Where-Object { $_.Name -in $ExistingFiles })

# Create markdown for new commands
if ($NewCommands.Count -gt 0) {
    Write-Host "Creating help for $($NewCommands.Count) new command(s)..." -ForegroundColor Yellow
    foreach ($Command in $NewCommands) {
        $CommandHelp = New-CommandHelp -Command $Command
        # Export-MarkdownCommandHelp appends a <ModuleName> subfolder, so pass $HelpRoot.
        Export-MarkdownCommandHelp -CommandHelp $CommandHelp -OutputFolder $HelpRoot -Force
        Write-Host "  Created: $($Command.Name).md" -ForegroundColor Green
    }
}

# Update existing markdown files
if ($ExistingCommands.Count -gt 0) {
    Write-Host "Updating help for $($ExistingCommands.Count) existing command(s)..." -ForegroundColor Cyan
    foreach ($Command in $ExistingCommands) {
        $FilePath = Join-Path -Path $HelpDocsPath -ChildPath "$($Command.Name).md"
        $CommandHelp = Import-MarkdownCommandHelp -Path $FilePath
        $CommandHelp = Update-MarkdownCommandHelp -CommandHelp $CommandHelp -Command $Command
        Export-MarkdownCommandHelp -CommandHelp $CommandHelp -OutputFolder $HelpRoot -Force
        Write-Host "  Updated: $($Command.Name).md" -ForegroundColor DarkGray
    }
}

Write-Host "Help documentation is up to date at: $HelpDocsPath" -ForegroundColor Green
