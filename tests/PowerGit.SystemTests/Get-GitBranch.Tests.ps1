#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitBranch cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerGit binary module
    against real git repositories created in temporary directories.
#>

BeforeAll {
    if ($env:POWERGIT_MODULE_PATH -and (Test-Path -Path $env:POWERGIT_MODULE_PATH)) {
        $ModulePath = $env:POWERGIT_MODULE_PATH
    }
    else {
        $RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path
        $ModuleLayoutDir = Join-Path -Path $RepoRoot -ChildPath 'artifacts/module/PowerGit'
        $VersionedDir = Get-ChildItem -Path $ModuleLayoutDir -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $VersionedDir) {
            throw "No versioned module folder found under '$ModuleLayoutDir'. Build the solution before running system tests."
        }
        $ModulePath = Join-Path -Path $VersionedDir.FullName -ChildPath 'PowerGit.psd1'
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

        $TempDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerGitTest_$([System.Guid]::NewGuid().ToString('N'))"
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
    Remove-Module -Name PowerGit -Force -ErrorAction SilentlyContinue
}

Describe 'Get-GitBranch basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns branch objects from a valid repository' {
        $Branches = @(Get-GitBranch -Path $script:RepoPath)
        $Branches | Should -Not -BeNullOrEmpty
    }

    It 'Lists the main branch' {
        $Branches = @(Get-GitBranch -Path $script:RepoPath)
        $Branches | Where-Object { $_.Name -eq 'main' } | Should -Not -BeNullOrEmpty
    }

    It 'Marks HEAD branch with IsHead' {
        $HeadBranch = Get-GitBranch -Path $script:RepoPath | Where-Object { $_.IsHead }
        $HeadBranch | Should -Not -BeNullOrEmpty
        $HeadBranch.Name | Should -BeExactly 'main'
    }
}

Describe 'Get-GitBranch multiple branches' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature 2>&1 | Out-Null
            git checkout -b develop 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Lists all local branches' {
        $Branches = @(Get-GitBranch -Path $script:RepoPath | Where-Object { -not $_.IsRemote })
        $Branches.Count | Should -BeGreaterOrEqual 3
    }

    It 'Branch has TipSha populated' {
        $Branch = Get-GitBranch -Path $script:RepoPath | Select-Object -First 1
        $Branch.TipSha | Should -Match '^[0-9a-f]{40}$'
        $Branch.TipShortSha.Length | Should -Be 7
    }
}

Describe 'Get-GitBranch error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitBranch -Path 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
