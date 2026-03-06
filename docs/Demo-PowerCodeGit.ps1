#Requires -Modules PowerCode.Git

<#
.SYNOPSIS
    End-to-end runnable demo of everyday PowerCode.Git cmdlets.

.DESCRIPTION
    Creates fresh git repositories in a temporary directory, then walks through
    the most commonly used PowerCode.Git cmdlets with real-world scenarios:
    staging, committing, branching, rebasing, remote operations, worktrees,
    commit searching, and more.

    The script runs top-to-bottom without manual intervention.  Each section
    is wrapped in a #region block so you can collapse/expand sections in VS Code
    or the PowerShell ISE and step through them interactively if you prefer.

    Where a topic deserves deeper study, a Git Book reference is included.
    The Pro Git book is free online at https://git-scm.com/book/en/v2

    For advanced history rewriting (Edit-GitHistory), see
    Demo-PowerCodeGit-Advanced.ps1 in the same directory.

.NOTES
    Prerequisites
    -------------
    Install the module if you haven't already:

        Install-Module PowerCode.Git

    git must be on your PATH (any recent version).

    No network access is required.  Remote operations use a local bare
    repository so the demo is entirely self-contained.

    Cmdlets demonstrated (39 total)
    --------------------------------
    Add-GitItem           Clear-GitConfiguration  Compare-GitTree
    Copy-GitRepository    Get-GitBranch           Get-GitCommitFile
    Get-GitConfiguration  Get-GitDiff             Get-GitLog
    Get-GitModuleConfiguration  Get-GitPromptStatus
    Get-GitRemote         Get-GitStatus           Get-GitTag
    Get-GitWorktree       Invoke-GitRepository    Lock-GitWorktree
    New-GitBranch         New-GitRemote           New-GitWorktree
    Receive-GitBranch     Remove-GitBranch        Remove-GitRemote
    Remove-GitTag         Remove-GitWorktree      Reset-GitHead
    Restore-GitItem       Resume-GitRebase        Save-GitCommit
    Select-GitCommit      Send-GitBranch          Set-GitBranch
    Set-GitConfiguration  Set-GitModuleConfiguration  Set-GitRemote
    Set-GitTag            Start-GitRebase         Stop-GitRebase
    Switch-GitBranch      Unlock-GitWorktree
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module PowerCode.Git -ErrorAction Stop

# ─── Shared helpers (display, temp dirs, git wrapper) ───────────────────────────
. "$PSScriptRoot/Demo-Helpers.ps1"

# ─── SECTION 1 - REPOSITORY STATUS ───────────────────────────────────────────

#region 01 - REPOSITORY STATUS

Write-Section '01 · REPOSITORY STATUS  (Get-GitStatus)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Basics-Recording-Changes-to-the-Repository

# Build a fresh repository with two commits to serve as the main playground
# for sections 1–12.  We use Invoke-Git for 'git init' because there is no
# PowerCode.Git cmdlet for repository creation.
$MainPath = New-TempDir 'Main'

Push-Location $MainPath
try {
    Invoke-Git @('init', '--initial-branch', 'main')
    # Disable autocrlf so PowerShell's LF-only strings don't trigger
    # "LF will be replaced by CRLF" warnings on Windows.
    Set-GitConfiguration -Name 'core.autocrlf' -Value 'false' | Out-Null
    Set-GitConfiguration -Name user.name -Value 'Alice Dev' | Out-Null
    Set-GitConfiguration -Name user.email -Value 'alice@example.com' | Out-Null
    
    # Seed the repo with two commits so we have some history to explore
    New-Item -Path 'README.md' -Value "# My Project`n`nWelcome to the project." | Out-Null
    New-Item -Path 'CHANGELOG.md' -Value "# Changelog`n`n## [Unreleased]`n" | Out-Null
    Add-GitItem -All
    Save-GitCommit -Message 'docs: initial commit with README and CHANGELOG' | Out-Null

    New-Item -ItemType Directory -Name 'src' | Out-Null
    New-Item -Path 'src/utils.ps1' -Value '# Utility functions`nfunction Get-Version { return "1.0.0" }' | Out-Null
    New-Item -Path 'src/main.ps1' -Value '# Entry point`n. ./utils.ps1`nGet-Version' | Out-Null
    Add-GitItem -All
    Save-GitCommit -Message 'feat: add src/utils and src/main' | Out-Null
}
finally {
    Pop-Location
}

# ── Now make some working-tree changes to demonstrate status ──────────────────

# 1. Modify a tracked file
Add-Content -Path (Join-Path $MainPath 'README.md') -Value "`n## Getting Started`n"

# 2. Stage a new file
New-Item -Path (Join-Path $MainPath 'CONTRIBUTING.md') -Value "# Contributing`n" | Out-Null
Add-GitItem -Path (Join-Path $MainPath 'CONTRIBUTING.md') -RepoPath $MainPath

# 3. Add an untracked file (not staged)
New-Item -Path (Join-Path $MainPath 'scratch.tmp') -Value 'work in progress' | Out-Null

Write-Demo "Get-GitStatus — shows staged, modified, and untracked entries"
$Status = Get-GitStatus -RepoPath $MainPath
$Status | Format-Table -AutoSize

Write-Demo "Entries broken out by state:"
$Status.Entries | Format-Table FilePath, Status -AutoSize

Write-Demo "Get-GitStatus -IncludeIgnored — also surfaces .gitignore'd files"
# Create a .gitignore first
Set-Content -Path (Join-Path $MainPath '.gitignore') -Value '*.tmp'
Add-GitItem -Path (Join-Path $MainPath '.gitignore') -RepoPath $MainPath
$StatusWithIgnored = Get-GitStatus -RepoPath $MainPath -IncludeIgnored
$StatusWithIgnored.Entries | Where-Object Status -EQ Ignored | Format-Table FilePath, Status

#endregion

# ─── SECTION 2 - STAGING FILES ────────────────────────────────────────────────

#region 02 - STAGING FILES  (Add-GitItem)

Write-Section '02 · STAGING FILES  (Add-GitItem)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Basics-Recording-Changes-to-the-Repository

Write-Demo "Add-GitItem -Path — stage a specific file"
Add-GitItem -Path (Join-Path $MainPath 'README.md') -RepoPath $MainPath

Write-Demo "Add-GitItem -All — stage everything (new + modified + deleted)"
Add-GitItem -All -RepoPath $MainPath

Write-Demo "Pipeline: stage only Modified entries reported by Get-GitStatus"
# Unstage README.md so we can re-stage it via the pipeline pattern
Reset-GitHead -Path (Join-Path $MainPath 'README.md') -RepoPath $MainPath -Confirm:$false

Get-GitStatus -RepoPath $MainPath |
    Select-Object -ExpandProperty Entries |
    Where-Object Status -EQ Modified |
    Add-GitItem -RepoPath $MainPath

