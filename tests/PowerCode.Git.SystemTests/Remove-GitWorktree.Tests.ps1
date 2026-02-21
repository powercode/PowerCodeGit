#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for Remove-GitWorktree, Lock-GitWorktree, and Unlock-GitWorktree cmdlets.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    against real git repositories created in temporary directories.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'Remove-GitWorktree basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_rmwt_$([System.Guid]::NewGuid().ToString('N'))"

        New-GitWorktree -RepoPath $script:RepoPath -Name 'remove-test' -Path $script:WorktreePath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Removes an existing worktree' {
        Remove-GitWorktree -RepoPath $script:RepoPath -Name 'remove-test' -Confirm:$false

        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktrees | Where-Object { $_.Name -eq 'remove-test' } | Should -BeNullOrEmpty
    }
}

Describe 'Remove-GitWorktree with -Force on locked worktree' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_forcermwt_$([System.Guid]::NewGuid().ToString('N'))"

        New-GitWorktree -RepoPath $script:RepoPath -Name 'force-remove-test' -Path $script:WorktreePath -Locked
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Removes a locked worktree with -Force' {
        Remove-GitWorktree -RepoPath $script:RepoPath -Name 'force-remove-test' -Force -Confirm:$false

        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktrees | Where-Object { $_.Name -eq 'force-remove-test' } | Should -BeNullOrEmpty
    }
}

Describe 'Lock-GitWorktree and Unlock-GitWorktree' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_lockwt_$([System.Guid]::NewGuid().ToString('N'))"

        New-GitWorktree -RepoPath $script:RepoPath -Name 'lock-test' -Path $script:WorktreePath
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree unlock $script:WorktreePath 2>&1 | Out-Null
            git worktree remove $script:WorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Locks a worktree' {
        Lock-GitWorktree -RepoPath $script:RepoPath -Name 'lock-test' -Reason 'Testing'

        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Locked = $Worktrees | Where-Object { $_.Name -eq 'lock-test' }
        $Locked.IsLocked | Should -BeTrue
    }

    It 'Unlocks a locked worktree' {
        Unlock-GitWorktree -RepoPath $script:RepoPath -Name 'lock-test'

        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Unlocked = $Worktrees | Where-Object { $_.Name -eq 'lock-test' }
        $Unlocked.IsLocked | Should -BeFalse
    }
}

Describe 'Remove-GitWorktree -WhatIf' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_whatifrmwt_$([System.Guid]::NewGuid().ToString('N'))"

        New-GitWorktree -RepoPath $script:RepoPath -Name 'whatif-remove' -Path $script:WorktreePath
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree remove $script:WorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Does not remove worktree with -WhatIf' {
        Remove-GitWorktree -RepoPath $script:RepoPath -Name 'whatif-remove' -WhatIf

        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktrees | Where-Object { $_.Name -eq 'whatif-remove' } | Should -Not -BeNullOrEmpty
    }
}
