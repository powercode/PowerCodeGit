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