Write-Demo "Add-GitItem -Update — stage only already-tracked files (no new files)"
# Unstage everything so we can re-demo with -Update
Reset-GitHead -RepoPath $MainPath -Confirm:$false
Add-GitItem -Update -RepoPath $MainPath

Write-Demo "Final staged state before commit:"
(Get-GitStatus -RepoPath $MainPath).Entries |
    Where-Object { $_.Status -notlike '*Untracked*' } |
    Format-Table FilePath, Status

# Stage everything cleanly for the next section
Add-GitItem -All -RepoPath $MainPath

#endregion

# ─── SECTION 3 - VIEWING DIFFS ────────────────────────────────────────────────

#region 03 - VIEWING DIFFS  (Get-GitDiff)

Write-Section '03 · VIEWING DIFFS  (Get-GitDiff)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Basics-Recording-Changes-to-the-Repository

# Make an unstaged change to demonstrate unstaged diff
Add-Content -Path (Join-Path $MainPath 'CHANGELOG.md') -Value "## [1.0.0] - 2026-03-05`n- Initial release`n"

Write-Demo "Get-GitDiff — unstaged changes in the working tree"
Get-GitDiff -RepoPath $MainPath | Format-Table FilePath, LinesAdded, LinesRemoved -AutoSize

Write-Demo "Get-GitDiff -Staged — changes already in the index (staged)"
Get-GitDiff -Staged -RepoPath $MainPath | Format-Table FilePath, LinesAdded, LinesRemoved -AutoSize

Write-Demo "Get-GitDiff -Context 0 — diff with no surrounding context lines"
Get-GitDiff -Context 0 -RepoPath $MainPath | Select-Object -First 1 | Select-Object -ExpandProperty Patch

Write-Demo "Get-GitDiff -Hunk — break diff into individual hunks for selective staging"
$Hunks = Get-GitDiff -Hunk -RepoPath $MainPath
$Hunks | Format-Table FilePath, StartLine, LinesAdded, LinesRemoved -AutoSize

Write-Demo "Selective staging: only stage hunks in *.md files"
$Hunks | Where-Object { $_.FilePath -like '*.md' } | Add-GitItem -RepoPath $MainPath

Write-Demo "Get-GitDiff -Commit — diff a specific commit vs its parent"
$HeadSha = (Get-GitLog -MaxCount 1 -RepoPath $MainPath).Sha
Get-GitDiff -Commit $HeadSha -RepoPath $MainPath | Format-Table FilePath, LinesAdded -AutoSize

# Stage everything for the commit section
Add-GitItem -All -RepoPath $MainPath

#endregion

# ─── SECTION 4 - COMMITTING ───────────────────────────────────────────────────

#region 04 - COMMITTING  (Save-GitCommit)

Write-Section '04 · COMMITTING  (Save-GitCommit)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Basics-Recording-Changes-to-the-Repository

Write-Demo "Save-GitCommit -Message — create a commit from the current index"
$Commit = Save-GitCommit -Message 'docs: add CONTRIBUTING, update CHANGELOG' -RepoPath $MainPath
Write-Host "  Created commit $($Commit.ShortSha): $($Commit.MessageShort)"

Write-Demo "Save-GitCommit -All — stage all tracked changes then commit in one step"
Add-Content -Path (Join-Path $MainPath 'README.md') -Value "`n## License`n"
$Commit2 = Save-GitCommit -All -Message 'docs: add License section to README' -RepoPath $MainPath
Write-Host "  Created commit $($Commit2.ShortSha): $($Commit2.MessageShort)"

Write-Demo "Save-GitCommit -Amend — amend the previous commit's message/content"
Add-Content -Path (Join-Path $MainPath 'README.md') -Value "`n> See LICENSE file for details.`n"
Add-GitItem -All -RepoPath $MainPath
$Amended = Save-GitCommit -Amend -Message 'docs: add License section and pointer to file' -RepoPath $MainPath
Write-Host "  Amended to: $($Amended.ShortSha) $($Amended.MessageShort)"

Write-Demo "Save-GitCommit -AllowEmpty — commit with no changes (useful for triggers)"
$Empty = Save-GitCommit -AllowEmpty -Message 'ci: trigger pipeline' -RepoPath $MainPath
Write-Host "  Empty commit: $($Empty.ShortSha)"

Write-Demo "Save-GitCommit -Author — override the commit author (e.g. on behalf of another)"
New-Item -Path (Join-Path $MainPath 'SECURITY.md') -Value '# Security Policy' | Out-Null
Add-GitItem -All -RepoPath $MainPath
$BobCommit = Save-GitCommit -Message 'docs: add security policy' `
    -Author 'Bob Reviewer <bob@example.com>' `
    -RepoPath $MainPath
Write-Host "  Author: $($BobCommit.AuthorName) <$($BobCommit.AuthorEmail)>"

Write-Demo "Save-GitCommit -Date — backdate a commit (useful for importing history)"
New-Item -Path (Join-Path $MainPath 'AUTHORS') -Value 'Alice Dev <alice@example.com>' | Out-Null
Add-GitItem -All -RepoPath $MainPath
$BackDate = [DateTimeOffset]::new(2025, 1, 1, 9, 0, 0, [TimeSpan]::Zero)
$Backdated = Save-GitCommit -Message 'chore: record original authors' -Date $BackDate -RepoPath $MainPath
Write-Host "  Commit date: $($Backdated.AuthorDate)"

#endregion

# ─── SECTION 5 - VIEWING HISTORY ─────────────────────────────────────────────

#region 05 - VIEWING HISTORY  (Get-GitLog, Get-GitCommitFile)

Write-Section '05 · VIEWING HISTORY  (Get-GitLog + Get-GitCommitFile)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Basics-Viewing-the-Commit-History

Write-Demo "Get-GitLog — full commit history (newest first)"
Get-GitLog -RepoPath $MainPath | Format-Table ShortSha, AuthorName, MessageShort -AutoSize

Write-Demo "Get-GitLog -MaxCount 3 — last 3 commits"
Get-GitLog -MaxCount 3 -RepoPath $MainPath | Format-Table ShortSha, MessageShort -AutoSize

Write-Demo "Get-GitLog -Author 'Bob' — filter by author name or email"
Get-GitLog -Author 'Bob' -RepoPath $MainPath | Format-Table ShortSha, AuthorName, MessageShort

Write-Demo "Get-GitLog -MessagePattern 'docs:' — filter by commit message pattern"
Get-GitLog -MessagePattern 'docs:' -RepoPath $MainPath | Format-Table ShortSha, MessageShort -AutoSize

Write-Demo "Get-GitLog -Since (30 days ago) — time-bounded history"
Get-GitLog -Since (Get-Date).AddDays(-30) -RepoPath $MainPath |
    Format-Table ShortSha, AuthorDate, MessageShort -AutoSize

Write-Demo "Get-GitLog -NoMerges -FirstParent — linear, merge-free view"
Get-GitLog -NoMerges -FirstParent -RepoPath $MainPath |
    Format-Table ShortSha, MessageShort -AutoSize

