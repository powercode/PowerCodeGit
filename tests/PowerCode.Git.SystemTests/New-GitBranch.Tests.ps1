#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the New-GitBranch cmdlet.
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

Describe 'New-GitBranch basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Creates a new branch and returns GitBranchInfo' {
        $Result = New-GitBranch -RepoPath $script:RepoPath -Name 'feature/test'

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'feature/test'
        $Result.IsHead | Should -BeTrue
    }

    It 'The new branch appears in Get-GitBranch' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath)
        $FeatureBranch = $Branches | Where-Object { $_.Name -eq 'feature/test' }
        $FeatureBranch | Should -Not -BeNullOrEmpty
    }

    It 'HEAD points to the new branch after creation' {
        Push-Location -Path $script:RepoPath
        try {
            $CurrentBranch = git rev-parse --abbrev-ref HEAD 2>&1
        }
        finally {
            Pop-Location
        }

        $CurrentBranch | Should -BeExactly 'feature/test'
    }

    It 'Creates a branch with the same commit SHA as the source' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath)
        $MainBranch = $Branches | Where-Object { $_.Name -eq 'main' }
        $FeatureBranch = $Branches | Where-Object { $_.Name -eq 'feature/test' }
        $FeatureBranch.Sha | Should -BeExactly $MainBranch.Sha
    }
}

Describe 'New-GitBranch error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Produces a non-terminating error when the branch already exists' {
        $Result = New-GitBranch -RepoPath $script:RepoPath -Name 'main' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for an invalid path' {
        $Result = New-GitBranch -RepoPath 'C:\nonexistent\repo\path' -Name 'test' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
