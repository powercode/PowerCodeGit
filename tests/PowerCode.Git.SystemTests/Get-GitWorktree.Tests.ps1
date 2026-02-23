#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitWorktree cmdlet.
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

Describe 'Get-GitWorktree basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns empty when no worktrees exist' {
        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktrees.Count | Should -Be 0
    }

    It 'Works without parameters when inside a repository' {
        Push-Location -Path $script:RepoPath
        try {
            $Worktrees = @(Get-GitWorktree)
            $Worktrees.Count | Should -Be 0
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Get-GitWorktree with worktrees' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_wt_$([System.Guid]::NewGuid().ToString('N'))"

        Push-Location -Path $script:RepoPath
        try {
            git worktree add $script:WorktreePath -b test-wt 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
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

    It 'Lists the added worktree' {
        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktrees.Count | Should -BeGreaterOrEqual 1
    }

    It 'Worktree has Name populated' {
        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktree = $Worktrees | Select-Object -First 1
        $Worktree.Name | Should -Not -BeNullOrEmpty
    }

    It 'Worktree has Path populated' {
        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktree = $Worktrees | Select-Object -First 1
        $Worktree.Path | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitWorktree error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitWorktree -RepoPath $NonExistentRepoPath -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