Write-Demo "Get-GitLog | Get-GitCommitFile — see which files changed in each commit"
Get-GitLog -MaxCount 2 -RepoPath $MainPath |
    Get-GitCommitFile -RepoPath $MainPath |
    Format-Table CommitSha, FilePath, Status -AutoSize

Write-Demo "Get-GitCommitFile -Hunk — patch-level detail for a specific commit"
$LatestSha = (Get-GitLog -MaxCount 1 -RepoPath $MainPath).Sha
Get-GitCommitFile -Commit $LatestSha -Hunk -RepoPath $MainPath |
    Select-Object -First 3 |
    Format-Table FilePath, StartLine, LinesAdded -AutoSize

#endregion

# ─── SECTION 6 - BRANCHING ───────────────────────────────────────────────────

#region 06 - BRANCHING  (Get/New/Switch/Set/Remove-GitBranch)

Write-Section '06 · BRANCHING  (Get-GitBranch · New-GitBranch · Switch-GitBranch · Set-GitBranch · Remove-GitBranch)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Branching-Branches-in-a-Nutshell

Write-Demo "Get-GitBranch — list local branches"
Get-GitBranch -RepoPath $MainPath | Format-Table Name, IsCurrentBranch, Ahead, Behind -AutoSize

Write-Demo "New-GitBranch -Name — create a branch (stays on current branch)"
New-GitBranch -Name 'feature/api-docs' -RepoPath $MainPath

Write-Demo "New-GitBranch with -Description — attach a description for team context"
New-GitBranch -Name 'fix/typo-in-readme' `
    -Description 'Fixes several typos spotted in code review' `
    -RepoPath $MainPath

Write-Demo "Switch-GitBranch — switch to an existing branch (like git switch)"
Switch-GitBranch -Name 'feature/api-docs' -RepoPath $MainPath

# Make a commit on this branch
Add-Content -Path (Join-Path $MainPath 'README.md') -Value "`n## API Reference`n"
Save-GitCommit -All -Message 'docs: add API Reference section placeholder' -RepoPath $MainPath | Out-Null

Write-Demo "Switch-GitBranch -Create — create AND switch in one step (git switch -c)"
Switch-GitBranch -Name 'experiment/quick-test' -Create -RepoPath $MainPath
# commit something then come back
New-Item -Path (Join-Path $MainPath 'experiment.txt') -Value 'testing an idea' | Out-Null
Add-GitItem -All -RepoPath $MainPath
Save-GitCommit -Message 'wip: quick experiment' -RepoPath $MainPath | Out-Null
Switch-GitBranch -Name 'main' -RepoPath $MainPath

Write-Demo "Get-GitBranch -Include — filter branches by glob pattern"
Get-GitBranch -Include 'feature/*' -RepoPath $MainPath | Format-Table Name

Write-Demo "Get-GitBranch -Exclude — exclude branches by glob pattern"
Get-GitBranch -Exclude 'experiment/*' -RepoPath $MainPath | Format-Table Name

Write-Demo "Set-GitBranch -Description — update branch description"
Set-GitBranch -Name 'feature/api-docs' `
    -Description 'Documents the public API surface; see docs/api-reference.md' `
    -RepoPath $MainPath

Write-Demo "Get-GitBranch -IncludeDescription — show branch descriptions"
Get-GitBranch -IncludeDescription -RepoPath $MainPath | Format-Table Name, Description -AutoSize

Write-Demo "Remove-GitBranch -Force — delete a branch (force-deletion for unmerged)"
Remove-GitBranch -Name 'experiment/quick-test' -Force -RepoPath $MainPath
Write-Demo "Branches after cleanup:"
Get-GitBranch -RepoPath $MainPath | Format-Table Name -AutoSize

# Merge feature/api-docs back into main so we have merged history for later sections.
# No PowerCode.Git merge cmdlet exists, so we call git directly.
Switch-GitBranch -Name 'feature/api-docs' -RepoPath $MainPath
Switch-GitBranch -Name 'main' -RepoPath $MainPath
Invoke-Git @('-C', $MainPath, 'merge', 'feature/api-docs', '--no-ff', '-m', 'Merge feature/api-docs into main')

#endregion

# ─── SECTION 7 - TAGGING ─────────────────────────────────────────────────────

#region 07 - TAGGING  (Set-GitTag · Get-GitTag · Remove-GitTag)

Write-Section '07 · TAGGING  (Set-GitTag · Get-GitTag · Remove-GitTag)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Basics-Tagging

Write-Demo "Set-GitTag -Name — create a lightweight tag at HEAD"
Set-GitTag -Name 'v1.0.0' -RepoPath $MainPath

Write-Demo "Set-GitTag -Message — create an annotated tag (has its own author + message)"
Set-GitTag -Name 'v1.0.0-annotated' -Message 'Release v1.0.0 — first stable release' -RepoPath $MainPath

Write-Demo "Set-GitTag at a specific commit"
$OldCommit = (Get-GitLog -MaxCount 4 -RepoPath $MainPath | Select-Object -Last 1).Sha
Set-GitTag -Name 'v0.9.0' -Target $OldCommit -RepoPath $MainPath

Write-Demo "Get-GitTag — list all tags"
Get-GitTag -RepoPath $MainPath | Format-Table Name, IsAnnotated, Target -AutoSize

Write-Demo "Get-GitTag -Include — filter tags by wildcard pattern"
Get-GitTag -Include 'v1.*' -RepoPath $MainPath | Format-Table Name, IsAnnotated

Write-Demo "Remove-GitTag — delete a tag"
Remove-GitTag -Name 'v0.9.0' -RepoPath $MainPath
Write-Demo "Tags after removal:"
Get-GitTag -RepoPath $MainPath | Format-Table Name

#endregion

# ─── SECTION 8 - UNDOING CHANGES ─────────────────────────────────────────────

#region 08 - UNDOING CHANGES  (Restore-GitItem · Reset-GitHead)

Write-Section '08 · UNDOING CHANGES  (Restore-GitItem · Reset-GitHead)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Basics-Undoing-Things

# Stage the accident file so we can demonstrate unstaging it
Add-Content -Path (Join-Path $MainPath 'README.md') -Value "`nOOPS — accidental content`n"
New-Item -Path (Join-Path $MainPath 'accident.txt') -Value 'should not be here' | Out-Null
Add-GitItem -Path (Join-Path $MainPath 'accident.txt') -RepoPath $MainPath

Write-Demo "Restore-GitItem -Path — discard unstaged working-tree changes to a file"
Restore-GitItem -Path (Join-Path $MainPath 'README.md') -RepoPath $MainPath
Write-Host "  README.md is back to its committed state"

Write-Demo "Restore-GitItem -Staged — unstage a file (moves it back to working tree)"
Restore-GitItem -Path (Join-Path $MainPath 'accident.txt') -Staged -RepoPath $MainPath
Write-Demo "Status after unstage:"
(Get-GitStatus -RepoPath $MainPath).Entries | Format-Table FilePath, Status

