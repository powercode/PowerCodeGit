#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitDiff cmdlet.
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

Describe 'Get-GitDiff no changes' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns empty when there are no changes' {
        $Diffs = @(Get-GitDiff -RepoPath $script:RepoPath)
        $Diffs | Should -HaveCount 0
    }
}

Describe 'Get-GitDiff unstaged changes' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        $ExistingFile = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        Set-Content -Path $ExistingFile.FullName -Value 'modified content'
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns diff entries for modified files' {
        $Diffs = @(Get-GitDiff -RepoPath $script:RepoPath)
        $Diffs | Should -Not -BeNullOrEmpty
    }

    It 'Diff entry has LinesAdded or LinesDeleted populated' {
        $Diff = Get-GitDiff -RepoPath $script:RepoPath | Select-Object -First 1
        ($Diff.LinesAdded + $Diff.LinesDeleted) | Should -BeGreaterThan 0
    }

    It 'Diff entry has Patch content' {
        $Diff = Get-GitDiff -RepoPath $script:RepoPath | Select-Object -First 1
        $Diff.Patch | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitDiff -Staged' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            Set-Content -Path 'new-staged.txt' -Value 'staged content'
            git add new-staged.txt 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns staged diff entries with -Staged switch' {
        $Diffs = @(Get-GitDiff -RepoPath $script:RepoPath -Staged)
        $Diffs | Should -Not -BeNullOrEmpty
    }

    It 'Shows the correct status for staged new file' {
        $Diff = Get-GitDiff -RepoPath $script:RepoPath -Staged | Where-Object { $_.NewPath -eq 'new-staged.txt' }
        $Diff.Status | Should -Be 'Added'
    }
}

Describe 'Get-GitDiff error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitDiff -RepoPath 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
