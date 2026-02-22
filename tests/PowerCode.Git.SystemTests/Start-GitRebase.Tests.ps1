#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Start-GitRebase cmdlet.
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

Describe 'Start-GitRebase basic rebase' {
    BeforeEach {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            # Create feature branch from initial commit
            git checkout -b feature 2>&1 | Out-Null
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'feature.txt') -Value 'feature change'
            git add . 2>&1 | Out-Null
            git commit -m 'Feature commit' 2>&1 | Out-Null

            # Add a new commit on main that does not conflict
            git checkout main 2>&1 | Out-Null
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'main.txt') -Value 'main change'
            git add . 2>&1 | Out-Null
            git commit -m 'Main commit' 2>&1 | Out-Null

            # Switch back to feature to rebase it
            git checkout feature 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterEach {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns a successful GitRebaseResult when no conflicts exist' {
        $Result = Start-GitRebase -RepoPath $script:RepoPath -Upstream main
        $Result | Should -Not -BeNullOrEmpty
        $Result.Success | Should -BeTrue
        $Result.HasConflicts | Should -BeFalse
    }

    It 'Produces a linear history after rebase' {
        Start-GitRebase -RepoPath $script:RepoPath -Upstream main | Out-Null

        Push-Location -Path $script:RepoPath
        try {
            # In a linear history, the feature commit is directly on top of main
            $Log = git log --oneline 2>&1
        }
        finally {
            Pop-Location
        }

        $Log | Should -Not -BeNullOrEmpty
        # Feature commit message should be present after rebase.
        # Use -match as a filter operator so the whole array is searched.
        ($Log -match 'Feature commit') | Should -Not -BeNullOrEmpty
    }
}

Describe 'Start-GitRebase pipeline input from Get-GitBranch' {
    BeforeEach {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature 2>&1 | Out-Null
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'feature.txt') -Value 'feature change'
            git add . 2>&1 | Out-Null
            git commit -m 'Feature commit' 2>&1 | Out-Null

            git checkout main 2>&1 | Out-Null
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'main.txt') -Value 'main change'
            git add . 2>&1 | Out-Null
            git commit -m 'Main commit' 2>&1 | Out-Null

            git checkout feature 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterEach {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Accepts a single branch piped from Get-GitBranch' {
        # Get-GitBranch uses -Pattern (not -Name) to filter; -Pattern 'main' returns only the main branch.
        $Result = Get-GitBranch -RepoPath $script:RepoPath -Pattern main | Start-GitRebase -RepoPath $script:RepoPath
        $Result | Should -Not -BeNullOrEmpty
        $Result.Success | Should -BeTrue
    }

    It 'Stops with a terminating error when multiple branches are piped' {
        # ThrowTerminatingError in EndProcessing surfaces as the inner exception type inside a
        # Pester scriptblock, so check that it throws without constraining the exception type.
        { Get-GitBranch -RepoPath $script:RepoPath | Start-GitRebase -RepoPath $script:RepoPath } |
            Should -Throw
    }
}

Describe 'Start-GitRebase with autostash' {
    BeforeEach {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature 2>&1 | Out-Null
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'feature.txt') -Value 'feature change'
            git add . 2>&1 | Out-Null
            git commit -m 'Feature commit' 2>&1 | Out-Null

            git checkout main 2>&1 | Out-Null
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'main.txt') -Value 'main change'
            git add . 2>&1 | Out-Null
            git commit -m 'Main commit' 2>&1 | Out-Null

            git checkout feature 2>&1 | Out-Null

            # Leave an uncommitted change in the working tree
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'dirty.txt') -Value 'uncommitted work'
        }
        finally {
            Pop-Location
        }
    }

    AfterEach {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Completes successfully with -AutoStash when working tree is dirty' {
        $Result = Start-GitRebase -RepoPath $script:RepoPath -Upstream main -AutoStash
        $Result | Should -Not -BeNullOrEmpty
        $Result.Success | Should -BeTrue
    }
}

Describe 'Start-GitRebase error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Produces a non-terminating error for a nonexistent upstream branch' {
        Push-Location -Path $script:RepoPath
        try {
            $Result = Start-GitRebase -Upstream nonexistent-branch -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        }
        finally {
            Pop-Location
        }
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for an invalid repository path' {
        $Result = Start-GitRebase -RepoPath $NonExistentRepoPath -Upstream main -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Start-GitRebase interactive with autosquash' {
    BeforeEach {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            # Create feature branch with a base commit and a fixup! commit targeting it
            git checkout -b feature 2>&1 | Out-Null

            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'a.txt') -Value 'base'
            git add . 2>&1 | Out-Null
            git commit -m 'Add a.txt' 2>&1 | Out-Null

            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'a.txt') -Value 'base fixed'
            git add . 2>&1 | Out-Null
            git commit -m 'fixup! Add a.txt' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterEach {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Squashes fixup! commits when -AutoSquash is specified' {
        # GIT_SEQUENCE_EDITOR=true accepts the todo list as-is; --autosquash pre-sorts
        # fixup! commits so they are automatically folded on apply.
        $env:GIT_SEQUENCE_EDITOR = 'true'
        try {
            $Result = Start-GitRebase -RepoPath $script:RepoPath -Upstream main -Interactive -AutoSquash
            $Result | Should -Not -BeNullOrEmpty
            $Result.Success | Should -BeTrue
        }
        finally {
            Remove-Item -Path Env:\GIT_SEQUENCE_EDITOR -ErrorAction SilentlyContinue
        }

        # After autosquash the two feature commits are folded into one
        Push-Location -Path $script:RepoPath
        try {
            $CommitCount = @(git log --oneline main..HEAD 2>&1).Count
        }
        finally {
            Pop-Location
        }
        $CommitCount | Should -Be 1
    }
}

Describe 'Start-GitRebase interactive with exec' {
    BeforeEach {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature 2>&1 | Out-Null
            Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'b.txt') -Value 'hello'
            git add . 2>&1 | Out-Null
            git commit -m 'Add b.txt' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterEach {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Completes successfully when -Exec specifies a passing command' {
        $env:GIT_SEQUENCE_EDITOR = 'true'
        try {
            # 'git log --oneline -1' exits 0 and is harmless
            $Result = Start-GitRebase -RepoPath $script:RepoPath -Upstream main -Interactive -Exec 'git log --oneline -1'
            $Result | Should -Not -BeNullOrEmpty
            $Result.Success | Should -BeTrue
        }
        finally {
            Remove-Item -Path Env:\GIT_SEQUENCE_EDITOR -ErrorAction SilentlyContinue
        }
    }
}
