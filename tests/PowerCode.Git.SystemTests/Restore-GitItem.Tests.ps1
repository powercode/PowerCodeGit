#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Restore-GitItem cmdlet.
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

Describe 'Restore-GitItem -Path' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Discards working-tree changes so the file matches HEAD' {
        $File = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        $OriginalContent = Get-Content -Path $File.FullName -Raw
        Set-Content -Path $File.FullName -Value 'modified content'

        Restore-GitItem -RepoPath $script:RepoPath -Path $File.Name -Confirm:$false

        $RestoredContent = Get-Content -Path $File.FullName -Raw
        $RestoredContent | Should -Be $OriginalContent
    }
}

Describe 'Restore-GitItem -All' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Discards all working-tree changes' {
        $Files = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt'

        foreach ($File in $Files)
        {
            Set-Content -Path $File.FullName -Value 'modified'
        }

        Restore-GitItem -RepoPath $script:RepoPath -All -Confirm:$false

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.ModifiedCount | Should -Be 0
    }
}

Describe 'Restore-GitItem -Path -Staged' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Unstages a staged file so StagedCount drops to zero' {
        $File = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        Set-Content -Path $File.FullName -Value 'staged change'
        Add-GitItem -RepoPath $script:RepoPath -Path $File.Name

        $Before = Get-GitStatus -RepoPath $script:RepoPath
        $Before.StagedCount | Should -BeGreaterOrEqual 1

        Restore-GitItem -RepoPath $script:RepoPath -Path $File.Name -Staged -Confirm:$false

        $After = Get-GitStatus -RepoPath $script:RepoPath
        $After.StagedCount | Should -Be 0
    }
}

Describe 'Restore-GitItem -Hunk pipeline' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Reverts individual hunks piped from Get-GitDiff -Hunk' {
        $File = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        $OriginalContent = Get-Content -Path $File.FullName -Raw
        Add-Content -Path $File.FullName -Value 'extra line'

        $HunksBefore = Get-GitDiff -RepoPath $script:RepoPath -Hunk
        $HunksBefore | Should -Not -BeNullOrEmpty

        $HunksBefore | Restore-GitItem -RepoPath $script:RepoPath -Confirm:$false

        $RestoredContent = Get-Content -Path $File.FullName -Raw
        $RestoredContent | Should -Be $OriginalContent
    }
}

Describe 'Restore-GitItem InputObject pipeline' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Restores files piped from Get-GitStatus Entries via FilePath property binding' {
        $Files = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt'

        foreach ($File in $Files)
        {
            Set-Content -Path $File.FullName -Value 'pipeline modified'
        }

        # GitStatusEntry exposes a FilePath property, which binds to the Path
        # parameter via its [Alias("FilePath")] and ValueFromPipelineByPropertyName.
        Get-GitStatus -RepoPath $script:RepoPath |
            Select-Object -ExpandProperty Entries |
            Restore-GitItem -RepoPath $script:RepoPath -Confirm:$false

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.ModifiedCount | Should -Be 0
    }
}

Describe 'Restore-GitItem error handling' {
    It 'Produces a non-terminating error for an invalid repository path' {
        $Result = Restore-GitItem -RepoPath $NonExistentRepoPath -Path 'file.txt' `
            -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}