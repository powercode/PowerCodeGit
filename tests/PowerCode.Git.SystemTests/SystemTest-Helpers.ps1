<#
.SYNOPSIS
    Shared helper functions and module import logic for PowerCode.Git system tests.
.DESCRIPTION
    This script is dot-sourced by system test scripts to provide consistent initialization logic and helper functions.
#>

if ($env:POWERCODE_GIT_MODULE_PATH -and (Test-Path -Path $env:POWERCODE_GIT_MODULE_PATH)) {
    $ModulePath = $env:POWERCODE_GIT_MODULE_PATH
}
else {
    $RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path
    $ModuleLayoutDir = Join-Path -Path $RepoRoot -ChildPath 'artifacts/module/PowerCode.Git'
    $VersionedDir = Get-ChildItem -Path $ModuleLayoutDir -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $VersionedDir) {
        throw "No versioned module folder found under '$ModuleLayoutDir'. Build the solution before running system tests."
    }
    $ModulePath = Join-Path -Path $VersionedDir.FullName -ChildPath 'PowerCode.Git.psd1'
}

if (-not (Test-Path -Path $ModulePath)) {
    throw "Module not found at '$ModulePath'. Build the solution before running system tests."
}

Import-Module -Name $ModulePath -Force -ErrorAction Stop

# Platform-agnostic non-existent repo path for tests (avoids Windows-only C:\ paths on Linux)
$NonExistentRepoPath = if ($IsWindows) { 'C:\nonexistent\repo\path' } else { '/nonexistent/repo/path' }

function New-TestGitRepository {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string[]]$CommitMessages = @('Initial commit'),

        [Parameter()]
        [string]$AuthorName = 'Test Author',

        [Parameter()]
        [string]$AuthorEmail = 'test@example.com'
    )

    $TempDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
    New-Item -Path $TempDir -ItemType Directory -Force | Out-Null

    Push-Location -Path $TempDir
    try {
        git init --initial-branch main 2>&1 | Out-Null
        git config user.name $AuthorName
        git config user.email $AuthorEmail

        foreach ($Message in $CommitMessages) {
            $FileName = "file_$([System.Guid]::NewGuid().ToString('N')).txt"
            Set-Content -Path (Join-Path -Path $TempDir -ChildPath $FileName) -Value $Message
            git add . 2>&1 | Out-Null
            git commit -m $Message 2>&1 | Out-Null
        }
    }
    finally {
        Pop-Location
    }

    return $TempDir
}

function Remove-TestGitRepository {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (Test-Path -Path $Path) {
        Get-ChildItem -Path $Path -Recurse -Force | ForEach-Object {
            if ($_.Attributes -band [System.IO.FileAttributes]::ReadOnly) {
                $_.Attributes = $_.Attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly)
            }
        }
        Remove-Item -Path $Path -Recurse -Force
    }
}

function New-TestRepoWithRemote {
    <#
    .SYNOPSIS
        Creates a working repository with a local bare remote, suitable for push/pull testing.
    .OUTPUTS
        A hashtable with keys WorkingPath and BarePath.
    #>
    [CmdletBinding()]
    param()

    $BareDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitBare_$([System.Guid]::NewGuid().ToString('N'))"
    $WorkDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitWork_$([System.Guid]::NewGuid().ToString('N'))"

    # Create bare repo
    git init --bare --initial-branch main $BareDir 2>&1 | Out-Null

    # Clone it to get a working copy with origin set
    git clone $BareDir $WorkDir 2>&1 | Out-Null

    Push-Location -Path $WorkDir
    try {
        git config user.name 'Test Author'
        git config user.email 'test@example.com'
        Set-Content -Path 'README.md' -Value '# Test Repo'
        git add . 2>&1 | Out-Null
        git commit -m 'Initial commit' 2>&1 | Out-Null
        git push origin main 2>&1 | Out-Null
    }
    finally {
        Pop-Location
    }

    return @{
        WorkingPath = $WorkDir
        BarePath    = $BareDir
    }
}
