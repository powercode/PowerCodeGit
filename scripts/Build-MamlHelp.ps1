<#
.SYNOPSIS
    Generates MAML external help from PlatyPS markdown documentation.
.DESCRIPTION
    Uses Microsoft.PowerShell.PlatyPS (v2) to convert the markdown help files
    under docs/help/PowerCode.Git/ into a MAML XML file that PowerShell can
    use for Get-Help. The output is placed in the en-US subfolder of the
    versioned module layout directory.

    When the current git branch is 'main' or 'preview', the script replaces
    the {{BranchName}} placeholder in HelpUri front-matter with the actual
    branch name before exporting.

    The script installs Microsoft.PowerShell.PlatyPS from PSGallery if it is
    not already available.
.PARAMETER Configuration
    The build configuration whose module layout to target. Defaults to 'debug'.
.PARAMETER OutputPath
    Optional override for the output directory. When omitted the script
    auto-discovers the module layout under artifacts/module/PowerCode.Git/<version>/en-US.
.EXAMPLE
    .\scripts\Build-MamlHelp.ps1
.EXAMPLE
    .\scripts\Build-MamlHelp.ps1 -Configuration Release
.EXAMPLE
    .\scripts\Build-MamlHelp.ps1 -OutputPath ./artifacts/help/en-US
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('debug', 'release')]
    [string]$Configuration = 'debug',

    [Parameter()]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path -Path "$PSScriptRoot/..").Path
$HelpDocsPath = Join-Path -Path $RepoRoot -ChildPath 'docs/help/PowerCode.Git'

if (-not (Test-Path -Path $HelpDocsPath)) {
    Write-Error -Message "Help docs directory not found: $HelpDocsPath"
    return
}

# Resolve the output directory
if (-not $OutputPath) {
    $ModuleLayoutDir = Join-Path -Path $RepoRoot -ChildPath 'artifacts/module/PowerCode.Git'
    $VersionedDir = Get-ChildItem -Path $ModuleLayoutDir -Directory -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $VersionedDir) {
        Write-Error -Message "No versioned module folder found under '$ModuleLayoutDir'. Build the solution first."
        return
    }
    $OutputPath = Join-Path -Path $VersionedDir.FullName -ChildPath 'en-US'
}

if (-not (Test-Path -Path $OutputPath)) {
    $null = New-Item -Path $OutputPath -ItemType Directory -Force
}

# Ensure Microsoft.PowerShell.PlatyPS is available
$PlatyPSModule = Get-Module -Name Microsoft.PowerShell.PlatyPS -ListAvailable |
    Select-Object -First 1
if (-not $PlatyPSModule) {
    Write-Host 'Installing Microsoft.PowerShell.PlatyPS...' -ForegroundColor Yellow
    Install-PSResource -Name Microsoft.PowerShell.PlatyPS -TrustRepository -Repository PSGallery
}

Import-Module -Name Microsoft.PowerShell.PlatyPS -Force

# Gather all markdown help files
$MarkdownFiles = Get-ChildItem -Path $HelpDocsPath -Filter '*.md' -File
if ($MarkdownFiles.Count -eq 0) {
    Write-Warning "No markdown help files found in '$HelpDocsPath'."
    return
}

Write-Host "Exporting MAML help from $($MarkdownFiles.Count) markdown file(s)..." -ForegroundColor Cyan
Write-Host "  Source: $HelpDocsPath" -ForegroundColor DarkGray
Write-Host "  Output: $OutputPath" -ForegroundColor DarkGray

# When on main or preview, replace the {{BranchName}} placeholder in HelpUri
# with the actual branch name. Work on a temp copy so the repo files stay unchanged.
# In CI (GitHub Actions) the checkout is detached, so fall back to GITHUB_REF_NAME.
$BranchName = git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null
if ($BranchName -eq 'HEAD' -and $env:GITHUB_REF_NAME) {
    $BranchName = $env:GITHUB_REF_NAME
}

# Always export from a temp copy so local builds never dirty the source tree.
$TempHelpDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.Git-help-$([System.Guid]::NewGuid().ToString('N'))"
$null = New-Item -Path $TempHelpDir -ItemType Directory -Force

foreach ($File in $MarkdownFiles) {
    $Content = Get-Content -Path $File.FullName -Raw
    if ($BranchName -in @('main', 'preview')) {
        $Content = $Content -replace '\{\{BranchName\}\}', $BranchName
    }
    Set-Content -Path (Join-Path -Path $TempHelpDir -ChildPath $File.Name) -Value $Content -NoNewline
}

if ($BranchName -in @('main', 'preview')) {
    Write-Host "  Branch '$BranchName' detected — replaced {{BranchName}} placeholder." -ForegroundColor Yellow
}

Export-MamlCommandHelp -Path $TempHelpDir -OutputFolder $OutputPath -Force

# Clean up temp directory
Remove-Item -Path $TempHelpDir -Recurse -Force

$MamlFile = Join-Path -Path $OutputPath -ChildPath 'PowerCode.Git.dll-Help.xml'
if (Test-Path -Path $MamlFile) {
    Write-Host "MAML help generated: $MamlFile" -ForegroundColor Green
}
else {
    Write-Error -Message "Expected MAML file was not created at '$MamlFile'."
}
