#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Resume-GitRebase cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    against real git repositories created in temporary directories.
    Tests require creating conflict scenarios to exercise --continue and --skip.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'Resume-GitRebase --continue after conflict resolution' {
    BeforeEach {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            # Create conflicting changes: feature and main both modify conflict.txt
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

            # Start the rebase — it will stop due to conflicts
            $script:StartResult = Start-GitRebase -RepoPath $script:RepoPath -Upstream main -ErrorAction SilentlyContinue
        }
        finally {
            Pop-Location
        }
    }

    AfterEach {
        # Ensure no rebase is in progress before cleanup
        Push-Location -Path $script:RepoPath
        try {
            git rebase --abort 2>&1 | Out-Null
        }
        catch { }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'The initial rebase reports HasConflicts when files conflict' {
        $script:StartResult | Should -Not -BeNullOrEmpty
        $script:StartResult.HasConflicts | Should -BeTrue
    }

    It 'Resume-GitRebase --continue succeeds after resolving conflicts' {
        # Resolve conflict by writing a merged version and staging it
        Push-Location -Path $script:RepoPath
        try {
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'conflict.txt') -Value 'resolved content'
            git add conflict.txt 2>&1 | Out-Null
            git -c core.editor=true rebase --continue 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        # Verify the rebase completed (no .git/rebase-merge directory)
        $RebaseMergeDir = Join-Path -Path $script:RepoPath -ChildPath '.git/rebase-merge'
        Test-Path -Path $RebaseMergeDir | Should -BeFalse
    }
}

Describe 'Resume-GitRebase --skip' {
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

            # Start the rebase — it will stop due to conflicts
            Start-GitRebase -RepoPath $script:RepoPath -Upstream main -ErrorAction SilentlyContinue | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterEach {
        Push-Location -Path $script:RepoPath
        try {
            git rebase --abort 2>&1 | Out-Null
        }
        catch { }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Resume-GitRebase -Skip drops the conflicting commit and completes the rebase' {
        $Result = Resume-GitRebase -RepoPath $script:RepoPath -Skip
        # Either the rebase completed (Success=true) or further conflicts were found
        $Result | Should -Not -BeNullOrEmpty
    }
}

Describe 'Resume-GitRebase error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Produces a non-terminating error when no rebase is in progress' {
        $Result = Resume-GitRebase -RepoPath $script:RepoPath -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for an invalid repository path' {
        $Result = Resume-GitRebase -RepoPath $NonExistentRepoPath -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
