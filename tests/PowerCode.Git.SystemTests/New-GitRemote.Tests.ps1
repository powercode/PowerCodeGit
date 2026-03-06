#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the New-GitRemote cmdlet.
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

Describe 'New-GitRemote adds a remote' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:BareUrl  = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        git init --bare $script:BareUrl | Out-Null
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path $script:BareUrl) { Remove-Item -Recurse -Force $script:BareUrl }
    }

    It 'Returns a GitRemoteInfo object' {
        $Remote = New-GitRemote -RepoPath $script:RepoPath -Name upstream -Url $script:BareUrl
        $Remote | Should -Not -BeNullOrEmpty
        $Remote.PSObject.TypeNames | Should -Contain 'PowerCode.Git.Abstractions.Models.GitRemoteInfo'
    }

    It 'Remote Name matches the requested name' {
        $Remote = Get-GitRemote -RepoPath $script:RepoPath -Name upstream
        $Remote.Name | Should -BeExactly 'upstream'
    }

    It 'Remote FetchUrl matches the supplied URL' {
        $Remote = Get-GitRemote -RepoPath $script:RepoPath -Name upstream
        $Remote.FetchUrl | Should -Be $script:BareUrl
    }

    It 'Remote is visible via git CLI' {
        Push-Location $script:RepoPath
        try {
            $Lines = git remote
            $Lines | Should -Contain 'upstream'
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'New-GitRemote with -PushUrl' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:FetchUrl = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        $script:PushUrl  = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        git init --bare $script:FetchUrl | Out-Null
        git init --bare $script:PushUrl  | Out-Null
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path $script:FetchUrl) { Remove-Item -Recurse -Force $script:FetchUrl }
        if (Test-Path $script:PushUrl)  { Remove-Item -Recurse -Force $script:PushUrl }
    }

    It 'Sets a distinct PushUrl' {
        $Remote = New-GitRemote -RepoPath $script:RepoPath -Name origin -Url $script:FetchUrl -PushUrl $script:PushUrl
        $Remote.FetchUrl | Should -Be $script:FetchUrl
        $Remote.PushUrl  | Should -Be $script:PushUrl
        $Remote.PushUrl  | Should -Not -Be $Remote.FetchUrl
    }
}

Describe 'New-GitRemote duplicate name error' {
    BeforeAll {
        $script:Repos   = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Emits a non-terminating error when the remote already exists' {
        $Result = New-GitRemote -RepoPath $script:RepoPath -Name origin -Url 'file:///nonexistent' `
            -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'New-GitRemote -Options parameter set' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:BareUrl  = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        git init --bare $script:BareUrl | Out-Null
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path $script:BareUrl) { Remove-Item -Recurse -Force $script:BareUrl }
    }

    It 'Adds remote via Options object' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitRemoteAddOptions]@{
            RepositoryPath = $script:RepoPath
            Name           = 'via-opts'
            Url            = $script:BareUrl
        }
        $Remote = New-GitRemote -Options $Opts
        $Remote.Name | Should -BeExactly 'via-opts'
    }
}

Describe 'New-GitRemote error handling' {
    It 'Produces a non-terminating error for an invalid repo path' {
        $Result = New-GitRemote -RepoPath $NonExistentRepoPath -Name origin -Url 'file:///nonexistent' `
            -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
