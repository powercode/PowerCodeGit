#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Clear-GitConfiguration cmdlet.
.DESCRIPTION
    End-to-end tests that exercise Clear-GitConfiguration against real git
    repositories created in temporary directories.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

# Example 1 — Remove a repository-local key
Describe 'Clear-GitConfiguration — local scope' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        # Write a key so it can be cleared
        git -C $script:RepoPath config user.name 'To Be Cleared'
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Removes a local key so it can no longer be read' {
        Clear-GitConfiguration -RepoPath $script:RepoPath -Name user.name
        $Value = git -C $script:RepoPath config --local user.name 2>$null
        $Value | Should -BeNullOrEmpty
    }

    It 'Produces no output' {
        git -C $script:RepoPath config user.name 'Temp'
        $Result = Clear-GitConfiguration -RepoPath $script:RepoPath -Name user.name
        $Result | Should -BeNullOrEmpty
    }
}

# Example 2 — Remove a key from the global configuration
Describe 'Clear-GitConfiguration — global scope' {
    BeforeEach {
        # Write a throwaway global key under a unique name to avoid touching real config
        $script:TestKey = "test.cleartest$(([System.Guid]::NewGuid().ToString('N').Substring(0,8)))"
        git config --global $script:TestKey 'test-value' 2>$null
    }

    AfterEach {
        # Ensure cleanup even if the test fails
        git config --global --unset $script:TestKey 2>$null
    }

    It 'Removes the key from the global config file' {
        Clear-GitConfiguration -Name $script:TestKey -Scope Global
        $Value = git config --global $script:TestKey 2>$null
        $Value | Should -BeNullOrEmpty
    }
}

# Example 3 — Remove multiple keys at once
Describe 'Clear-GitConfiguration — multiple keys' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        git -C $script:RepoPath config user.name 'Alice'
        git -C $script:RepoPath config user.email 'alice@example.com'
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Removes all specified keys in a single call' {
        Clear-GitConfiguration -RepoPath $script:RepoPath -Name user.name, user.email

        $Name  = git -C $script:RepoPath config --local user.name  2>$null
        $Email = git -C $script:RepoPath config --local user.email 2>$null

        $Name  | Should -BeNullOrEmpty
        $Email | Should -BeNullOrEmpty
    }
}

# Example 4 — Preview with -WhatIf
Describe 'Clear-GitConfiguration — -WhatIf' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        git -C $script:RepoPath config user.name 'Should Remain'
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Does not remove the key when -WhatIf is specified' {
        Clear-GitConfiguration -RepoPath $script:RepoPath -Name user.name -WhatIf
        $Value = git -C $script:RepoPath config --local user.name
        $Value | Should -Be 'Should Remain'
    }
}

Describe 'Clear-GitConfiguration — error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Silently succeeds when the key does not exist' {
        $Errors = @()
        $Result = Clear-GitConfiguration -RepoPath $script:RepoPath -Name nonexistent.key `
            -ErrorAction SilentlyContinue `
            -ErrorVariable Errors
        $Result | Should -BeNullOrEmpty
        $Errors | Should -BeNullOrEmpty
    }

    It 'Processes all keys even when one does not exist' {
        git -C $script:RepoPath config user.email 'keep@example.com'
        $Errors = @()
        Clear-GitConfiguration -RepoPath $script:RepoPath -Name nonexistent.key, user.email `
            -ErrorAction SilentlyContinue `
            -ErrorVariable Errors

        # No error for the missing key
        $Errors | Should -BeNullOrEmpty
        # user.email was still removed
        $Email = git -C $script:RepoPath config --local user.email 2>$null
        $Email | Should -BeNullOrEmpty
    }
}