Write-Demo "Restore-GitItem -All — discard ALL unstaged working-tree changes"
Add-Content -Path (Join-Path $MainPath 'CHANGELOG.md') -Value "`n--- more accidental edits ---`n"
Add-Content -Path (Join-Path $MainPath 'README.md') -Value "`n--- another accident ---`n"
Restore-GitItem -All -RepoPath $MainPath
Write-Host "  All working-tree changes discarded"

Write-Demo "Reset-GitHead -Path — unstage specific path (mixed reset for a single file)"
Add-GitItem -Path (Join-Path $MainPath 'accident.txt') -RepoPath $MainPath
Reset-GitHead -Path (Join-Path $MainPath 'accident.txt') -RepoPath $MainPath -Confirm:$false

Write-Demo "Reset-GitHead -Soft — move HEAD back but keep changes staged"
$SoftReset = Save-GitCommit -AllowEmpty -Message 'temp: will be soft-reset' -RepoPath $MainPath
Write-Host "  HEAD was at: $($SoftReset.ShortSha)"
Reset-GitHead -Revision 'HEAD~1' -Soft -RepoPath $MainPath -Confirm:$false
Write-Host "  HEAD after soft reset: $((Get-GitLog -MaxCount 1 -RepoPath $MainPath).ShortSha)"

Write-Demo "Reset-GitHead (mixed, default) — unstage changes, keep in working tree"
$null = Save-GitCommit -AllowEmpty -Message 'temp: will be mixed-reset' -RepoPath $MainPath
Reset-GitHead -Revision 'HEAD~1' -RepoPath $MainPath -Confirm:$false

Write-Demo "Reset-GitHead -Hard — discard staged AND working-tree changes  [DESTRUCTIVE]"
Add-Content -Path (Join-Path $MainPath 'README.md') -Value "`nDesperado content`n"
Add-GitItem -All -RepoPath $MainPath
Write-Warning "Hard reset discards all uncommitted changes — only use when you are sure."
Reset-GitHead -Revision 'HEAD' -Hard -RepoPath $MainPath -Confirm:$false
Write-Host "  Working tree restored to HEAD"

# Clean up the accident file
Remove-Item -Path (Join-Path $MainPath 'accident.txt') -ErrorAction SilentlyContinue

#endregion

# ─── SECTION 9 - CLONING ─────────────────────────────────────────────────────

#region 09 - CLONING  (Copy-GitRepository)

Write-Section '09 · CLONING  (Copy-GitRepository)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Basics-Getting-a-Git-Repository

# Create a local bare repository from the main repo to act as a "remote".
# No PowerCode.Git cmdlet wraps 'git clone --bare', so we use Invoke-Git.
$BarePath = New-TempDir 'Bare'
Remove-Item $BarePath -Recurse  # git clone --bare will re-create the directory
$TempDirs.RemoveAt($TempDirs.Count - 1)

Invoke-Git @('clone', '--bare', $MainPath, $BarePath)
$TempDirs.Add($BarePath)

$ClonePath = New-TempDir 'Clone'
Remove-Item $ClonePath -Recurse
$TempDirs.RemoveAt($TempDirs.Count - 1)

Write-Demo "Copy-GitRepository — clone a repository to a local path"
Copy-GitRepository -Url $BarePath -LocalPath $ClonePath
$TempDirs.Add($ClonePath)

Write-Host "  Clone created at: $ClonePath"
Get-GitLog -MaxCount 3 -RepoPath $ClonePath | Format-Table ShortSha, MessageShort -AutoSize

#endregion

# ─── SECTION 10 - REMOTE MANAGEMENT ──────────────────────────────────────────

#region 10 - REMOTE MANAGEMENT  (Get/New/Set/Remove-GitRemote)

Write-Section '10 · REMOTE MANAGEMENT  (Get-GitRemote · New-GitRemote · Set-GitRemote · Remove-GitRemote)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Basics-Working-with-Remotes

Write-Demo "Get-GitRemote — list remotes (name + fetch/push URLs)"
Get-GitRemote -RepoPath $ClonePath | Format-Table Name, Url -AutoSize

Write-Demo "New-GitRemote -Name -Url — add a second remote (e.g. an upstream fork)"
# Create a second bare repo to act as an 'upstream'
$UpstreamBarePath = New-TempDir 'UpstreamBare'
Remove-Item $UpstreamBarePath -Recurse
$TempDirs.RemoveAt($TempDirs.Count - 1)
Invoke-Git @('clone', '--bare', $MainPath, $UpstreamBarePath)  # no cmdlet for bare clone
$TempDirs.Add($UpstreamBarePath)

New-GitRemote -Name 'upstream' -Url $UpstreamBarePath -RepoPath $ClonePath
Write-Demo "Remotes after adding upstream:"
Get-GitRemote -RepoPath $ClonePath | Format-Table Name, Url -AutoSize

Write-Demo "Set-GitRemote -Name -NewName — rename a remote"
Set-GitRemote -Name 'upstream' -NewName 'source' -RepoPath $ClonePath
Get-GitRemote -RepoPath $ClonePath | Format-Table Name

Write-Demo "Set-GitRemote -Name -Url — update a remote's fetch URL"
Set-GitRemote -Name 'source' -Url $UpstreamBarePath -RepoPath $ClonePath
Get-GitRemote -RepoPath $ClonePath | Format-Table Name, Url -AutoSize

Write-Demo "Remove-GitRemote — remove a remote"
Remove-GitRemote -Name 'source' -RepoPath $ClonePath
Get-GitRemote -RepoPath $ClonePath | Format-Table Name

#endregion

# ─── SECTION 11 - PUSH & PULL ─────────────────────────────────────────────────

#region 11 - PUSH & PULL  (Send-GitBranch · Receive-GitBranch · Set-GitBranch tracking)

Write-Section '11 · PUSH & PULL  (Send-GitBranch · Receive-GitBranch · Set-GitBranch)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Basics-Working-with-Remotes

# Configure identity in the clone
Set-GitConfiguration -Name 'user.name' -Value 'Alice Dev' -RepoPath $ClonePath | Out-Null
Set-GitConfiguration -Name 'user.email' -Value 'alice@example.com' -RepoPath $ClonePath | Out-Null

Write-Demo "Set-GitBranch -Upstream — configure tracking for an existing branch"
Set-GitBranch -Name 'main' -Remote 'origin' -RepoPath $ClonePath

Write-Demo "Send-GitBranch -SetUpstream — push and set tracking at the same time"
Send-GitBranch -Remote 'origin' -Name 'main' -SetUpstream -Force -RepoPath $ClonePath

Write-Demo "Get-GitBranch — shows ahead/behind counts vs upstream"
Get-GitBranch -RepoPath $ClonePath | Format-Table Name, Ahead, Behind, UpstreamName -AutoSize

