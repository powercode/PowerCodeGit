#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitStatus cmdlet.
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

Describe 'Get-GitStatus clean repository' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns a GitStatusResult object' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status | Should -Not -BeNullOrEmpty
    }

    It 'Reports zero counts on a clean repository' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -Be 0
        $Status.ModifiedCount | Should -Be 0
        $Status.UntrackedCount | Should -Be 0
    }

    It 'Reports the current branch' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.CurrentBranch | Should -BeExactly 'main'
    }
}

Describe 'Get-GitStatus with changes' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Reports untracked files' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'untracked.txt') -Value 'new'
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.UntrackedCount | Should -BeGreaterOrEqual 1
    }

    It 'Reports modified files' {
        $ExistingFile = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        Set-Content -Path $ExistingFile.FullName -Value 'modified content'
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.ModifiedCount | Should -BeGreaterOrEqual 1
    }

    It 'Reports staged files' {
        Push-Location -Path $script:RepoPath
        try {
            Set-Content -Path 'staged.txt' -Value 'staged content'
            git add staged.txt 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 1
    }
}

Describe 'Get-GitStatus error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitStatus -RepoPath 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
