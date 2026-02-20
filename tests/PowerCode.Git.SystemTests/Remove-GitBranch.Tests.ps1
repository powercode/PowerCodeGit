#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Remove-GitBranch cmdlet.
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

Describe 'Remove-GitBranch basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature-to-delete 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Deletes a merged branch' {
        Remove-GitBranch -RepoPath $script:RepoPath -Name 'feature-to-delete' -Confirm:$false

        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath | Where-Object { -not $_.IsRemote })
        $Deleted = $Branches | Where-Object { $_.Name -eq 'feature-to-delete' }
        $Deleted | Should -BeNullOrEmpty
    }
}

Describe 'Remove-GitBranch -Force' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b unmerged-feature 2>&1 | Out-Null
            Set-Content -Path 'unmerged.txt' -Value 'content'
            git add . 2>&1 | Out-Null
            git commit -m 'Unmerged work' 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Force-deletes an unmerged branch' {
        Remove-GitBranch -RepoPath $script:RepoPath -Name 'unmerged-feature' -Force -Confirm:$false

        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath | Where-Object { -not $_.IsRemote })
        $Deleted = $Branches | Where-Object { $_.Name -eq 'unmerged-feature' }
        $Deleted | Should -BeNullOrEmpty
    }
}

Describe 'Remove-GitBranch error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Produces a non-terminating error when deleting the current branch' {
        $Result = Remove-GitBranch -RepoPath $script:RepoPath -Name 'main' -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for a nonexistent branch' {
        $Result = Remove-GitBranch -RepoPath $script:RepoPath -Name 'nonexistent' -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }

    It 'Produces a non-terminating error for an invalid path' {
        $Result = Remove-GitBranch -RepoPath 'C:\nonexistent\repo\path' -Name 'test' -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
