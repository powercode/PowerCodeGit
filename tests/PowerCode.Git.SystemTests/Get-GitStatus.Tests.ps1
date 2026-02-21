#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitStatus cmdlet.
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

Describe 'Get-GitStatus clean repository' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns a GitStatusResult object' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status | Should -Not -BeNullOrEmpty
    }

    It 'Reports zero counts on a clean repository' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -Be 0
        $Status.ModifiedCount | Should -Be 0
        $Status.UntrackedCount | Should -Be 0
    }

    It 'Reports the current branch' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.CurrentBranch | Should -BeExactly 'main'
    }
}

Describe 'Get-GitStatus from current directory' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            # Create a .gitignore and an ignored file for Example 2
            Set-Content -Path '.gitignore' -Value '*.log'
            git add .gitignore 2>&1 | Out-Null
            git commit -m 'Add .gitignore' 2>&1 | Out-Null
            Set-Content -Path 'debug.log' -Value 'ignored log content'
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Gets repository status from the current directory (Example 1)' {
        Push-Location -Path $script:RepoPath
        try {
            $Status = Get-GitStatus
            $Status | Should -Not -BeNullOrEmpty
            $Status.CurrentBranch | Should -BeExactly 'main'
        }
        finally {
            Pop-Location
        }
    }

    It 'Includes ignored files from the current directory (Example 2)' {
        Push-Location -Path $script:RepoPath
        try {
            $Status = Get-GitStatus -IncludeIgnored
            $IgnoredEntries = $Status.Entries | Where-Object { $_.FilePath -like '*.log' }
            $IgnoredEntries | Should -Not -BeNullOrEmpty
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Get-GitStatus with changes' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Reports untracked files' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'untracked.txt') -Value 'new'
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.UntrackedCount | Should -BeGreaterOrEqual 1
    }

    It 'Reports modified files' {
        $ExistingFile = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        Set-Content -Path $ExistingFile.FullName -Value 'modified content'
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.ModifiedCount | Should -BeGreaterOrEqual 1
    }

    It 'Reports staged files' {
        Push-Location -Path $script:RepoPath
        try {
            Set-Content -Path 'staged.txt' -Value 'staged content'
            git add staged.txt 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 1
    }
}

Describe 'Get-GitStatus -IncludeIgnored' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            # Create a .gitignore that ignores *.log files
            Set-Content -Path '.gitignore' -Value '*.log'
            git add .gitignore 2>&1 | Out-Null
            git commit -m 'Add .gitignore' 2>&1 | Out-Null

            # Create an ignored file
            Set-Content -Path 'debug.log' -Value 'ignored log content'
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Does not include ignored files by default' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $IgnoredEntries = $Status.Entries | Where-Object { $_.FilePath -like '*.log' }
        $IgnoredEntries | Should -BeNullOrEmpty
    }

    It 'Includes ignored files when -IncludeIgnored is specified' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath -IncludeIgnored
        $IgnoredEntries = $Status.Entries | Where-Object { $_.FilePath -like '*.log' }
        $IgnoredEntries | Should -Not -BeNullOrEmpty
    }

    It 'Ignored entry has the Ignored status' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath -IncludeIgnored
        $LogEntry = $Status.Entries | Where-Object { $_.FilePath -like '*.log' } | Select-Object -First 1
        $LogEntry.Status | Should -Be 'Ignored'
    }
}

Describe 'Get-GitStatus error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitStatus -RepoPath 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitStatus -Path' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            Set-Content -Path 'match.txt' -Value 'a'
            Set-Content -Path 'other.txt' -Value 'b'
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns only entries matching the specified path' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath -Path 'match.txt'
        $Status.Entries | Where-Object { $_.FilePath -eq 'match.txt' } | Should -Not -BeNullOrEmpty
        $Status.Entries | Where-Object { $_.FilePath -eq 'other.txt' } | Should -BeNullOrEmpty
    }
}

Describe 'Get-GitStatus -UntrackedFiles' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            Set-Content -Path 'untracked.txt' -Value 'x'
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Excludes untracked files when -UntrackedFiles No is specified' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath -UntrackedFiles No
        $Status.Entries | Where-Object { $_.Status -eq 'Untracked' } | Should -BeNullOrEmpty
    }
}

Describe 'Get-GitStatus -Options' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns status via -Options parameter set' {
        $Options = [PowerCode.Git.Abstractions.Models.GitStatusOptions]@{
            RepositoryPath = $script:RepoPath
        }

        $Status = Get-GitStatus -Options $Options
        $Status | Should -Not -BeNullOrEmpty
        $Status.CurrentBranch | Should -Not -BeNullOrEmpty
    }
}
