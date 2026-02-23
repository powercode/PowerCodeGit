#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Set-GitConfiguration cmdlet.
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

Describe 'Set-GitConfiguration local config' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Sets a local configuration value' {
        $Entry = Set-GitConfiguration -RepoPath $script:RepoPath -Name 'user.name' -Value 'SystemTestUser'
        $Entry | Should -Not -BeNullOrEmpty
        $Entry.Name | Should -BeExactly 'user.name'
        $Entry.Value | Should -BeExactly 'SystemTestUser'
    }

    It 'Value is readable via git config' {
        Push-Location $script:RepoPath
        try {
            $Value = git config --get user.name
            $Value | Should -BeExactly 'SystemTestUser'
        }
        finally {
            Pop-Location
        }
    }

    It 'Returns a GitConfigEntry object' {
        $Entry = Set-GitConfiguration -RepoPath $script:RepoPath -Name 'core.autocrlf' -Value 'input'
        $Entry.PSObject.TypeNames | Should -Contain 'PowerCode.Git.Abstractions.Models.GitConfigEntry'
    }
}

Describe 'Set-GitConfiguration with -Scope Local' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Sets a config value with explicit Local scope' {
        $Entry = Set-GitConfiguration -RepoPath $script:RepoPath -Name 'diff.algorithm' -Value 'histogram' -Scope Local
        $Entry | Should -Not -BeNullOrEmpty
        $Entry.Name | Should -BeExactly 'diff.algorithm'
        $Entry.Value | Should -BeExactly 'histogram'
    }

    It 'Value is visible in local scope' {
        Push-Location $script:RepoPath
        try {
            $Value = git config --local --get diff.algorithm
            $Value | Should -BeExactly 'histogram'
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Set-GitConfiguration overwrites existing value' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Overwrites a previously set value' {
        Set-GitConfiguration -RepoPath $script:RepoPath -Name 'core.autocrlf' -Value 'true'
        Set-GitConfiguration -RepoPath $script:RepoPath -Name 'core.autocrlf' -Value 'false'

        Push-Location $script:RepoPath
        try {
            $Value = git config --get core.autocrlf
            $Value | Should -BeExactly 'false'
        }
        finally {
            Pop-Location
        }
    }
}
