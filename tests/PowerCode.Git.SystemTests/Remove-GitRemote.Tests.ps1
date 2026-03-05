#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Remove-GitRemote cmdlet.
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

Describe 'Remove-GitRemote removes an existing remote' {
    BeforeAll {
        $script:Repos    = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Produces no output (Remove- convention)' {
        $Output = Remove-GitRemote -RepoPath $script:RepoPath -Name origin
        $Output | Should -BeNullOrEmpty
    }

    It 'Remote is absent after removal' {
        $Remotes = @(Get-GitRemote -RepoPath $script:RepoPath)
        $Remotes | Should -HaveCount 0
    }

    It 'git remote confirms removal' {
        Push-Location $script:RepoPath
        try {
            $Lines = git remote
            $Lines | Should -Not -Contain 'origin'
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Remove-GitRemote pipeline from Get-GitRemote' {
    BeforeAll {
        $script:Repos    = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Accepts pipeline input from Get-GitRemote' {
        Get-GitRemote -RepoPath $script:RepoPath -Name origin |
            Remove-GitRemote -RepoPath $script:RepoPath
        $Remotes = @(Get-GitRemote -RepoPath $script:RepoPath)
        $Remotes | Should -HaveCount 0
    }
}

Describe 'Remove-GitRemote -Options parameter set' {
    BeforeAll {
        $script:Repos    = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Removes remote via Options object' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitRemoteRemoveOptions]@{
            RepositoryPath = $script:RepoPath
            Name           = 'origin'
        }
        Remove-GitRemote -Options $Opts
        $Remotes = @(Get-GitRemote -RepoPath $script:RepoPath)
        $Remotes | Should -HaveCount 0
    }
}

Describe 'Remove-GitRemote error handling' {
    BeforeAll {
        $script:Repos    = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Emits a non-terminating error for a non-existent remote' {
        $Result = Remove-GitRemote -RepoPath $script:RepoPath -Name fake `
            -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Emits a non-terminating error for an invalid repo path' {
        $Result = Remove-GitRemote -RepoPath $NonExistentRepoPath -Name origin `
            -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
