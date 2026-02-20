#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitBranch cmdlet.
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

Describe 'Get-GitBranch basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns branch objects from a valid repository' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath)
        $Branches | Should -Not -BeNullOrEmpty
    }

    It 'Lists the main branch' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath)
        $Branches | Where-Object { $_.Name -eq 'main' } | Should -Not -BeNullOrEmpty
    }

    It 'Marks HEAD branch with IsHead' {
        $HeadBranch = Get-GitBranch -RepoPath $script:RepoPath | Where-Object { $_.IsHead }
        $HeadBranch | Should -Not -BeNullOrEmpty
        $HeadBranch.Name | Should -BeExactly 'main'
    }
}

Describe 'Get-GitBranch multiple branches' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature 2>&1 | Out-Null
            git checkout -b develop 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Lists all local branches' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath | Where-Object { -not $_.IsRemote })
        $Branches.Count | Should -BeGreaterOrEqual 3
    }

    It 'Branch has TipSha populated' {
        $Branch = Get-GitBranch -RepoPath $script:RepoPath | Select-Object -First 1
        $Branch.TipSha | Should -Match '^[0-9a-f]{40}$'
        $Branch.TipShortSha.Length | Should -Be 7
    }
}

Describe 'Get-GitBranch error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitBranch -RepoPath 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