Write-Demo "Make a new commit in the clone, then push"
New-Item -Path (Join-Path $ClonePath 'NOTICE') -Value 'Copyright 2026 Alice Dev' | Out-Null
Add-GitItem -All -RepoPath $ClonePath
Save-GitCommit -Message 'chore: add NOTICE file' -RepoPath $ClonePath | Out-Null
Send-GitBranch -Remote 'origin' -Name 'main' -RepoPath $ClonePath
Write-Host "  Pushed NOTICE commit to origin/main"

Write-Demo "Receive-GitBranch — pull remote changes into the local branch"
# Simulate a teammate pushing to the shared remote by cloning the bare
# repo, making a commit there, and pushing it.
$BouncePath = New-TempDir 'Bounce'
Remove-Item $BouncePath -Recurse
$TempDirs.RemoveAt($TempDirs.Count - 1)
Copy-GitRepository -Url $BarePath -LocalPath $BouncePath
$TempDirs.Add($BouncePath)
Set-GitConfiguration -Name 'user.name' -Value 'Bob Reviewer' -RepoPath $BouncePath | Out-Null
Set-GitConfiguration -Name 'user.email' -Value 'bob@example.com' -RepoPath $BouncePath | Out-Null
New-Item -Path (Join-Path $BouncePath 'teammate.txt') -Value 'from Bob' | Out-Null
Add-GitItem -All -RepoPath $BouncePath
Save-GitCommit -Message 'feat: teammate commit from Bob' -RepoPath $BouncePath | Out-Null
Send-GitBranch -Remote 'origin' -Name 'main' -RepoPath $BouncePath

Receive-GitBranch -MergeStrategy FastForward -RepoPath $ClonePath
Write-Host "  Pulled Bob's commit"
Get-GitLog -MaxCount 2 -RepoPath $ClonePath | Format-Table ShortSha, AuthorName, MessageShort -AutoSize

Write-Demo "Send-GitBranch -Tags — push all local tags to the remote"
Send-GitBranch -Tags -Remote 'origin' -RepoPath $ClonePath

#endregion

# ─── SECTION 12 - GIT CONFIGURATION ─────────────────────────────────────────

#region 12 - GIT CONFIGURATION  (Get/Set/Clear-GitConfiguration)

Write-Section '12 · GIT CONFIGURATION  (Get-GitConfiguration · Set-GitConfiguration · Clear-GitConfiguration)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Customizing-Git-Git-Configuration

Write-Demo "Get-GitConfiguration -Name — read a specific config key"
Get-GitConfiguration -Name 'user.name' -RepoPath $MainPath

Write-Demo "Get-GitConfiguration without -Name — read all config entries"
Get-GitConfiguration -RepoPath $MainPath | Select-Object -First 8 | Format-Table Key, Value -AutoSize

Write-Demo "Set-GitConfiguration -Name -Value — write a config value"
Set-GitConfiguration -Name 'core.pager' -Value 'less' -RepoPath $MainPath
Set-GitConfiguration -Name 'push.default' -Value 'simple' -RepoPath $MainPath
Get-GitConfiguration -Name 'push.default' -RepoPath $MainPath

Write-Demo "Clear-GitConfiguration -Name — remove a config entry"
Clear-GitConfiguration -Name 'push.default' -RepoPath $MainPath
$Gone = Get-GitConfiguration -Name 'push.default' -RepoPath $MainPath -ErrorAction SilentlyContinue
Write-Host "  push.default after clear: $(if ($null -eq $Gone) { '(removed)' } else { $Gone })"

#endregion

# ─── SECTION 13 - REBASE ─────────────────────────────────────────────────────

#region 13 - REBASE  (Start-GitRebase · Resume-GitRebase · Stop-GitRebase)

Write-Section '13 · REBASE  (Start-GitRebase · Resume-GitRebase · Stop-GitRebase)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Branching-Rebasing

# Build a repo where a feature branch diverges from main, so we can
# demonstrate rebasing the feature onto the updated main.
$RebasePath = New-TempDir 'Rebase'
Push-Location $RebasePath
try {
    Invoke-Git @('init', '--initial-branch', 'main')
    Set-GitConfiguration -Name 'core.autocrlf' -Value 'false' | Out-Null
    Set-GitConfiguration -Name 'user.name' -Value 'Alice Dev' | Out-Null
    Set-GitConfiguration -Name 'user.email' -Value 'alice@example.com' | Out-Null

    # Shared base commit
    Set-Content -Path 'app.ps1' -Value '# App v1'
    Add-GitItem -All
    Save-GitCommit -Message 'initial: app v1' | Out-Null

    # ── Create and build the feature branch ───────────────────────────────────
    Switch-GitBranch -Name 'feature/login' -Create
    Set-Content -Path 'login.ps1' -Value 'function Login { param([string]$User) }'
    Add-GitItem -All
    Save-GitCommit -Message 'feat: add login module' | Out-Null

    Add-Content -Path 'login.ps1' -Value "`nfunction Logout { }"
    Add-GitItem -All
    Save-GitCommit -Message 'feat: add logout to login module' | Out-Null

    # ── Add commits to main AFTER the feature branched ────────────────────────
    Switch-GitBranch -Name 'main'
    Set-Content -Path 'config.ps1' -Value '$Config = @{ Timeout = 30 }'
    Add-GitItem -All
    Save-GitCommit -Message 'feat: add config module' | Out-Null

    Set-Content -Path 'logger.ps1' -Value 'function Write-Log { param([string]$Msg) }'
    Add-GitItem -All
    Save-GitCommit -Message 'feat: add logger module' | Out-Null

    # Switch to feature branch — it is now behind main
    Switch-GitBranch -Name 'feature/login'
}
finally {
    Pop-Location
}

Write-Demo "Start-GitRebase -Upstream main — replay feature commits on top of main"
$RebaseResult = Start-GitRebase -Upstream 'main' -RepoPath $RebasePath
Write-Host "  Success: $($RebaseResult.Success)  Conflicts: $($RebaseResult.HasConflicts)"

Write-Demo "Log after rebase — history is now linear"
Get-GitLog -RepoPath $RebasePath | Format-Table ShortSha, MessageShort -AutoSize

# ─── Demonstrate conflict handling with a fresh repo ─────────────────────────

Write-Demo "Conflict scenario: same line edited on both branches"

