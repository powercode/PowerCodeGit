#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Add-GitItem cmdlet.
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

Describe 'Add-GitItem single file' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Stages a new file so it appears in Get-GitStatus as staged' {
        $NewFile = Join-Path -Path $script:RepoPath -ChildPath 'newfile.txt'
        Set-Content -Path $NewFile -Value 'hello'

        Add-GitItem -RepoPath $script:RepoPath -Path 'newfile.txt'

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 1
    }

    It 'Stages a modified tracked file' {
        $ExistingFile = Get-ChildItem -Path $script:RepoPath -Filter 'file_*.txt' | Select-Object -First 1
        Set-Content -Path $ExistingFile.FullName -Value 'modified content'

        Add-GitItem -RepoPath $script:RepoPath -Path $ExistingFile.Name

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 1
    }
}

Describe 'Add-GitItem -All' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Stages all changes when -All is specified' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'a.txt') -Value 'a'
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'b.txt') -Value 'b'

        Add-GitItem -RepoPath $script:RepoPath -All

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 2
        $Status.UntrackedCount | Should -Be 0
    }
}

Describe 'Add-GitItem error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Add-GitItem -RepoPath 'C:\nonexistent\repo\path' -Path 'file.txt' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
