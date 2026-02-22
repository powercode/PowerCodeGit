#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Remove-GitBranch cmdlet.
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

Describe 'Remove-GitBranch basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature-to-delete 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Deletes a merged branch' {
        Remove-GitBranch -RepoPath $script:RepoPath -Name 'feature-to-delete' -Confirm:$false

        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath | Where-Object { -not $_.IsRemote })
        $Deleted = $Branches | Where-Object { $_.Name -eq 'feature-to-delete' }
        $Deleted | Should -BeNullOrEmpty
    }
}

Describe 'Remove-GitBranch -Force' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b unmerged-feature 2>&1 | Out-Null
            Set-Content -Path 'unmerged.txt' -Value 'content'
            git add . 2>&1 | Out-Null
            git commit -m 'Unmerged work' 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Force-deletes an unmerged branch' {
        Remove-GitBranch -RepoPath $script:RepoPath -Name 'unmerged-feature' -Force -Confirm:$false

        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath | Where-Object { -not $_.IsRemote })
        $Deleted = $Branches | Where-Object { $_.Name -eq 'unmerged-feature' }
        $Deleted | Should -BeNullOrEmpty
    }
}

Describe 'Remove-GitBranch error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Produces a non-terminating error when deleting the current branch' {
        $Result = Remove-GitBranch -RepoPath $script:RepoPath -Name 'main' -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for a nonexistent branch' {
        $Result = Remove-GitBranch -RepoPath $script:RepoPath -Name 'nonexistent' -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for an invalid path' {
        $Result = Remove-GitBranch -RepoPath $NonExistentRepoPath -Name 'test' -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Remove-GitBranch -Options' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature-opts 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Deletes a branch via -Options parameter set' {
        $Options = [PowerCode.Git.Abstractions.Models.GitBranchDeleteOptions]@{
            RepositoryPath = $script:RepoPath
            Name           = 'feature-opts'
        }

        Remove-GitBranch -Options $Options -Confirm:$false

        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath | Where-Object { -not $_.IsRemote })
        $Branches | Where-Object { $_.Name -eq 'feature-opts' } | Should -BeNullOrEmpty
    }
}
