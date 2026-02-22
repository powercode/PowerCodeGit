#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Stop-GitRebase cmdlet.
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

Describe 'Stop-GitRebase aborts an in-progress rebase' {
    BeforeEach {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'conflict.txt') -Value 'base content'
            git add . 2>&1 | Out-Null
            git commit -m 'Add conflict.txt' 2>&1 | Out-Null

            git checkout -b feature 2>&1 | Out-Null
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'conflict.txt') -Value 'feature version'
            git add . 2>&1 | Out-Null
            git commit -m 'Feature modifies conflict.txt' 2>&1 | Out-Null

            git checkout main 2>&1 | Out-Null
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'conflict.txt') -Value 'main version'
            git add . 2>&1 | Out-Null
            git commit -m 'Main modifies conflict.txt' 2>&1 | Out-Null

            git checkout feature 2>&1 | Out-Null
            $script:TipBeforeRebase = git rev-parse HEAD 2>&1

            # Start the rebase — it will stop due to conflicts
            Start-GitRebase -RepoPath $script:RepoPath -Upstream main -ErrorAction SilentlyContinue | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterEach {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Completes without error' {
        { Stop-GitRebase -RepoPath $script:RepoPath -Confirm:$false } | Should -Not -Throw
    }

    It 'Restores HEAD to the original commit' {
        Stop-GitRebase -RepoPath $script:RepoPath -Confirm:$false

        Push-Location -Path $script:RepoPath
        try {
            $TipAfterAbort = git rev-parse HEAD 2>&1
        }
        finally {
            Pop-Location
        }

        $TipAfterAbort | Should -BeExactly $script:TipBeforeRebase
    }

    It 'Removes the rebase-merge state directory after abort' {
        Stop-GitRebase -RepoPath $script:RepoPath -Confirm:$false

        $RebaseMergeDir = Join-Path -Path $script:RepoPath -ChildPath '.git/rebase-merge'
        Test-Path -Path $RebaseMergeDir | Should -BeFalse
    }

    It 'Restores the correct branch name' {
        Stop-GitRebase -RepoPath $script:RepoPath -Confirm:$false

        Push-Location -Path $script:RepoPath
        try {
            $BranchName = git rev-parse --abbrev-ref HEAD 2>&1
        }
        finally {
            Pop-Location
        }

        $BranchName | Should -BeExactly 'feature'
    }
}

Describe 'Stop-GitRebase error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Produces a non-terminating error when no rebase is in progress' {
        Stop-GitRebase -RepoPath $script:RepoPath -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for an invalid repository path' {
        Stop-GitRebase -RepoPath $NonExistentRepoPath -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
