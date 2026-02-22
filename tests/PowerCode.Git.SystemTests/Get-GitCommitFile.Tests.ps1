#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitCommitFile cmdlet.
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

Describe 'Get-GitCommitFile default (HEAD)' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Second commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Example 1 - Returns files changed in the latest commit' {
        $Files = @(Get-GitCommitFile -RepoPath $script:RepoPath)
        $Files | Should -Not -BeNullOrEmpty
    }

    It 'Each entry has a Status property' {
        $File = Get-GitCommitFile -RepoPath $script:RepoPath | Select-Object -First 1
        $File.Status | Should -Not -BeNullOrEmpty
    }

    It 'Each entry has a NewPath property' {
        $File = Get-GitCommitFile -RepoPath $script:RepoPath | Select-Object -First 1
        $File.NewPath | Should -Not -BeNullOrEmpty
    }

    It 'Each entry has Patch content' {
        $File = Get-GitCommitFile -RepoPath $script:RepoPath | Select-Object -First 1
        $File.Patch | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitCommitFile -Commit <sha>' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Second commit')

        Push-Location -Path $script:RepoPath
        try {
            $script:FirstSha = git log --format="%H" 2>&1 | Select-Object -Last 1
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Example 2 - Returns files changed in a specific commit' {
        $Files = @(Get-GitCommitFile -RepoPath $script:RepoPath -Commit $script:FirstSha)
        $Files | Should -Not -BeNullOrEmpty
    }

    It 'Returns Added status for the initial commit' {
        $Files = @(Get-GitCommitFile -RepoPath $script:RepoPath -Commit $script:FirstSha)
        $Files[0].Status | Should -Be 'Added'
    }
}

Describe 'Get-GitCommitFile pipeline from Get-GitLog' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Second commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Example 3 - Accepts pipeline input from Get-GitLog' {
        $Files = @(Get-GitLog -RepoPath $script:RepoPath -MaxCount 1 | Get-GitCommitFile -RepoPath $script:RepoPath)
        $Files | Should -Not -BeNullOrEmpty
    }

    It 'Returns correct file for piped commit' {
        $Files = @(Get-GitLog -RepoPath $script:RepoPath -MaxCount 1 | Get-GitCommitFile -RepoPath $script:RepoPath)
        $Files[0].NewPath | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitCommitFile -Hunk' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Second commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Example 4 - Returns GitDiffHunk objects with -Hunk switch' {
        $Hunks = @(Get-GitCommitFile -RepoPath $script:RepoPath -Hunk)
        $Hunks | Should -Not -BeNullOrEmpty
        $Hunks[0] | Should -BeOfType 'PowerCode.Git.Abstractions.Models.GitDiffHunk'
    }

    It 'Each hunk has FilePath and Header populated' {
        $Hunks = @(Get-GitCommitFile -RepoPath $script:RepoPath -Hunk)
        foreach ($Hunk in $Hunks) {
            $Hunk.FilePath | Should -Not -BeNullOrEmpty
            $Hunk.Header | Should -Not -BeNullOrEmpty
        }
    }
}

Describe 'Get-GitCommitFile -Path filter' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        # Create a second commit with two known files
        Push-Location -Path $script:RepoPath
        try {
            Set-Content -Path 'target.txt' -Value 'target content'
            Set-Content -Path 'other.txt' -Value 'other content'
            git add . 2>&1 | Out-Null
            git commit -m 'Add two files' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Filters output to specified path' {
        $Files = @(Get-GitCommitFile -RepoPath $script:RepoPath -Path 'target.txt')
        $Files | Should -HaveCount 1
        $Files[0].NewPath | Should -BeExactly 'target.txt'
    }
}

Describe 'Get-GitCommitFile error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitCommitFile -RepoPath $NonExistentRepoPath -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
