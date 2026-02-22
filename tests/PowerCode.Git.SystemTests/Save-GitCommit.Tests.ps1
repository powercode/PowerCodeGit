#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Save-GitCommit cmdlet.
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

Describe 'Save-GitCommit basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Creates a commit with the given message and returns GitCommitInfo' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'new.txt') -Value 'content'
        Add-GitItem -RepoPath $script:RepoPath -Path 'new.txt'

        $Result = Save-GitCommit -RepoPath $script:RepoPath -Message 'Add new file'

        $Result | Should -Not -BeNullOrEmpty
        $Result.MessageShort | Should -BeExactly 'Add new file'
        $Result.Sha | Should -Match '^[0-9a-f]{40}$'
    }

    It 'The new commit appears in Get-GitLog' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -MaxCount 1)
        $Commits[0].MessageShort | Should -BeExactly 'Add new file'
    }

    It 'Reports the correct author from git config' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -MaxCount 1)
        $Commits[0].AuthorName | Should -BeExactly 'Test Author'
        $Commits[0].AuthorEmail | Should -BeExactly 'test@example.com'
    }
}

Describe 'Save-GitCommit -Amend' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Amends the previous commit with a new message' {
        $Result = Save-GitCommit -RepoPath $script:RepoPath -Message 'Amended message' -Amend

        $Result | Should -Not -BeNullOrEmpty
        $Result.MessageShort | Should -BeExactly 'Amended message'

        $Commits = @(Get-GitLog -RepoPath $script:RepoPath)
        $Commits | Should -HaveCount 1
        $Commits[0].MessageShort | Should -BeExactly 'Amended message'
    }
}

Describe 'Save-GitCommit -AllowEmpty' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Creates an empty commit when -AllowEmpty is specified' {
        $Result = Save-GitCommit -RepoPath $script:RepoPath -Message 'Empty commit' -AllowEmpty

        $Result | Should -Not -BeNullOrEmpty
        $Result.MessageShort | Should -BeExactly 'Empty commit'

        $Commits = @(Get-GitLog -RepoPath $script:RepoPath)
        $Commits | Should -HaveCount 2
    }
}

Describe 'Save-GitCommit error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Save-GitCommit -RepoPath $NonExistentRepoPath -Message 'test' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Save-GitCommit -All' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Stages tracked modified files and commits without explicit Add-GitItem' {
        # Modify an existing tracked file without staging
        $TrackedFile = Get-ChildItem -Path $script:RepoPath -File | Select-Object -First 1
        Set-Content -Path $TrackedFile.FullName -Value 'modified content'

        $Result = Save-GitCommit -RepoPath $script:RepoPath -Message 'All tracked changes' -All

        $Result | Should -Not -BeNullOrEmpty
        $Result.MessageShort | Should -BeExactly 'All tracked changes'
    }
}

Describe 'Save-GitCommit -Author' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Uses the provided author name and email' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'authored.txt') -Value 'authored'
        Add-GitItem -RepoPath $script:RepoPath -Path 'authored.txt'

        $Result = Save-GitCommit -RepoPath $script:RepoPath -Message 'Custom author' -Author 'Jane Doe <jane@example.com>'

        $Result | Should -Not -BeNullOrEmpty
        $Result.AuthorName | Should -BeExactly 'Jane Doe'
        $Result.AuthorEmail | Should -BeExactly 'jane@example.com'
    }
}

Describe 'Save-GitCommit -Date' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Uses the provided date for the commit' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'dated.txt') -Value 'dated'
        Add-GitItem -RepoPath $script:RepoPath -Path 'dated.txt'

        $BackDate = [DateTimeOffset]::new(2020, 6, 15, 12, 0, 0, [TimeSpan]::Zero)
        $Result = Save-GitCommit -RepoPath $script:RepoPath -Message 'Backdated commit' -Date $BackDate

        $Result | Should -Not -BeNullOrEmpty
        $Result.AuthorDate.Year | Should -Be 2020
        $Result.AuthorDate.Month | Should -Be 6
        $Result.AuthorDate.Day | Should -Be 15
    }
}

Describe 'Save-GitCommit -Options' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Accepts a GitCommitOptions object and creates a commit' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'opts.txt') -Value 'via options'
        Add-GitItem -RepoPath $script:RepoPath -Path 'opts.txt'

        $Opts = [PowerCode.Git.Abstractions.Models.GitCommitOptions]@{
            RepositoryPath = $script:RepoPath
            Message        = 'Via options object'
        }

        $Result = Save-GitCommit -Options $Opts

        $Result | Should -Not -BeNullOrEmpty
        $Result.MessageShort | Should -BeExactly 'Via options object'
    }
}

