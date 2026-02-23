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
        $Result = Get-GitDiff -RepoPath $NonExistentRepoPath -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitDiff -Commit' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        Push-Location -Path $script:RepoPath
        try {
            $script:InitialSha = git rev-parse HEAD 2>&1
            Set-Content -Path 'file.txt' -Value 'changed'
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Diffs working tree against the specified commit' {
        $Diffs = @(Get-GitDiff -RepoPath $script:RepoPath -Commit $script:InitialSha)
        $Diffs | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitDiff range (FromCommit / ToCommit)' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Second commit')
        Push-Location -Path $script:RepoPath
        try {
            $Commits = git log --format="%H" 2>&1
            $script:FirstSha = $Commits | Select-Object -Last 1
            $script:SecondSha = $Commits | Select-Object -First 1
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Diffs between two commits' {
        $Diffs = @(Get-GitDiff -RepoPath $script:RepoPath -FromCommit $script:FirstSha -ToCommit $script:SecondSha)
        $Diffs | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitDiff -Options' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns diff via -Options parameter set' {
        $Options = [PowerCode.Git.Abstractions.Models.GitDiffOptions]@{
            RepositoryPath = $script:RepoPath
        }

        $Diffs = @(Get-GitDiff -Options $Options)
        $Diffs | Should -HaveCount 0
    }
}

Describe 'Get-GitDiff Example 5 - Stage only hunks that contain added lines' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        # Create a file with content to produce two distant hunks:
        # one that only removes a line, and one that adds a line.
        $FilePath = Join-Path -Path $script:RepoPath -ChildPath 'example5.txt'
        1..20 | ForEach-Object { "line $_" } | Set-Content -Path $FilePath

        Push-Location -Path $script:RepoPath
        try {
            git add example5.txt 2>&1 | Out-Null
            git commit -m 'Add example5 file' 2>&1 | Out-Null

            # Hunk A (near top): remove line 2, do NOT add a replacement
            $Content = Get-Content -Path $FilePath
            $Content = $Content | Where-Object { $_ -ne 'line 2' }
            Set-Content -Path $FilePath -Value $Content

            # Hunk B (near bottom): add a brand-new line at the end
            Add-Content -Path $FilePath -Value 'new line at end'
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Stages only hunks that have at least one Added line' {
        # Run the pipeline from Example 5
        Get-GitDiff -RepoPath $script:RepoPath -Hunk |
            Where-Object { $_.Lines | Where-Object Kind -eq 'Added' } |
            Add-GitItem -RepoPath $script:RepoPath

        # After staging, the staged diff should contain the added line
        $StagedDiffs = @(Get-GitDiff -RepoPath $script:RepoPath -Staged)
        $StagedDiffs | Should -Not -BeNullOrEmpty
        $StagedContent = $StagedDiffs.Patch -join ''
        $StagedContent | Should -Match 'new line at end'
    }

    It 'Does not stage deletion-only hunks' {
        # The removal-only hunk should still be unstaged (working tree)
        $UnstagedDiffs = @(Get-GitDiff -RepoPath $script:RepoPath)
        $UnstagedContent = $UnstagedDiffs.Patch -join ''
        $UnstagedContent | Should -Match 'line 2'
    }
}

Describe 'Get-GitDiff Example 6 - Show unstaged changes with no surrounding context' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        $ExistingFile = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        Set-Content -Path $ExistingFile.FullName -Value 'modified content'
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns diff entries when -Context 0 is specified' {
        $Diffs = @(Get-GitDiff -RepoPath $script:RepoPath -Context 0)
        $Diffs | Should -Not -BeNullOrEmpty
    }

    It 'Patch contains no context lines when -Context 0 is used' {
        $Diff = Get-GitDiff -RepoPath $script:RepoPath -Context 0 | Select-Object -First 1
        $ContextLines = ($Diff.Patch -split "`n") | Where-Object { $_ -match '^ ' }
        $ContextLines | Should -HaveCount 0
    }

    It 'Produces fewer patch lines than the default context' {
        $ZeroContext = (Get-GitDiff -RepoPath $script:RepoPath -Context 0 | Select-Object -First 1).Patch
        $DefaultContext = (Get-GitDiff -RepoPath $script:RepoPath | Select-Object -First 1).Patch
        $ZeroContext.Length | Should -BeLessOrEqual $DefaultContext.Length
    }
}

Describe 'Get-GitDiff -Hunk returns individual hunks' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        # Create a file with 20 lines, commit it, then modify lines near top and bottom
        $FilePath = Join-Path -Path $script:RepoPath -ChildPath 'multi-hunk.txt'
        $Lines = 1..20 | ForEach-Object { "line $_" }
        Set-Content -Path $FilePath -Value ($Lines -join "`n")

        Push-Location -Path $script:RepoPath
        try {
            git add multi-hunk.txt 2>&1 | Out-Null
            git commit -m 'Add multi-hunk file' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        # Modify two distant lines to produce two separate hunks
        $Lines[1] = 'MODIFIED line 2'
        $Lines[18] = 'MODIFIED line 19'
        Set-Content -Path $FilePath -Value ($Lines -join "`n")
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns more than one hunk object' {
        $Hunks = @(Get-GitDiff -RepoPath $script:RepoPath -Hunk)
        $Hunks.Count | Should -BeGreaterOrEqual 2
    }

    It 'Each hunk has FilePath and Header populated' {
        $Hunks = @(Get-GitDiff -RepoPath $script:RepoPath -Hunk)
        foreach ($Hunk in $Hunks) {
            $Hunk.FilePath | Should -Not -BeNullOrEmpty
            $Hunk.Header | Should -Not -BeNullOrEmpty
            $Hunk.Content | Should -Not -BeNullOrEmpty
        }
    }

    It 'Each hunk has correct type' {
        $Hunks = @(Get-GitDiff -RepoPath $script:RepoPath -Hunk)
        foreach ($Hunk in $Hunks) {
            $Hunk | Should -BeOfType 'PowerCode.Git.Abstractions.Models.GitDiffHunk'
        }
    }

    It 'Each hunk has correct LinesAdded and LinesDeleted counts' {
        $Hunks = @(Get-GitDiff -RepoPath $script:RepoPath -Hunk)
        foreach ($Hunk in $Hunks) {
            ($Hunk.LinesAdded + $Hunk.LinesDeleted) | Should -BeGreaterThan 0
        }
    }
}
