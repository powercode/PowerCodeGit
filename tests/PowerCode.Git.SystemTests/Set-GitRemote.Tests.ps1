#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Set-GitRemote cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    against real git repositories created in temporary directories.
.NOTES
    Set-GitRemote can rename a remote (-NewName) and/or update its
    fetch/push URL (-Url, -PushUrl). Both operations may be combined.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'Set-GitRemote -Url updates the fetch URL' {
    BeforeAll {
        $script:Repos    = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
        $script:NewUrl   = $script:Repos.BarePath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Returns a GitRemoteInfo object' {
        $Remote = Set-GitRemote -RepoPath $script:RepoPath -Name origin -Url $script:NewUrl
        $Remote | Should -Not -BeNullOrEmpty
        $Remote.PSObject.TypeNames | Should -Contain 'PowerCode.Git.Abstractions.Models.GitRemoteInfo'
    }

    It 'FetchUrl reflects the new URL' {
        $Remote = Get-GitRemote -RepoPath $script:RepoPath -Name origin
        $Remote.FetchUrl | Should -Be $script:NewUrl
    }

    It 'git remote get-url echoes the new URL' {
        Push-Location $script:RepoPath
        try {
            $Url = git remote get-url origin
            $Url | Should -Be $script:NewUrl
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Set-GitRemote -PushUrl updates the push URL' {
    BeforeAll {
        $script:Repos    = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
        $script:PushUrl  = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        git init --bare $script:PushUrl | Out-Null
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
        if (Test-Path $script:PushUrl) { Remove-Item -Recurse -Force $script:PushUrl }
    }

    It 'PushUrl differs from FetchUrl after update' {
        $Remote = Set-GitRemote -RepoPath $script:RepoPath -Name origin -PushUrl $script:PushUrl
        $Remote.PushUrl | Should -Be $script:PushUrl
        $Remote.PushUrl | Should -Not -Be $Remote.FetchUrl
    }

    It 'git remote get-url --push echoes the new push URL' {
        Push-Location $script:RepoPath
        try {
            $Url = git remote get-url --push origin
            $Url | Should -Be $script:PushUrl
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Set-GitRemote -NewName renames the remote' {
    BeforeAll {
        $script:Repos    = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Returns GitRemoteInfo with updated name' {
        $Remote = Set-GitRemote -RepoPath $script:RepoPath -Name origin -NewName upstream
        $Remote.Name | Should -BeExactly 'upstream'
    }

    It 'Old name is no longer present' {
        Push-Location $script:RepoPath
        try {
            $Lines = git remote
            $Lines | Should -Not -Contain 'origin'
        }
        finally {
            Pop-Location
        }
    }

    It 'New name is present in git CLI' {
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

Describe 'Set-GitRemote -NewName combined with -Url' {
    BeforeAll {
        $script:Repos    = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
        $script:NewUrl   = $script:Repos.BarePath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Renames and updates URL in a single call' {
        $Remote = Set-GitRemote -RepoPath $script:RepoPath -Name origin -NewName upstream -Url $script:NewUrl
        $Remote.Name     | Should -BeExactly 'upstream'
        $Remote.FetchUrl | Should -Be $script:NewUrl
    }
}

Describe 'Set-GitRemote -Options parameter set' {
    BeforeAll {
        $script:Repos    = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
        $script:NewUrl   = $script:Repos.BarePath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Updates remote via Options object' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitRemoteUpdateOptions]@{
            RepositoryPath = $script:RepoPath
            Name           = 'origin'
            Url            = $script:NewUrl
        }
        $Remote = Set-GitRemote -Options $Opts
        $Remote.FetchUrl | Should -Be $script:NewUrl
    }
}

Describe 'Set-GitRemote error handling' {
    BeforeAll {
        $script:Repos    = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Emits a non-terminating error for a non-existent remote' {
        $Result = Set-GitRemote -RepoPath $script:RepoPath -Name fake -Url 'file:///nonexistent' `
            -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Emits a non-terminating error for an invalid repo path' {
        $Result = Set-GitRemote -RepoPath $NonExistentRepoPath -Name origin -Url 'file:///nonexistent' `
            -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
