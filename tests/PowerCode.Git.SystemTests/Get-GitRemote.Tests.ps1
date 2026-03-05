#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitRemote cmdlet.
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

Describe 'Get-GitRemote no remotes' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns empty when there are no remotes' {
        $Remotes = @(Get-GitRemote -RepoPath $script:RepoPath)
        $Remotes | Should -HaveCount 0
    }
}

Describe 'Get-GitRemote with origin' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Returns origin remote' {
        $Remotes = @(Get-GitRemote -RepoPath $script:RepoPath)
        $Remotes | Should -HaveCount 1
        $Remotes[0].Name | Should -BeExactly 'origin'
    }

    It 'Remote has FetchUrl and PushUrl' {
        $Remote = Get-GitRemote -RepoPath $script:RepoPath | Select-Object -First 1
        $Remote.FetchUrl | Should -Not -BeNullOrEmpty
        $Remote.PushUrl | Should -Not -BeNullOrEmpty
    }

    It 'Returns GitRemoteInfo type' {
        $Remote = Get-GitRemote -RepoPath $script:RepoPath | Select-Object -First 1
        $Remote.PSObject.TypeNames | Should -Contain 'PowerCode.Git.Abstractions.Models.GitRemoteInfo'
    }

    It 'Filters by -Name origin returns single result' {
        $Remotes = @(Get-GitRemote -RepoPath $script:RepoPath -Name origin)
        $Remotes | Should -HaveCount 1
        $Remotes[0].Name | Should -BeExactly 'origin'
    }

    It 'Filters by non-existent -Name returns empty' {
        $Remotes = @(Get-GitRemote -RepoPath $script:RepoPath -Name fake)
        $Remotes | Should -HaveCount 0
    }

    It 'Accepts -Name as position 0 argument' {
        $Remotes = @(Get-GitRemote origin -RepoPath $script:RepoPath)
        $Remotes | Should -HaveCount 1
        $Remotes[0].Name | Should -BeExactly 'origin'
    }
}

Describe 'Get-GitRemote -Options parameter set' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Returns remotes via Options object' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitRemoteListOptions]@{
            RepositoryPath = $script:RepoPath
        }
        $Remotes = @(Get-GitRemote -Options $Opts)
        $Remotes | Should -HaveCount 1
    }

    It 'Filters by name via Options object' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitRemoteListOptions]@{
            RepositoryPath = $script:RepoPath
            Name           = 'origin'
        }
        $Remotes = @(Get-GitRemote -Options $Opts)
        $Remotes | Should -HaveCount 1
        $Remotes[0].Name | Should -BeExactly 'origin'
    }
}

Describe 'Get-GitRemote error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitRemote -RepoPath $NonExistentRepoPath -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
