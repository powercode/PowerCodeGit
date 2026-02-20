#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the New-GitBranch cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    against real git repositories created in temporary directories.
#>

BeforeAll {
    if ($env:POWERCODE_GIT_MODULE_PATH -and (Test-Path -Path $env:POWERCODE_GIT_MODULE_PATH)) {
        $ModulePath = $env:POWERCODE_GIT_MODULE_PATH
    }
    else {
        $RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path
        $ModuleLayoutDir = Join-Path -Path $RepoRoot -ChildPath 'artifacts/module/PowerCode.Git'
        $VersionedDir = Get-ChildItem -Path $ModuleLayoutDir -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $VersionedDir) {
            throw "No versioned module folder found under '$ModuleLayoutDir'. Build the solution before running system tests."
        }
        $ModulePath = Join-Path -Path $VersionedDir.FullName -ChildPath 'PowerCode.Git.psd1'
    }

    if (-not (Test-Path -Path $ModulePath)) {
        throw "Module not found at '$ModulePath'. Build the solution before running system tests."
    }

    Import-Module -Name $ModulePath -Force -ErrorAction Stop

    function New-TestGitRepository {
        [CmdletBinding()]
        param(
            [Parameter()]
            [string[]]$CommitMessages = @('Initial commit'),

            [Parameter()]
            [string]$AuthorName = 'Test Author',

            [Parameter()]
            [string]$AuthorEmail = 'test@example.com'
        )

        $TempDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -Path $TempDir -ItemType Directory -Force | Out-Null

        Push-Location -Path $TempDir
        try {
            git init --initial-branch main 2>&1 | Out-Null
            git config user.name $AuthorName
            git config user.email $AuthorEmail

            foreach ($Message in $CommitMessages) {
                $FileName = "file_$([System.Guid]::NewGuid().ToString('N')).txt"
                Set-Content -Path (Join-Path -Path $TempDir -ChildPath $FileName) -Value $Message
                git add . 2>&1 | Out-Null
                git commit -m $Message 2>&1 | Out-Null
            }
        }
        finally {
            Pop-Location
        }

        return $TempDir
    }

    function Remove-TestGitRepository {
        [CmdletBinding()]
        param(
            [Parameter(Mandatory)]
            [string]$Path
        )

        if (Test-Path -Path $Path) {
            Get-ChildItem -Path $Path -Recurse -Force | ForEach-Object {
                if ($_.Attributes -band [System.IO.FileAttributes]::ReadOnly) {
                    $_.Attributes = $_.Attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly)
                }
            }
            Remove-Item -Path $Path -Recurse -Force
        }
    }
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'New-GitBranch basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Creates a new branch and returns GitBranchInfo' {
        $Result = New-GitBranch -RepoPath $script:RepoPath -Name 'feature/test'

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'feature/test'
        $Result.IsHead | Should -BeTrue
    }

    It 'The new branch appears in Get-GitBranch' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath)
        $FeatureBranch = $Branches | Where-Object { $_.Name -eq 'feature/test' }
        $FeatureBranch | Should -Not -BeNullOrEmpty
    }

    It 'HEAD points to the new branch after creation' {
        Push-Location -Path $script:RepoPath
        try {
            $CurrentBranch = git rev-parse --abbrev-ref HEAD 2>&1
        }
        finally {
            Pop-Location
        }

        $CurrentBranch | Should -BeExactly 'feature/test'
    }

    It 'Creates a branch with the same commit SHA as the source' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath)
        $MainBranch = $Branches | Where-Object { $_.Name -eq 'main' }
        $FeatureBranch = $Branches | Where-Object { $_.Name -eq 'feature/test' }
        $FeatureBranch.Sha | Should -BeExactly $MainBranch.Sha
    }
}

Describe 'New-GitBranch error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Produces a non-terminating error when the branch already exists' {
        $Result = New-GitBranch -RepoPath $script:RepoPath -Name 'main' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for an invalid path' {
        $Result = New-GitBranch -RepoPath 'C:\nonexistent\repo\path' -Name 'test' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