# Build a second repo where the same line is edited on both branches,
# guaranteeing a rebase conflict so we can demonstrate abort and skip.
$ConflictPath = New-TempDir 'Conflict'
Push-Location $ConflictPath
try {
    Invoke-Git @('init', '--initial-branch', 'main')
    Set-GitConfiguration -Name 'core.autocrlf' -Value 'false' | Out-Null
    Set-GitConfiguration -Name 'user.name' -Value 'Alice Dev' | Out-Null
    Set-GitConfiguration -Name 'user.email' -Value 'alice@example.com' | Out-Null

    Set-Content -Path 'version.txt' -Value 'VERSION=1.0.0'
    Add-GitItem -All
    Save-GitCommit -Message 'initial: version 1.0.0' | Out-Null

    Switch-GitBranch -Name 'release/1.1' -Create
    Set-Content -Path 'version.txt' -Value 'VERSION=1.1.0'
    Add-GitItem -All
    Save-GitCommit -Message 'bump: version to 1.1.0' | Out-Null

    Switch-GitBranch -Name 'main'
    Set-Content -Path 'version.txt' -Value 'VERSION=2.0.0'
    Add-GitItem -All
    Save-GitCommit -Message 'bump: major version to 2.0.0' | Out-Null

    Switch-GitBranch -Name 'release/1.1'
}
finally {
    Pop-Location
}

Write-Demo "Start-GitRebase — will detect a conflict on version.txt"
$ConflictResult = Start-GitRebase -Upstream 'main' -RepoPath $ConflictPath
Write-Host "  Success: $($ConflictResult.Success)  Conflicts: $($ConflictResult.HasConflicts)"

if ($ConflictResult.HasConflicts) {
    Write-Demo "Stop-GitRebase — abort the rebase and restore the original branch state"
    Stop-GitRebase -RepoPath $ConflictPath
    Write-Host "  Rebase aborted.  Branch is back to its original state."
    Get-GitLog -MaxCount 2 -RepoPath $ConflictPath | Format-Table ShortSha, MessageShort -AutoSize

    Write-Demo "Resume-GitRebase -Skip — skip a conflicting commit during rebase"
    # Start again so we can demo -Skip
    $ConflictResult2 = Start-GitRebase -Upstream 'main' -RepoPath $ConflictPath
    if ($ConflictResult2.HasConflicts) {
        Resume-GitRebase -Skip -RepoPath $ConflictPath
        Write-Host "  Skipped the conflicting commit and continued."
        Get-GitLog -MaxCount 3 -RepoPath $ConflictPath | Format-Table ShortSha, MessageShort -AutoSize
    }
}

Write-Demo "Start-GitRebase -AutoStash — stash dirty working tree before rebasing"
# Put a dirty change in the rebase (clean) repo to demonstrate -AutoStash
Add-Content -Path (Join-Path $RebasePath 'app.ps1') -Value "`n# wip change"
$AutoStashResult = Start-GitRebase -Upstream 'main' -AutoStash -RepoPath $RebasePath
Write-Host "  AutoStash result — Success: $($AutoStashResult.Success)"

#endregion

# ─── SECTION 14 - SEARCHING COMMITS ─────────────────────────────────────────

#region 14 - SEARCHING COMMITS  (Select-GitCommit)

Write-Section '14 · SEARCHING COMMITS  (Select-GitCommit)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Tools-Searching

# Build a repo with commits containing TODO/FIXME markers and two different
# authors, so we can demonstrate content search and author filtering.
$SearchPath = New-TempDir 'Search'
Push-Location $SearchPath
try {
    Invoke-Git @('init', '--initial-branch', 'main')
    Set-GitConfiguration -Name 'core.autocrlf' -Value 'false' | Out-Null
    Set-GitConfiguration -Name 'user.name' -Value 'Alice Dev' | Out-Null
    Set-GitConfiguration -Name 'user.email' -Value 'alice@example.com' | Out-Null

    Set-Content -Path 'README.md' -Value "# Project`nWelcome."
    Add-GitItem -All; Save-GitCommit -Message 'init: project readme' | Out-Null

    Set-Content -Path 'todo.txt' -Value "TODO: implement authentication"
    Add-GitItem -All; Save-GitCommit -Message 'chore: list open TODOs' | Out-Null

    Set-Content -Path 'fix.txt' -Value 'FIXME: null pointer in handler'
    Add-GitItem -All; Save-GitCommit -Message 'chore: note FIXME in handler' | Out-Null

    # Switch to a different author for a few commits (demonstrates author filtering)
    Set-GitConfiguration -Name 'user.name' -Value 'Bob Reviewer'
    Set-GitConfiguration -Name 'user.email' -Value 'bob@example.com'

    Set-Content -Path 'docs.md' -Value 'TODO: write API docs'
    Add-GitItem -All; Save-GitCommit -Message 'docs: note outstanding TODO' | Out-Null

    Set-Content -Path 'patch.txt' -Value 'Routine cleanup pass'
    Add-GitItem -All; Save-GitCommit -Message 'chore: cleanup' | Out-Null

    Set-GitConfiguration -Name 'user.name' -Value 'Alice Dev'
    Set-GitConfiguration -Name 'user.email' -Value 'alice@example.com'
}
finally {
    Pop-Location
}

Write-Demo "Select-GitCommit -Contains — find commits whose diff contains a literal string"
$TODOCommits = Select-GitCommit -Contains 'TODO' -RepoPath $SearchPath
$TODOCommits | Format-Table ShortSha, AuthorName, MessageShort -AutoSize

Write-Demo "Select-GitCommit -Match — search using a .NET regex  (like git log -G)"
Select-GitCommit -Match 'TODO|FIXME' -RepoPath $SearchPath |
    Format-Table ShortSha, AuthorName, MessageShort -AutoSize

Write-Demo "Select-GitCommit -First — stop after the first N matching commits"
Select-GitCommit -Contains 'TODO' -First 1 -RepoPath $SearchPath |
    Format-Table ShortSha, MessageShort -AutoSize

Write-Demo "Select-GitCommit -Where — arbitrary ScriptBlock predicate on `$commit"
Select-GitCommit -Where { $commit.Author.Name -eq 'Bob Reviewer' } -RepoPath $SearchPath |
    Format-Table ShortSha, AuthorName, MessageShort -AutoSize

Write-Demo "Select-GitCommit -Contains + -Where — combine text search with author filter"
Select-GitCommit -Contains 'TODO' `
    -Where { $commit.Author.Name -eq 'Bob Reviewer' } `
    -RepoPath $SearchPath |
    Format-Table ShortSha, AuthorName, MessageShort -AutoSize

Write-Demo "Select-GitCommit -Path — restrict search to a specific file path"
Select-GitCommit -Contains 'TODO' -Path 'todo.txt' -RepoPath $SearchPath |
    Format-Table ShortSha, MessageShort -AutoSize

#endregion

# ─── SECTION 15 - COMPARING TREES ────────────────────────────────────────────

#region 15 - COMPARING TREES  (Compare-GitTree)

Write-Section '15 · COMPARING TREES  (Compare-GitTree)'

Write-Demo "Compare-GitTree -Base -Compare — file-level diff between two commits"
$Commits = Get-GitLog -RepoPath $SearchPath
$BaseRef    = $Commits | Select-Object -Last 1 | Select-Object -ExpandProperty Sha
$CompareRef = $Commits | Select-Object -First 1 | Select-Object -ExpandProperty Sha

