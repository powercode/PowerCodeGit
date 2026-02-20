#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Switch-GitBranch cmdlet.
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

Describe 'Switch-GitBranch basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Switches to the specified branch and returns branch info' {
        $Result = Switch-GitBranch -RepoPath $script:RepoPath -Name 'feature'
        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'feature'
        $Result.IsHead | Should -BeTrue
    }

    It 'HEAD points to the new branch after switching' {
        Switch-GitBranch -RepoPath $script:RepoPath -Name 'main' | Out-Null

        Push-Location -Path $script:RepoPath
        try {
            $CurrentBranch = git rev-parse --abbrev-ref HEAD 2>&1
        }
        finally {
            Pop-Location
        }

        $CurrentBranch | Should -BeExactly 'main'
    }
}

Describe 'Switch-GitBranch error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Produces a non-terminating error for a nonexistent branch' {
        $Result = Switch-GitBranch -RepoPath $script:RepoPath -Name 'nonexistent' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for an invalid path' {
        $Result = Switch-GitBranch -RepoPath 'C:\nonexistent\repo\path' -Name 'main' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
