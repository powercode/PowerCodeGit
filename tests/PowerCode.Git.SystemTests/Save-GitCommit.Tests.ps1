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
        $Result = Save-GitCommit -RepoPath 'C:\nonexistent\repo\path' -Message 'test' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
