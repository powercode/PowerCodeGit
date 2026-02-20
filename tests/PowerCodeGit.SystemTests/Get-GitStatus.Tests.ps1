#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitStatus cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCodeGit binary module
    against real git repositories created in temporary directories.
#>

BeforeAll {
    if ($env:POWERCODEGIT_MODULE_PATH -and (Test-Path -Path $env:POWERCODEGIT_MODULE_PATH)) {
        $ModulePath = $env:POWERCODEGIT_MODULE_PATH
    }
    else {
        $RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path
        $ModuleLayoutDir = Join-Path -Path $RepoRoot -ChildPath 'artifacts/module/PowerCodeGit'
        $VersionedDir = Get-ChildItem -Path $ModuleLayoutDir -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $VersionedDir) {
            throw "No versioned module folder found under '$ModuleLayoutDir'. Build the solution before running system tests."
        }
        $ModulePath = Join-Path -Path $VersionedDir.FullName -ChildPath 'PowerCodeGit.psd1'
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

        $TempDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCodeGitTest_$([System.Guid]::NewGuid().ToString('N'))"
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
    Remove-Module -Name PowerCodeGit -Force -ErrorAction SilentlyContinue
}

Describe 'Get-GitStatus clean repository' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns a GitStatusResult object' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status | Should -Not -BeNullOrEmpty
    }

    It 'Reports zero counts on a clean repository' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -Be 0
        $Status.ModifiedCount | Should -Be 0
        $Status.UntrackedCount | Should -Be 0
    }

    It 'Reports the current branch' {
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.CurrentBranch | Should -BeExactly 'main'
    }
}

Describe 'Get-GitStatus with changes' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Reports untracked files' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'untracked.txt') -Value 'new'
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.UntrackedCount | Should -BeGreaterOrEqual 1
    }

    It 'Reports modified files' {
        $ExistingFile = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        Set-Content -Path $ExistingFile.FullName -Value 'modified content'
        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.ModifiedCount | Should -BeGreaterOrEqual 1
    }

    It 'Reports staged files' {
        Push-Location -Path $script:RepoPath
        try {
            Set-Content -Path 'staged.txt' -Value 'staged content'
            git add staged.txt 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 1
    }
}

Describe 'Get-GitStatus error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitStatus -RepoPath 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
