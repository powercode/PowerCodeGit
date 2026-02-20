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
