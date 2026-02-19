<#
.SYNOPSIS
    Launches Pester system tests for PowerGit in a clean pwsh process.
.DESCRIPTION
    Builds the solution, then spawns a separate pwsh.exe process to run the
    Pester system tests. A fresh process is required because binary modules
    lock their assemblies in the hosting process, which can interfere with
    rebuilds and test isolation.
.PARAMETER Configuration
    The build configuration to use. Defaults to 'debug'.
.PARAMETER NoBuild
    Skip the dotnet build step. Use when the solution is already built.
.EXAMPLE
    .\scripts\Invoke-SystemTests.ps1
.EXAMPLE
    .\scripts\Invoke-SystemTests.ps1 -Configuration Release
.EXAMPLE
    .\scripts\Invoke-SystemTests.ps1 -NoBuild
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('debug', 'release')]
    [string]$Configuration = 'debug',

    [Parameter()]
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path -Path "$PSScriptRoot/..").Path
$TestFile = Join-Path -Path $RepoRoot -ChildPath 'tests/PowerGit.SystemTests/Get-GitLog.Tests.ps1'

if (-not (Test-Path -Path $TestFile)) {
    Write-Error -Message "Test file not found: $TestFile"
    return
}

# Build the solution unless skipped
if (-not $NoBuild) {
    Write-Host 'Building solution...' -ForegroundColor Cyan
    $SlnPath = Join-Path -Path $RepoRoot -ChildPath 'PowerGit.slnx'
    dotnet build $SlnPath --configuration $Configuration --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error -Message "Build failed with exit code $LASTEXITCODE."
        return
    }
}

# Verify the module was produced
$ModulePath = Join-Path -Path $RepoRoot -ChildPath "artifacts/bin/PowerGit/$Configuration/PowerGit.psd1"
if (-not (Test-Path -Path $ModulePath)) {
    Write-Error -Message "Module manifest not found at '$ModulePath'. Ensure the build succeeded."
    return
}

Write-Host "Running Pester system tests in a clean pwsh process..." -ForegroundColor Cyan
Write-Host "Test file: $TestFile" -ForegroundColor DarkGray
Write-Host "Module:    $ModulePath" -ForegroundColor DarkGray

# Launch a fresh pwsh.exe process to avoid binary module assembly locking.
# Use -EncodedCommand to pass the script block safely without quoting issues.
$PesterScript = @"
`$ErrorActionPreference = 'Stop'

# Ensure Pester v5+ is available
`$PesterModule = Get-Module -Name Pester -ListAvailable | Where-Object { `$_.Version.Major -ge 5 } | Select-Object -First 1
if (-not `$PesterModule) {
    Write-Host 'Installing Pester v5...' -ForegroundColor Yellow
    Install-Module -Name Pester -MinimumVersion 5.0.0 -Force -Scope CurrentUser -SkipPublisherCheck
}

Import-Module -Name Pester -MinimumVersion 5.0.0 -Force

`$Config = New-PesterConfiguration
`$Config.Run.Path = '$($TestFile -replace "'", "''")'
`$Config.Run.Exit = `$true
`$Config.Output.Verbosity = 'Detailed'

Invoke-Pester -Configuration `$Config
"@

$EncodedCommand = [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($PesterScript))
pwsh -NoProfile -NonInteractive -EncodedCommand $EncodedCommand
$TestExitCode = $LASTEXITCODE

if ($TestExitCode -eq 0) {
    Write-Host "`nAll system tests passed." -ForegroundColor Green
}
else {
    Write-Host "`nSystem tests failed with exit code $TestExitCode." -ForegroundColor Red
}

exit $TestExitCode
