<#
.SYNOPSIS
    Launches Pester system tests for PowerCode.Git in a clean pwsh process.
.DESCRIPTION
    Builds the solution, then spawns a separate pwsh.exe process to run the
    Pester system tests. A fresh process is required because binary modules
    lock their assemblies in the hosting process, which can interfere with
    rebuilds and test isolation.
.PARAMETER Configuration
    The build configuration to use. Defaults to 'debug'.
.PARAMETER NoBuild
    Skip the dotnet build step. Use when the solution is already built.
.PARAMETER CommandName
    One or more cmdlet names to filter which test files are run. Each name
    must match the file stem of a test file (e.g. 'Get-GitBranch' runs
    'Get-GitBranch.Tests.ps1'). When omitted, all test files are run.
.EXAMPLE
    .\scripts\Invoke-SystemTests.ps1
.EXAMPLE
    .\scripts\Invoke-SystemTests.ps1 -Configuration Release
.EXAMPLE
    .\scripts\Invoke-SystemTests.ps1 -NoBuild
.EXAMPLE
    .\scripts\Invoke-SystemTests.ps1 -CommandName Get-GitBranch
.EXAMPLE
    .\scripts\Invoke-SystemTests.ps1 -CommandName Get-GitBranch, Save-GitCommit
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('debug', 'release')]
    [string]$Configuration = 'debug',

    [Parameter()]
    [switch]$NoBuild,

    [ArgumentCompleter({
        $TestDir = Join-Path -Path (Resolve-Path -Path "$PSScriptRoot/..").Path -ChildPath 'tests/PowerCode.Git.SystemTests'
        if (Test-Path -Path $TestDir) {
            Get-ChildItem -Path $TestDir -Filter '*.Tests.ps1' -File | ForEach-Object {
                $_.BaseName -replace '\.Tests$'
            }
        }
    })]
    [Parameter()]
    [string[]]$CommandName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path -Path "$PSScriptRoot/..").Path
$TestDir = Join-Path -Path $RepoRoot -ChildPath 'tests/PowerCode.Git.SystemTests'

if (-not (Test-Path -Path $TestDir)) {
    Write-Error -Message "System test directory not found: $TestDir"
    return
}

# Build the solution unless skipped
if (-not $NoBuild) {
    Write-Host 'Building solution...' -ForegroundColor Cyan
    $SlnPath = Join-Path -Path $RepoRoot -ChildPath 'PowerCode.Git.slnx'
    dotnet build $SlnPath --configuration $Configuration --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error -Message "Build failed with exit code $LASTEXITCODE."
        return
    }
}

# Verify the module layout was produced
$ModuleLayoutDir = Join-Path -Path $RepoRoot -ChildPath 'artifacts/module/PowerCode.Git'
$VersionedDir = Get-ChildItem -Path $ModuleLayoutDir -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $VersionedDir) {
    Write-Error -Message "No versioned module folder found under '$ModuleLayoutDir'. Ensure the build succeeded."
    return
}

$ModulePath = Join-Path -Path $VersionedDir.FullName -ChildPath 'PowerCode.Git.psd1'
if (-not (Test-Path -Path $ModulePath)) {
    Write-Error -Message "Module manifest not found at '$ModulePath'. Ensure the build succeeded."
    return
}

Write-Host "Running Pester system tests in a clean pwsh process..." -ForegroundColor Cyan
Write-Host "Test dir:  $TestDir" -ForegroundColor DarkGray
Write-Host "Module:    $ModulePath" -ForegroundColor DarkGray

# Build the Run.Path expression that the child process will evaluate.
# Always use the @(...) array literal form so Pester accepts one or many paths.
if ($CommandName) {
    $TestRunPaths = $CommandName | ForEach-Object { Join-Path -Path $TestDir -ChildPath "$($_).Tests.ps1" }
    $EscapedPaths  = $TestRunPaths | ForEach-Object { "'" + ($_ -replace "'", "''") + "'" }
    $TestRunPathArg = "@($($EscapedPaths -join ','))"
} else {
    $TestRunPathArg = "'$($TestDir -replace "'", "''")'"
}

# Launch a fresh pwsh.exe process to avoid binary module assembly locking.
# Use -EncodedCommand to pass the script block safely without quoting issues.
# Pass the module path via environment variable so the test file can locate it.
$PesterScript = @"
`$ErrorActionPreference = 'Stop'
`$ProgressPreference = 'SilentlyContinue'

`$env:POWERCODE_GIT_MODULE_PATH = '$($ModulePath -replace "'", "''")'

# Ensure Pester v5+ is available
`$PesterModule = Get-Module -Name Pester -ListAvailable | Where-Object { `$_.Version.Major -ge 5 } | Select-Object -First 1
if (-not `$PesterModule) {
    Write-Host 'Installing Pester v5...' -ForegroundColor Yellow
    Install-Module -Name Pester -MinimumVersion 5.0.0 -Force -Scope CurrentUser -SkipPublisherCheck
}

Import-Module -Name Pester -MinimumVersion 5.0.0 -Force

`$Config = New-PesterConfiguration
`$Config.Run.Path = $TestRunPathArg
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
