#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitConfiguration cmdlet.
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

Describe 'Get-GitConfiguration list all' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns configuration entries' {
        $Entries = @(Get-GitConfiguration -RepoPath $script:RepoPath)
        $Entries.Count | Should -BeGreaterThan 0
    }

    It 'Entries include user.name set by test helper' {
        $Entries = @(Get-GitConfiguration -RepoPath $script:RepoPath)
        $Match = $Entries | Where-Object { $_.Name -eq 'user.name' }
        $Match | Should -Not -BeNullOrEmpty
    }

    It 'Each entry has Name and Value properties' {
        $Entries = @(Get-GitConfiguration -RepoPath $script:RepoPath)
        $Entries[0].Name | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitConfiguration by name' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns a single entry for a specific key' {
        $Entry = Get-GitConfiguration -RepoPath $script:RepoPath -Name 'user.name'
        $Entry | Should -Not -BeNullOrEmpty
        $Entry.Name | Should -BeExactly 'user.name'
    }

    It 'Returns null for a nonexistent key' {
        $Entry = Get-GitConfiguration -RepoPath $script:RepoPath -Name 'nonexistent.key'
        $Entry | Should -BeNullOrEmpty
    }
}

Describe 'Get-GitConfiguration with -Scope' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        # Ensure at least one local config entry exists
        Push-Location $script:RepoPath
        git config --local test.scope localvalue
        Pop-Location
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Filters entries by scope' {
        $Entries = @(Get-GitConfiguration -RepoPath $script:RepoPath -Scope Local)
        $Entries.Count | Should -BeGreaterThan 0
    }
}

Describe 'Get-GitConfiguration with -ShowScope' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Entries include Scope when -ShowScope is specified' {
        $Entries = @(Get-GitConfiguration -RepoPath $script:RepoPath -ShowScope)
        $WithScope = $Entries | Where-Object { $null -ne $_.Scope }
        $WithScope.Count | Should -BeGreaterThan 0
    }
}
