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

Describe 'New-GitBranch -StartPoint' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('First commit', 'Second commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Creates a branch at the specified commit SHA' {
        Push-Location -Path $script:RepoPath
        try {
            # Get the first commit SHA
            $FirstSha = git log --format='%H' | Select-Object -Last 1
        }
        finally {
            Pop-Location
        }

        $Result = New-GitBranch -RepoPath $script:RepoPath -Name 'at-first' -StartPoint $FirstSha
        $Result | Should -Not -BeNullOrEmpty
        $Result.TipSha | Should -BeExactly $FirstSha
    }
}

Describe 'New-GitBranch -Force' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Overwrites an existing branch when -Force is specified' {
        # Create the branch once
        New-GitBranch -RepoPath $script:RepoPath -Name 'overwritable' | Out-Null

        # Go back to main and make a new commit
        Push-Location -Path $script:RepoPath
        try {
            git checkout main 2>&1 | Out-Null
            Set-Content -Path 'newfile.txt' -Value 'new'
            git add . 2>&1 | Out-Null
            git commit -m 'Extra commit' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        # Force-reset the branch to the new HEAD
        $Result = New-GitBranch -RepoPath $script:RepoPath -Name 'overwritable' -Force
        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'overwritable'
    }
}

Describe 'New-GitBranch -Options catch-all' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Accepts a GitBranchCreateOptions object via -Options' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitBranchCreateOptions]@{
            RepositoryPath = $script:RepoPath
            Name           = 'via-options'
        }

        $Result = New-GitBranch -Options $Opts
        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'via-options'
    }
}
