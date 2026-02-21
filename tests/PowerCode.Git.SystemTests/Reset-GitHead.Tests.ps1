#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Reset-GitHead cmdlet.
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

Describe 'Reset-GitHead mixed (default)' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Unstages a staged file with default mixed reset' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'staged.txt') -Value 'content'
        Add-GitItem -RepoPath $script:RepoPath -Path 'staged.txt'

        $StatusBefore = Get-GitStatus -RepoPath $script:RepoPath
        $StatusBefore.StagedCount | Should -BeGreaterOrEqual 1

        Reset-GitHead -RepoPath $script:RepoPath -Confirm:$false

        $StatusAfter = Get-GitStatus -RepoPath $script:RepoPath
        $StatusAfter.StagedCount | Should -Be 0
        $StatusAfter.UntrackedCount | Should -BeGreaterOrEqual 1
    }
}

Describe 'Reset-GitHead -Soft' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('First', 'Second')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Moves HEAD back but keeps changes staged' {
        $CommitsBefore = @(Get-GitLog -RepoPath $script:RepoPath)
        $FirstSha = ($CommitsBefore | Where-Object { $_.MessageShort -eq 'First' }).Sha

        Reset-GitHead -RepoPath $script:RepoPath -Revision $FirstSha -Soft -Confirm:$false

        $CommitsAfter = @(Get-GitLog -RepoPath $script:RepoPath)
        $CommitsAfter | Should -HaveCount 1
        $CommitsAfter[0].MessageShort | Should -BeExactly 'First'

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 1
    }
}

Describe 'Reset-GitHead -Hard' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('First', 'Second')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Moves HEAD back and discards all changes' {
        $CommitsBefore = @(Get-GitLog -RepoPath $script:RepoPath)
        $FirstSha = ($CommitsBefore | Where-Object { $_.MessageShort -eq 'First' }).Sha

        Reset-GitHead -RepoPath $script:RepoPath -Revision $FirstSha -Hard -Confirm:$false

        $CommitsAfter = @(Get-GitLog -RepoPath $script:RepoPath)
        $CommitsAfter | Should -HaveCount 1
        $CommitsAfter[0].MessageShort | Should -BeExactly 'First'

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -Be 0
        $Status.ModifiedCount | Should -Be 0
    }
}

Describe 'Reset-GitHead error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Reset-GitHead -RepoPath 'C:\nonexistent\repo\path' -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Reset-GitHead -Path (path-based reset)' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Unstages a specific file without affecting others' {
        $FileA = Join-Path -Path $script:RepoPath -ChildPath 'a.txt'
        $FileB = Join-Path -Path $script:RepoPath -ChildPath 'b.txt'
        Set-Content -Path $FileA -Value 'a'
        Set-Content -Path $FileB -Value 'b'
        Add-GitItem -RepoPath $script:RepoPath -Path 'a.txt'
        Add-GitItem -RepoPath $script:RepoPath -Path 'b.txt'

        $Before = Get-GitStatus -RepoPath $script:RepoPath
        $Before.StagedCount | Should -Be 2

        Reset-GitHead -RepoPath $script:RepoPath -Path 'a.txt' -Confirm:$false

        $After = Get-GitStatus -RepoPath $script:RepoPath
        $After.StagedCount | Should -Be 1
    }
}

Describe 'Reset-GitHead -Options' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Performs mixed reset when Options object is provided' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'staged.txt') -Value 'content'
        Add-GitItem -RepoPath $script:RepoPath -Path 'staged.txt'

        $Opts = [PowerCode.Git.Abstractions.Models.GitResetOptions]@{
            RepositoryPath = $script:RepoPath
            Mode           = [PowerCode.Git.Abstractions.Models.GitResetMode]::Mixed
        }

        Reset-GitHead -RepoPath $script:RepoPath -Options $Opts -Confirm:$false

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -Be 0
    }
}
