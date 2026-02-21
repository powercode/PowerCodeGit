#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Add-GitItem cmdlet.
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

Describe 'Add-GitItem single file' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Stages a new file so it appears in Get-GitStatus as staged' {
        $NewFile = Join-Path -Path $script:RepoPath -ChildPath 'newfile.txt'
        Set-Content -Path $NewFile -Value 'hello'

        Add-GitItem -RepoPath $script:RepoPath -Path 'newfile.txt'

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 1
    }

    It 'Stages a modified tracked file' {
        $ExistingFile = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        Set-Content -Path $ExistingFile.FullName -Value 'modified content'

        Add-GitItem -RepoPath $script:RepoPath -Path $ExistingFile.Name

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 1
    }
}

Describe 'Add-GitItem -All' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Stages all changes when -All is specified' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'a.txt') -Value 'a'
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'b.txt') -Value 'b'

        Add-GitItem -RepoPath $script:RepoPath -All

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 2
        $Status.UntrackedCount | Should -Be 0
    }
}

Describe 'Add-GitItem error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Add-GitItem -RepoPath 'C:\nonexistent\repo\path' -Path 'file.txt' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Add-GitItem -Update' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Stages only tracked modified files, not untracked files' {
        $ExistingFile = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        Set-Content -Path $ExistingFile.FullName -Value 'modified content'

        $UntrackedFile = Join-Path -Path $script:RepoPath -ChildPath 'new-untracked.txt'
        Set-Content -Path $UntrackedFile -Value 'untracked'

        Add-GitItem -RepoPath $script:RepoPath -Update

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 1
        $Status.UntrackedCount | Should -BeGreaterOrEqual 1
    }
}

Describe 'Add-GitItem -Options' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Stages all changes when Options with All=true is provided' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'options-a.txt') -Value 'a'
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'options-b.txt') -Value 'b'

        $Opts = [PowerCode.Git.Abstractions.Models.GitStageOptions]@{
            RepositoryPath = $script:RepoPath
            All            = $true
        }

        Add-GitItem -RepoPath $script:RepoPath -Options $Opts

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 2
    }
}

Describe 'Add-GitItem pipeline from GitStatusEntry' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Stages only modified files when filtered by Status' {
        # Modify an existing tracked file
        $ExistingFile = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        Set-Content -Path $ExistingFile.FullName -Value 'modified via pipeline'

        # Create an untracked file that should NOT be staged
        $UntrackedFile = Join-Path -Path $script:RepoPath -ChildPath 'untracked-pipeline.txt'
        Set-Content -Path $UntrackedFile -Value 'should stay untracked'

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.Entries | Where-Object { $_.Status -eq 'Modified' } | Add-GitItem -RepoPath $script:RepoPath

        $UpdatedStatus = Get-GitStatus -RepoPath $script:RepoPath
        # The modified tracked file should now be staged
        $StagedEntry = $UpdatedStatus.Entries | Where-Object { $_.FilePath -eq $ExistingFile.Name -and $_.StagingState -eq 'Staged' }
        $StagedEntry | Should -Not -BeNullOrEmpty
        # The untracked file should remain unstaged
        $UpdatedStatus.UntrackedCount | Should -BeGreaterOrEqual 1
    }
}