Compare-GitTree -Base $BaseRef -Compare $CompareRef -RepoPath $SearchPath |
    Format-Table NewPath, Status, LinesAdded -AutoSize

Write-Demo "Compare-GitTree -Path — filter to changes under a specific path"
Compare-GitTree -Base $BaseRef -Compare $CompareRef -Path 'docs.md' -RepoPath $SearchPath |
    Format-Table NewPath, Status, LinesAdded

Write-Demo "Compare-GitTree -Where — ScriptBlock filter on each GitDiffEntry (`$change`)"
Compare-GitTree -Base $BaseRef -Compare $CompareRef `
    -Where { $change.LinesAdded -gt 0 } `
    -RepoPath $SearchPath |
    Format-Table NewPath, Status, LinesAdded -AutoSize

Write-Demo "Compare-GitTree -Transform — project each entry to a custom shape"
$ChangedFiles = Compare-GitTree -Base $BaseRef -Compare $CompareRef `
    -Transform { [pscustomobject]@{ File = $change.NewPath; Added = $change.LinesAdded } } `
    -RepoPath $SearchPath
$ChangedFiles | Format-Table File, Added -AutoSize

Write-Demo "Compare-GitTree with branch names instead of SHAs"
Push-Location $RebasePath
try {
    Compare-GitTree -Base 'main' -Compare 'feature/login' -RepoPath $RebasePath |
        Format-Table NewPath, Status, LinesAdded -AutoSize
}
finally {
    Pop-Location
}

#endregion

# ─── SECTION 16 - WORKTREES ───────────────────────────────────────────────────

#region 16 - WORKTREES  (New/Get/Lock/Unlock/Remove-GitWorktree)

Write-Section '16 · WORKTREES  (New-GitWorktree · Get-GitWorktree · Lock/Unlock-GitWorktree · Remove-GitWorktree)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Tools-Advanced-Merging
# Worktrees let you check out multiple branches simultaneously without cloning.
# Deep dive: https://git-scm.com/docs/git-worktree

# Use the SearchPath repo (quiet history, no ongoing operations)
Write-Demo "New-GitWorktree -Name -Path — link a new worktree to a branch"
$WtPath = Join-Path ([System.IO.Path]::GetTempPath()) "PowerGitDemo_WT_$([System.Guid]::NewGuid().ToString('N')[0..7] -join '')"
$TempDirs.Add($WtPath)
New-GitWorktree -Name 'hotfix' -Path $WtPath -RepoPath $SearchPath
Write-Host "  Hotfix worktree at: $WtPath"

Write-Demo "Get-GitWorktree — list all worktrees"
Get-GitWorktree -RepoPath $SearchPath | Format-Table Path, BranchName, IsMain -AutoSize

Write-Demo "Lock-GitWorktree -Name -Reason — prevent git worktree prune from removing it"
Lock-GitWorktree -Name 'hotfix' -Reason 'Long-running CI build in progress' -RepoPath $SearchPath
Write-Host "  Worktree locked"

Write-Demo "Unlock-GitWorktree -Name — allow pruning again"
Unlock-GitWorktree -Name 'hotfix' -RepoPath $SearchPath
Write-Host "  Worktree unlocked"

Write-Demo "New-GitWorktree -Name — create a second worktree (branch is created automatically)"
$Wt2Path = Join-Path ([System.IO.Path]::GetTempPath()) "PowerGitDemo_WT2_$([System.Guid]::NewGuid().ToString('N')[0..7] -join '')"
$TempDirs.Add($Wt2Path)
New-GitWorktree -Name 'release-v2' -Path $Wt2Path -RepoPath $SearchPath

Get-GitWorktree -RepoPath $SearchPath | Format-Table Path, BranchName -AutoSize

Write-Demo "Remove-GitWorktree -Name — detach and delete a linked worktree"
Remove-GitWorktree -Name 'hotfix' -RepoPath $SearchPath
Remove-GitWorktree -Name 'release-v2' -RepoPath $SearchPath
Get-GitWorktree -RepoPath $SearchPath | Format-Table Path, BranchName -AutoSize

#endregion

# ─── SECTION 17 - LOW-LEVEL REPOSITORY ACCESS ───────────────────────────────

#region 17 - LOW-LEVEL REPOSITORY ACCESS  (Invoke-GitRepository)

Write-Section '17 · LOW-LEVEL REPOSITORY ACCESS  (Invoke-GitRepository)'
# 📖 Git Book: https://git-scm.com/book/en/v2/Git-Internals-Plumbing-and-Porcelain
#
# Invoke-GitRepository is an escape hatch into the LibGit2Sharp object model.
# $repo inside the ScriptBlock is a LibGit2Sharp.Repository instance.
#
# Important: LibGit2Sharp is loaded in an isolated AssemblyLoadContext, so type
# literals like [LibGit2Sharp.Signature] will NOT resolve outside the cmdlet.
# Any .NET objects returned from $repo are only valid *within* the ScriptBlock.

Write-Demo "Invoke-GitRepository — read HEAD branch name"
$HeadBranch = Invoke-GitRepository -RepoPath $MainPath -Action {
    $repo.Head.FriendlyName
}
Write-Host "  HEAD is: $HeadBranch"

Write-Demo "Read HEAD commit SHA and message"
Invoke-GitRepository -RepoPath $MainPath -Action {
    [pscustomobject]@{
        Sha     = $repo.Head.Tip.Sha.Substring(0, 7)
        Message = $repo.Head.Tip.MessageShort
        Author  = $repo.Head.Tip.Author.Name
    }
} | Format-Table

Write-Demo "Count total commits via repository object"
$CommitCount = Invoke-GitRepository -RepoPath $MainPath -Action {
    @($repo.Commits).Count
}
Write-Host "  Total commits: $CommitCount"

Write-Demo "Enumerate all local and remote refs"
Invoke-GitRepository -RepoPath $MainPath -Action {
    $repo.Refs | ForEach-Object {
        [pscustomobject]@{ Ref = $_.CanonicalName; Type = $_.GetType().Name }
    }
} | Format-Table Ref, Type -AutoSize

Write-Demo "List all remotes via the LibGit2Sharp network model"
Invoke-GitRepository -RepoPath $ClonePath -Action {
    $repo.Network.Remotes | ForEach-Object {
        [pscustomobject]@{ Name = $_.Name; Url = $_.Url }
    }
} | Format-Table Name, Url -AutoSize

Write-Demo "Stream commit log entries from the raw Commits collection"
Invoke-GitRepository -RepoPath $MainPath -Action {
    $repo.Commits |
        Select-Object -First 3 |
        ForEach-Object {
            [pscustomobject]@{
                Sha     = $_.Sha.Substring(0, 7)
                Author  = $_.Author.Name
                Message = $_.MessageShort
            }
        }
} | Format-Table -AutoSize

#endregion

# ─── SECTION 18 - MODULE CONFIGURATION ───────────────────────────────────────

