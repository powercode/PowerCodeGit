#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Copy-GitRepository cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    by cloning from a local bare repository created in a temporary directory.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"

    function New-TestBareRepository {
        <#
        .SYNOPSIS
            Creates a temporary bare git repository with an initial commit,
            suitable for use as a clone source.
        #>
        [CmdletBinding()]
        param()

        $TempDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
        $WorkDir = "${TempDir}_work"

        New-Item -Path $TempDir -ItemType Directory -Force | Out-Null
        New-Item -Path $WorkDir -ItemType Directory -Force | Out-Null

        Push-Location -Path $WorkDir
        try {
            git init --initial-branch main 2>&1 | Out-Null
            git config user.name 'Test Author'
            git config user.email 'test@example.com'
            Set-Content -Path 'README.md' -Value '# Test Repo'
            git add . 2>&1 | Out-Null
            git commit -m 'Initial commit' 2>&1 | Out-Null
            git clone --bare $WorkDir $TempDir 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        # Clean up the working directory
        Get-ChildItem -Path $WorkDir -Recurse -Force | ForEach-Object {
            if ($_.Attributes -band [System.IO.FileAttributes]::ReadOnly) {
                $_.Attributes = $_.Attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly)
            }
        }
        Remove-Item -Path $WorkDir -Recurse -Force

        return $TempDir
    }
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'Copy-GitRepository from local bare repo' {
    BeforeAll {
        $script:BareRepoPath = New-TestBareRepository
        $script:ClonePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitClone_$([System.Guid]::NewGuid().ToString('N'))"
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:BareRepoPath
        Remove-TestGitRepository -Path $script:ClonePath
    }

    It 'Clones a repository and returns the local path' {
        $Result = Copy-GitRepository -Url $script:BareRepoPath -LocalPath $script:ClonePath

        $Result | Should -Not -BeNullOrEmpty
        Test-Path -Path $Result | Should -BeTrue
    }

    It 'The cloned repository contains a .git directory' {
        $GitDir = Join-Path -Path $script:ClonePath -ChildPath '.git'
        Test-Path -Path $GitDir | Should -BeTrue
    }

    It 'The cloned repository has the initial commit' {
        $Commits = @(Get-GitLog -RepoPath $script:ClonePath)
        $Commits | Should -HaveCount 1
        $Commits[0].MessageShort | Should -BeExactly 'Initial commit'
    }

    It 'The cloned repository has the README.md file' {
        $ReadmePath = Join-Path -Path $script:ClonePath -ChildPath 'README.md'
        Test-Path -Path $ReadmePath | Should -BeTrue
        Get-Content -Path $ReadmePath -Raw | Should -Match '# Test Repo'
    }
}

Describe 'Copy-GitRepository error handling' {
    It 'Produces a non-terminating error for an invalid URL' {
        $Result = Copy-GitRepository -Url 'https://invalid.example.com/nonexistent.git' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Copy-GitRepository -Options' {
    BeforeAll {
        $script:BareRepoPath = New-TestBareRepository
        $script:ClonePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitClone_$([System.Guid]::NewGuid().ToString('N'))"
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:BareRepoPath
        Remove-TestGitRepository -Path $script:ClonePath
    }

    It 'Clones using a GitCloneOptions object' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitCloneOptions]@{
            Url       = $script:BareRepoPath
            LocalPath = $script:ClonePath
        }

        $Result = Copy-GitRepository -Options $Opts

        $Result | Should -Not -BeNullOrEmpty
        Test-Path -Path $Result | Should -BeTrue
    }
}
