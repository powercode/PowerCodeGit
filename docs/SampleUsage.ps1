#Requires -Modules PowerCode.Git

<#
.SYNOPSIS
    Introduction to the PowerCode.Git module for new users.

.DESCRIPTION
    This script demonstrates the most common PowerCode.Git workflows
    step-by-step.  It is meant to be read and run section-by-section
    in a PowerShell prompt — NOT executed all at once, because some
    commands are destructive (reset, rebase, force-delete).

    Every cmdlet and parameter used below is taken directly from the
    official help docs shipped with the module.
#>

# ============================================================================
# 1. CLONE A REPOSITORY
# ============================================================================
# Copy-GitRepository wraps 'git clone'.
# Provide a URL and, optionally, a target directory.

Copy-GitRepository -Url https://github.com/user/repo.git
Copy-GitRepository -Url https://github.com/user/repo.git -LocalPath ./my-repo

# ============================================================================
# 2. INSPECT REPOSITORY STATUS
# ============================================================================
# Get-GitStatus wraps 'git status'.
# It returns structured objects describing tracked, untracked, and staged files.

Get-GitStatus
Get-GitStatus -IncludeIgnored          # also show .gitignore'd files

# ============================================================================
# 3. BROWSE BRANCHES
# ============================================================================
# Get-GitBranch wraps 'git branch'.
# By default it shows local branches; add -Remote or -All for more.

Get-GitBranch                           # local branches
Get-GitBranch -Remote                   # remote-tracking branches
Get-GitBranch -All                      # both local and remote
Get-GitBranch -Include 'feature/*'      # only branches matching a pattern
Get-GitBranch -Exclude 'temp/*'         # exclude branches matching a pattern

# ============================================================================
# 4. CREATE AND SWITCH BRANCHES
# ============================================================================
# New-GitBranch wraps 'git branch <name>'.
# Switch-GitBranch wraps 'git switch'.

New-GitBranch -Name feature/my-feature
Switch-GitBranch -Name feature/my-feature

# Create and switch in one step (like 'git switch -c'):
Switch-GitBranch -Name feature/quick -Create

# You can also create a branch from a specific start point:
New-GitBranch -Name hotfix/p1 -StartPoint v2.0.0

# ============================================================================
# 5. STAGE CHANGES
# ============================================================================
# Add-GitItem wraps 'git add'.

Add-GitItem -Path newfile.txt           # stage a single file
Add-GitItem -All                        # stage everything (new, modified, deleted)
Add-GitItem -Update                     # stage only already-tracked files

# Pipeline: stage only modified files reported by Get-GitStatus
Get-GitStatus |
    Select-Object -ExpandProperty Entries |
    Where-Object Status -EQ Modified |
    Add-GitItem

# ============================================================================
# 6. VIEW DIFFS
# ============================================================================
# Get-GitDiff wraps 'git diff'.  Use -Staged for the index diff,
# -Hunk for individual hunk objects you can pipe into Add-GitItem.

Get-GitDiff                             # unstaged changes
Get-GitDiff -Staged                     # changes already staged
Get-GitDiff -Hunk                       # return individual hunk objects

# Selectively stage only C# file hunks:
Get-GitDiff -Hunk |
    Where-Object { $_.FilePath -like '*.cs' } |
    Add-GitItem

# Stage hunks that contain added lines:
Get-GitDiff -Hunk |
    Where-Object { $_.Lines | Where-Object Kind -eq 'Added' } |
    Add-GitItem

# Show diff with no surrounding context lines:
Get-GitDiff -Context 0

# ============================================================================
# 7. COMMIT
# ============================================================================
# Save-GitCommit wraps 'git commit'.

Save-GitCommit -Message 'Add new feature'
Save-GitCommit -Amend                   # amend the previous commit message/content
Save-GitCommit -All -Message 'Track all changes'   # stage tracked + commit

# ============================================================================
# 8. VIEW HISTORY
# ============================================================================
# Get-GitLog wraps 'git log'.  It returns commit objects.

Get-GitLog -MaxCount 5                  # last 5 commits
Get-GitLog -Author 'Alice'             # filter by author

# See which files were changed in the most recent commit:
Get-GitLog -MaxCount 1 | Get-GitCommitFile

# Get detailed diff hunks for a specific commit:
Get-GitCommitFile -Commit abc1234 -Hunk

# ============================================================================
# 9. PUSH AND PULL
# ============================================================================
# Send-GitBranch  wraps 'git push'.
# Receive-GitBranch wraps 'git pull'.

Send-GitBranch                                      # push current branch
Send-GitBranch -Remote origin -SetUpstream           # push and set upstream

Receive-GitBranch                                    # pull from remote
Receive-GitBranch -MergeStrategy FastForward -Prune  # fast-forward only, prune stale remotes

# ============================================================================
# 10. TAGGING
# ============================================================================
# Set-GitTag wraps 'git tag'.
# Get-GitTag wraps 'git tag -l'.

Set-GitTag -Name v1.0.0                              # lightweight tag at HEAD
Set-GitTag -Name v2.0.0 -Message 'Release v2.0.0'   # annotated tag

Get-GitTag                               # list all tags
Get-GitTag -Pattern 'v1.*'              # filter tags by pattern

Send-GitBranch -Tags                     # push all tags to remote

# ============================================================================
# 11. UNDO / RESTORE
# ============================================================================
# Restore-GitItem wraps 'git restore'.
# Reset-GitHead   wraps 'git reset'.

Restore-GitItem -Path ./file.txt                     # discard working-tree changes
Restore-GitItem -All                                 # discard ALL working-tree changes
Restore-GitItem -Path ./file.txt -Staged             # unstage a file (keep changes)

# Unstage via reset (equivalent):
Reset-GitHead -Path file.txt

# Hard reset to a previous commit (DESTRUCTIVE — discards changes):
Reset-GitHead -Revision HEAD~1 -Hard

# ============================================================================
# 12. ADVANCED: WORKTREES
# ============================================================================
# Worktrees let you check out multiple branches simultaneously in separate
# directories without multiple clones.

New-GitWorktree -Name feature -Path ../feature-worktree
Get-GitWorktree                          # list all worktrees
Lock-GitWorktree -Name feature -Reason 'Work in progress'
Unlock-GitWorktree -Name feature
Remove-GitWorktree -Name feature         # clean up when done

# Create a worktree for an existing branch via pipeline:
Get-GitBranch -Include develop | New-GitWorktree

# ============================================================================
# 13. ADVANCED: REBASE
# ============================================================================
# Start-GitRebase  wraps 'git rebase'.
# Resume-GitRebase wraps 'git rebase --continue / --skip'.
# Stop-GitRebase   wraps 'git rebase --abort'.

Start-GitRebase -Upstream main                        # replay commits onto main
Start-GitRebase -Upstream main -AutoStash             # stash uncommitted changes first

# If conflicts arise during rebase:
Resume-GitRebase                          # continue after resolving conflicts
Resume-GitRebase -Skip                    # skip the conflicting commit
Stop-GitRebase                            # abort and restore original state

# ============================================================================
# 14. CLEANUP
# ============================================================================
# Remove-GitBranch wraps 'git branch -d'.

Remove-GitBranch -Name feature/my-feature             # delete a merged branch
Remove-GitBranch -Name feature/my-feature -Force       # force-delete an unmerged branch