#region 18 - MODULE CONFIGURATION  (Get/Set-GitModuleConfiguration)

Write-Section '18 · MODULE CONFIGURATION  (Get-GitModuleConfiguration · Set-GitModuleConfiguration)'
#
# Module configuration stores in-process defaults for PowerCode.Git cmdlets.
# Changes affect the current PowerShell session only — not persisted to disk.

Write-Demo "Get-GitModuleConfiguration — inspect current defaults"
Get-GitModuleConfiguration | Format-List

Write-Demo "Set-GitModuleConfiguration -LogMaxCount — change default commit limit"
Set-GitModuleConfiguration -LogMaxCount 25
Write-Host "  Get-GitLog will now default to at most 25 commits"

Write-Demo "Set-GitModuleConfiguration -DiffContext — change default context lines"
Set-GitModuleConfiguration -DiffContext 5
Write-Host "  Get-GitDiff will now show 5 context lines instead of 3"

Write-Demo "Set-GitModuleConfiguration -BranchReferenceBranch — change ahead/behind reference"
Set-GitModuleConfiguration -BranchReferenceBranch 'main'
Write-Host "  Get-GitBranch ahead/behind counts now relative to 'main'"

Write-Demo "Set-GitModuleConfiguration -BranchIncludeDescription — show descriptions by default"
Set-GitModuleConfiguration -BranchIncludeDescription

Write-Demo "Verify the new defaults are in effect"
Get-GitModuleConfiguration | Format-List

Write-Demo "Set-GitModuleConfiguration -Reset — restore all defaults"
Set-GitModuleConfiguration -Reset
Get-GitModuleConfiguration | Format-List

#endregion

# ─── SECTION 19 - PROMPT INTEGRATION ─────────────────────────────────────────

#region 19 - PROMPT INTEGRATION  (Get-GitPromptStatus)

Write-Section '19 · PROMPT INTEGRATION  (Get-GitPromptStatus)'
#
# Get-GitPromptStatus returns a GitPromptStatus object whose .ToString() / FormattedString
# produces a Powerline / Nerd Font-styled prompt segment showing:
#   • Upstream provider icon (GitHub, GitLab, Bitbucket, Azure DevOps, …)
#   • Branch name (green = clean, yellow = dirty)
#   • Ahead (↑N) / behind (↓N) counts
#   • Staged (+N), modified (~N), untracked (?N) counts
#   • Stash count (⚑N)
#
# Integrate in $PROFILE with something like:
#
#     function prompt {
#         $gitStatus = Get-GitPromptStatus -ErrorAction SilentlyContinue
#         $ps1 = 'PS '
#         if ($gitStatus) { $ps1 += "$gitStatus " }
#         $ps1 + '> '
#     }

Write-Demo "Get-GitPromptStatus on a clean repository"
$CleanStatus = Get-GitPromptStatus -RepoPath $MainPath
Write-Host "  BranchName  : $($CleanStatus.BranchName)"
Write-Host "  StagedCount : $($CleanStatus.StagedCount)"
Write-Host "  ModifiedCount: $($CleanStatus.ModifiedCount)"
Write-Host "  Formatted   : $($CleanStatus.FormattedString)"

Write-Demo "Get-GitPromptStatus on a dirty repository (staged + modified)"
Add-Content -Path (Join-Path $MainPath 'README.md') -Value "`n## About`n"
Add-GitItem -All -RepoPath $MainPath
Add-Content -Path (Join-Path $MainPath 'CHANGELOG.md') -Value "`n<!-- draft -->`n"

$DirtyStatus = Get-GitPromptStatus -RepoPath $MainPath
Write-Host "  StagedCount   : $($DirtyStatus.StagedCount)"
Write-Host "  ModifiedCount : $($DirtyStatus.ModifiedCount)"
Write-Host "  Formatted     : $($DirtyStatus.FormattedString)"

Write-Demo "Get-GitPromptStatus -NoColor — plain text without ANSI escape codes"
$NoColorStatus = Get-GitPromptStatus -NoColor -RepoPath $MainPath
Write-Host "  Formatted (no color): $($NoColorStatus.FormattedString)"

Write-Demo "Get-GitPromptStatus -HideUpstream — omit the provider icon"
$HideUp = Get-GitPromptStatus -HideUpstream -RepoPath $MainPath
Write-Host "  Formatted (no upstream): $($HideUp.FormattedString)"

Write-Demo "Get-GitPromptStatus -HideCounts — show branch name only"
$HideCounts = Get-GitPromptStatus -HideCounts -RepoPath $MainPath
Write-Host "  Formatted (no counts): $($HideCounts.FormattedString)"

Write-Demo "Get-GitPromptStatus -HideStash — suppress the stash indicator"
$HideStash = Get-GitPromptStatus -HideStash -RepoPath $MainPath
Write-Host "  Formatted (no stash): $($HideStash.FormattedString)"

Write-Demo "Using `$PROFILE integration — prompt shows git status automatically"
Write-Host @'

  Add this snippet to your $PROFILE to show git context in every prompt:

  function prompt {
      $gitStatus = Get-GitPromptStatus -ErrorAction SilentlyContinue
      $ps1 = "$(Get-Location) "
      if ($gitStatus) { $ps1 += "$gitStatus " }
      $ps1 + '> '
  }

'@

# Restore working tree for cleanup
Restore-GitItem -All -RepoPath $MainPath
Restore-GitItem -All -Staged -RepoPath $MainPath

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
Write-Host '  All done!  Everyday PowerCode.Git cmdlets demonstrated.' -ForegroundColor Green
Write-Host ''
Write-Host '  Cmdlets demonstrated:' -ForegroundColor Green
Write-Host @'
    Add-GitItem           Clear-GitConfiguration  Compare-GitTree
    Copy-GitRepository    Get-GitBranch           Get-GitCommitFile
    Get-GitConfiguration  Get-GitDiff             Get-GitLog
    Get-GitModuleConfiguration  Get-GitPromptStatus
    Get-GitRemote         Get-GitStatus           Get-GitTag
    Get-GitWorktree       Invoke-GitRepository    Lock-GitWorktree
    New-GitBranch         New-GitRemote           New-GitWorktree
    Receive-GitBranch     Remove-GitBranch        Remove-GitRemote
    Remove-GitTag         Remove-GitWorktree      Reset-GitHead
    Restore-GitItem       Resume-GitRebase        Save-GitCommit
    Select-GitCommit      Send-GitBranch          Set-GitBranch
    Set-GitConfiguration  Set-GitModuleConfiguration  Set-GitRemote
    Set-GitTag            Start-GitRebase         Stop-GitRebase
    Switch-GitBranch      Unlock-GitWorktree
'@
Write-Host ''
Write-Host '  For advanced history rewriting (Edit-GitHistory), see' -ForegroundColor DarkGray
Write-Host '  Demo-PowerCodeGit-Advanced.ps1' -ForegroundColor DarkGray

#endregion
