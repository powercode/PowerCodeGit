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

Describe 'Restore-GitItem restores only hunks filtered by Lines content' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        # Create two files: one with a metadata line, one without.
        $MetadataFile = Join-Path -Path $script:RepoPath -ChildPath 'metadata.txt'
        $NormalFile = Join-Path -Path $script:RepoPath -ChildPath 'normal.txt'

        Set-Content -Path $MetadataFile -Value "header`nms.date: 01-01-2026`ndescription: test file`nfooter"
        Set-Content -Path $NormalFile -Value "header`nregular content`nmore content`nfooter"

        Push-Location -Path $script:RepoPath
        try {
            git add metadata.txt normal.txt 2>&1 | Out-Null
            git commit -m 'Add test files' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Restores only hunks whose Lines match a content filter' {
        # Modify the metadata line in one file and the regular line in the other.
        Set-Content -Path (Join-Path $script:RepoPath 'metadata.txt') -Value "header`nms.date: 02-22-2026`ndescription: test file`nfooter"
        Set-Content -Path (Join-Path $script:RepoPath 'normal.txt') -Value "header`nMODIFIED content`nmore content`nfooter"

        Get-GitDiff -RepoPath $script:RepoPath -Hunk |
            Where-Object { $_.Lines.Content -match 'ms\.date' } |
            Restore-GitItem -RepoPath $script:RepoPath -Confirm:$false

        # metadata.txt should have been restored to the original value
        $MetadataContent = Get-Content -Path (Join-Path $script:RepoPath 'metadata.txt') -Raw
        $MetadataContent | Should -Match 'ms\.date: 01-01-2026'

        # normal.txt should still have the modified content
        $NormalContent = Get-Content -Path (Join-Path $script:RepoPath 'normal.txt') -Raw
        $NormalContent | Should -Match 'MODIFIED content'
    }
}

Describe 'Restore-GitItem restores hunks in files with BOM encoding' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $BomFile = Join-Path -Path $script:RepoPath -ChildPath 'bom-file.md'
        $Utf8Bom = [System.Text.UTF8Encoding]::new($true)

        [System.IO.File]::WriteAllText(
            $BomFile,
            "---`nms.date: 01-01-2026`ndescription: test`n---`n",
            $Utf8Bom)

        Push-Location -Path $script:RepoPath
        try {
            git add bom-file.md 2>&1 | Out-Null
            git commit -m 'Add BOM file' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Restores a hunk from a BOM-encoded file without error' {
        $Utf8Bom = [System.Text.UTF8Encoding]::new($true)
        [System.IO.File]::WriteAllText(
            (Join-Path $script:RepoPath 'bom-file.md'),
            "---`nms.date: 02-22-2026`ndescription: test`n---`n",
            $Utf8Bom)

        $Hunks = @(Get-GitDiff -RepoPath $script:RepoPath -Hunk)
        $Hunks | Should -Not -BeNullOrEmpty

        { $Hunks | Restore-GitItem -RepoPath $script:RepoPath -Confirm:$false } | Should -Not -Throw

        # The ms.date line should be reverted to the original committed value.
        $Content = Get-Content -Path (Join-Path $script:RepoPath 'bom-file.md') -Raw
        $Content | Should -Match 'ms\.date: 01-01-2026'
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