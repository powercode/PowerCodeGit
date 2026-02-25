#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Remove-GitTag cmdlet.
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

Describe 'Remove-GitTag basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git tag v1.0.0 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Deletes an existing tag' {
        Remove-GitTag -RepoPath $script:RepoPath -Name 'v1.0.0' -Confirm:$false

        $Tags = @(Get-GitTag -RepoPath $script:RepoPath)
        $Deleted = $Tags | Where-Object { $_.Name -eq 'v1.0.0' }
        $Deleted | Should -BeNullOrEmpty
    }
}

Describe 'Remove-GitTag piped from Get-GitTag' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git tag v0.1.0 2>&1 | Out-Null
            git tag v0.2.0 2>&1 | Out-Null
            git tag v1.0.0 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Deletes tags piped from Get-GitTag' {
        Get-GitTag -RepoPath $script:RepoPath -Include 'v0.*' | Remove-GitTag -RepoPath $script:RepoPath -Confirm:$false

        $Tags = @(Get-GitTag -RepoPath $script:RepoPath)
        $Tags | Where-Object { $_.Name -like 'v0.*' } | Should -BeNullOrEmpty
        $Tags | Where-Object { $_.Name -eq 'v1.0.0' } | Should -Not -BeNullOrEmpty
    }
}

Describe 'Remove-GitTag error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Produces a non-terminating error for a nonexistent tag' {
        $Result = Remove-GitTag -RepoPath $script:RepoPath -Name 'nonexistent' -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for an invalid path' {
        $Result = Remove-GitTag -RepoPath $NonExistentRepoPath -Name 'v1.0.0' -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Remove-GitTag -Options' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git tag v2.0.0 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Deletes a tag via -Options parameter set' {
        $Options = [PowerCode.Git.Abstractions.Models.GitTagDeleteOptions]@{
            RepositoryPath = $script:RepoPath
            Name           = 'v2.0.0'
        }

        Remove-GitTag -Options $Options -Confirm:$false

        $Tags = @(Get-GitTag -RepoPath $script:RepoPath)
        $Tags | Where-Object { $_.Name -eq 'v2.0.0' } | Should -BeNullOrEmpty
    }
}
