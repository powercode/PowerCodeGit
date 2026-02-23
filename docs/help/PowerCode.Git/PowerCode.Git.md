---
document type: module
Help Version: 1.0.0.0
HelpInfoUri: 
Locale: en-US
Module Guid: 86ed19db-80a7-48c4-a04e-1125b82f7cce
Module Name: PowerCode.Git
ms.date: 02-23-2026
PlatyPS schema version: 2024-05-01
title: PowerCode.Git Module
---

# PowerCode.Git Module

## Description

A PowerShell module for Git that provides discoverability through standard PowerShell noun-verb naming,
rich pipeline support for composing commands, and tab completion for parameters such as branch names, tags, and remotes.

## PowerCode.Git

### [Add-GitItem](Add-GitItem.md)

Stages files in the working tree for the next commit, equivalent to git add.

### [Copy-GitRepository](Copy-GitRepository.md)

Clones a remote git repository to a local directory, equivalent to git clone.

### [Get-GitBranch](Get-GitBranch.md)

Lists branches in a git repository, equivalent to git branch.

### [Get-GitCommitFile](Get-GitCommitFile.md)

Lists the files changed by a specific commit, comparing it against its parent.

### [Get-GitDiff](Get-GitDiff.md)

Shows changes between the working tree, index, or commits, equivalent to git diff.

### [Get-GitLog](Get-GitLog.md)

Retrieves commit history from a git repository, equivalent to git log.

### [Get-GitStatus](Get-GitStatus.md)

Retrieves the working tree and index status of a git repository, equivalent to git status.

### [Get-GitTag](Get-GitTag.md)

Lists tags in a git repository, equivalent to git tag -l.

### [Get-GitWorktree](Get-GitWorktree.md)

Lists worktrees in a git repository, equivalent to git worktree list.

### [Lock-GitWorktree](Lock-GitWorktree.md)

Locks a worktree to prevent it from being pruned, equivalent to git worktree lock.

### [New-GitBranch](New-GitBranch.md)

Creates a new branch in a git repository, equivalent to git branch.

### [New-GitWorktree](New-GitWorktree.md)

Creates a new worktree in a git repository, equivalent to git worktree add.

### [Receive-GitBranch](Receive-GitBranch.md)

Pulls remote changes into the current branch, equivalent to git pull.

### [Remove-GitBranch](Remove-GitBranch.md)

Deletes a branch from a git repository, equivalent to git branch -d.

### [Remove-GitWorktree](Remove-GitWorktree.md)

Removes a linked worktree from a git repository, equivalent to git worktree remove.

### [Reset-GitHead](Reset-GitHead.md)

Resets the current HEAD to a specified state, equivalent to git reset.

### [Restore-GitItem](Restore-GitItem.md)

Discards working-tree changes or unstages index changes for files in a git repository, equivalent to git restore.

### [Resume-GitRebase](Resume-GitRebase.md)

Resumes a paused rebase after resolving conflicts or skips the current conflicting commit.

### [Save-GitCommit](Save-GitCommit.md)

Creates a commit from the current index, equivalent to git commit.

### [Send-GitBranch](Send-GitBranch.md)

Pushes a branch to a remote repository, equivalent to git push.

### [Set-GitTag](Set-GitTag.md)

Creates a git tag, equivalent to git tag [-a] [-f] <name> [<target>].

### [Start-GitRebase](Start-GitRebase.md)

Starts a rebase operation, replaying commits from the current branch on top of the specified upstream branch.

### [Stop-GitRebase](Stop-GitRebase.md)

Aborts the current rebase operation and restores the branch to its original state.

### [Switch-GitBranch](Switch-GitBranch.md)

Switches the current branch of a git repository, equivalent to git switch.

### [Unlock-GitWorktree](Unlock-GitWorktree.md)

Unlocks a previously locked worktree, equivalent to git worktree unlock.

