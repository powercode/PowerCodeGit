<#
.SYNOPSIS
    Shared helper functions for the PowerCode.Git demo scripts.

.DESCRIPTION
    Dot-source this file from Demo-PowerCodeGit.ps1 and Demo-PowerCodeGit-Advanced.ps1
    to get the common Write-Demo, Write-Section, Invoke-Git, Remove-TempRepo,
    New-TempDir functions and the $TempDirs tracking list.
#>

# ─── Display helpers ────────────────────────────────────────────────────────────

function Write-Demo {
    param([string]$Text)
    Write-Host ''
    Write-Host "  ▶  $Text" -ForegroundColor Cyan
}

function Write-Section {
    param([string]$Title)
    Write-Host ''
    Write-Host ('─' * 72) -ForegroundColor DarkGray
    Write-Host "  $Title" -ForegroundColor Yellow
    Write-Host ('─' * 72) -ForegroundColor DarkGray
}

# ─── Git helpers ────────────────────────────────────────────────────────────────

# Simple git wrapper that surfaces errors only when git exits non-zero.
# Used for low-level git operations that have no PowerCode.Git cmdlet equivalent
# (e.g. git init, git merge --no-ff).
function Invoke-Git {
    param([string[]]$ArgumentList)
    $output = git @ArgumentList 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git $($ArgumentList -join ' ') failed: $output"
    }
    $output
}

# ─── Temporary directory management ────────────────────────────────────────────

# Tracks every temp directory created during the demo so the cleanup block
# at the end can remove them all, even if the script errors out early.
$TempDirs = [System.Collections.Generic.List[string]]::new()

function New-TempDir {
    param([string]$Tag = 'Demo')
    $path = Join-Path ([System.IO.Path]::GetTempPath()) "PowerGitDemo_${Tag}_$([System.Guid]::NewGuid().ToString('N')[0..7] -join '')"
    New-Item -Path $path -ItemType Directory -Force | Out-Null
    $TempDirs.Add($path)
    $path
}

# Remove a temp repo, clearing read-only flags first (important on Windows
# because git marks pack files as read-only).
function Remove-TempRepo {
    param([string]$Path)
    if (Test-Path $Path) {
        Get-ChildItem -Path $Path -Recurse -Force |
            Where-Object { $_.Attributes -band [System.IO.FileAttributes]::ReadOnly } |
            ForEach-Object { $_.Attributes = $_.Attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly) }
        Remove-Item -Path $Path -Recurse -Force
    }
}
