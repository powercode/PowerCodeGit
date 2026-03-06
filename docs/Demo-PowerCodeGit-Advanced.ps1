#Requires -Modules PowerCode.Git

<#
.SYNOPSIS
    Demo of advanced history-rewriting cmdlets in PowerCode.Git.

.DESCRIPTION
    Demonstrates Edit-GitHistory — a PowerShell-typed alternative to
    git filter-branch / git filter-repo that rewrites commits in-place.

    THIS SCRIPT IS DESTRUCTIVE BY DESIGN.  It permanently alters commit
    history in throw-away temporary repositories.  Never run these
    operations on shared branches without team agreement.

    Scenarios covered
    -----------------
    • Dry-run preview with -WhatIf
    • Rewrite author/committer across all commits  (-CommitFilter)
    • Remove a sensitive file from every commit     (-TreeFilter)
    • Prune commits that become empty after filtering

    For everyday workflows (status, staging, branching, remotes, etc.)
    see Demo-PowerCodeGit.ps1 in the same directory.

.NOTES
    Prerequisites
    -------------
    Install the module if you haven't already:

        Install-Module PowerCode.Git

    git must be on your PATH (any recent version).

    No network access is required.  All operations use local
    temporary repositories that are cleaned up automatically.

    Cmdlets demonstrated
    --------------------
    Edit-GitHistory       Invoke-GitRepository
    Get-GitLog            Save-GitCommit
    Add-GitItem
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module PowerCode.Git -ErrorAction Stop

# ─── Shared helpers (display, temp dirs, git wrapper) ───────────────────────────
. "$PSScriptRoot/Demo-Helpers.ps1"

# ─── SECTION 1 — HISTORY REWRITING  (Edit-GitHistory) ────────────────────────

#region 01 - HISTORY REWRITING  (Edit-GitHistory)

Write-Section '01 · HISTORY REWRITING  (Edit-GitHistory)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Tools-Rewriting-History
#
# Edit-GitHistory is a PowerShell-typed alternative to git filter-branch / git filter-repo.
# It rewrites history in-place — use only on unshared branches or with team agreement.

# ── Build a small repo with three commits.
# One commit accidentally contains a secret file, and all commits have an
# outdated author identity.  We will fix both problems with Edit-GitHistory.
$HistoryPath = New-TempDir 'History'
Push-Location $HistoryPath
try {
    Invoke-Git @('init', '--initial-branch', 'main')
    # Disable autocrlf to avoid "LF will be replaced by CRLF" warnings
    Set-GitConfiguration -Name 'core.autocrlf' -Value 'false' | Out-Null
    Set-GitConfiguration -Name 'user.name' -Value 'Old Name' | Out-Null
    Set-GitConfiguration -Name 'user.email' -Value 'old@oldcorp.com' | Out-Null

    Set-Content -Path 'app.ps1' -Value '# Application'
    Add-GitItem -All
    Save-GitCommit -Message 'initial commit' | Out-Null

    Set-Content -Path 'secret.txt' -Value 'API_KEY=s3cr3t_d0_n0t_c0mm1t'
    Add-GitItem -All
    Save-GitCommit -Message 'feat: add API integration' | Out-Null

    Set-Content -Path 'README.md' -Value '# My App'
    Add-GitItem -All
    Save-GitCommit -Message 'docs: project README' | Out-Null
}
finally {
    Pop-Location
}

# ── 1a. Dry-run preview ──────────────────────────────────────────────────────

Write-Demo "Edit-GitHistory -WhatIf — preview what would change without modifying anything"
$Preview = Edit-GitHistory -CommitFilter {
    if ($commit.Author.Email -eq 'old@oldcorp.com') {
        @{ Author = @{ Name = 'Alice Dev'; Email = 'alice@example.com'; When = $commit.Author.When } }
    }
} -WhatIf -RepoPath $HistoryPath

$Preview | Format-Table OldSha, EmailModified, MessageModified -AutoSize
Write-Host "  (repository unchanged — this was a dry run)"

# ── 1b. Rewrite author identity ──────────────────────────────────────────────

Write-Demo "Edit-GitHistory -CommitFilter — rewrite author email across all commits  [REWRITES HISTORY]"
Write-Warning "Edit-GitHistory rewrites history. Only use on branches that have not been shared with others."
Edit-GitHistory -CommitFilter {
    if ($commit.Author.Email -eq 'old@oldcorp.com') {
        @{
            Author    = @{ Name = 'Alice Dev'; Email = 'alice@example.com'; When = $commit.Author.When }
            Committer = @{ Name = 'Alice Dev'; Email = 'alice@example.com'; When = $commit.Committer.When }
        }
    }
} -Force -Confirm:$false -RepoPath $HistoryPath | Out-Null

Get-GitLog -RepoPath $HistoryPath | Format-Table ShortSha, AuthorName, AuthorEmail, MessageShort -AutoSize

# ── 1c. Remove a sensitive file from every commit ────────────────────────────

Write-Demo "Edit-GitHistory -TreeFilter — remove a file from all commits  [REWRITES HISTORY]"

# Use Invoke-GitRepository to verify the secret file is present before removal.
# $repo is a LibGit2Sharp.Repository — see Demo-PowerCodeGit.ps1 section 17 for
# a full tour of Invoke-GitRepository.
$SecretInHistory = Invoke-GitRepository -RepoPath $HistoryPath -Action {
    @($repo.Commits | Where-Object { $_.Tree['secret.txt'] }).Count
}
Write-Host "  'secret.txt' appears in $SecretInHistory commit tree(s) before removal"

# Use a separate backup namespace so we don't collide with the author-rewrite backups above.
Edit-GitHistory -TreeFilter {
    # Return $false / $null to exclude a path from the tree; $true to keep
    -not ($_.Path -eq 'secret.txt')
} -PruneEmptyCommits -BackupNamespace 'refs/original-tree/' -Force -Confirm:$false -RepoPath $HistoryPath | Out-Null

$SecretAfter = Invoke-GitRepository -RepoPath $HistoryPath -Action {
    @($repo.Commits | Where-Object { $_.Tree['secret.txt'] }).Count
}
Write-Host "  'secret.txt' appears in $SecretAfter commit tree(s) after removal"
Get-GitLog -RepoPath $HistoryPath | Format-Table ShortSha, MessageShort -AutoSize

#endregion

# ─── CLEANUP ─────────────────────────────────────────────────────────────────

#region CLEANUP

Write-Section 'CLEANUP'

Write-Demo "Removing all temporary repositories..."
foreach ($dir in $TempDirs) {
    Write-Host "  Removing $dir" -ForegroundColor DarkGray
    Remove-TempRepo -Path $dir
}

Write-Host ''
Write-Host '  All done!  Advanced history-rewriting cmdlets demonstrated.' -ForegroundColor Green
Write-Host ''
Write-Host '  Cmdlets demonstrated:' -ForegroundColor Green
Write-Host @'
    Edit-GitHistory       Invoke-GitRepository
    Get-GitLog            Save-GitCommit
    Add-GitItem
'@
Write-Host ''
Write-Host '  For everyday workflows (status, staging, branching, remotes, etc.)' -ForegroundColor DarkGray
Write-Host '  see Demo-PowerCodeGit.ps1' -ForegroundColor DarkGray

#endregion
