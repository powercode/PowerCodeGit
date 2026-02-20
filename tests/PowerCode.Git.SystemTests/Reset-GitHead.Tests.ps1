#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Reset-GitHead cmdlet.
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

Describe 'Reset-GitHead mixed (default)' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Unstages a staged file with default mixed reset' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'staged.txt') -Value 'content'
        Add-GitItem -RepoPath $script:RepoPath -Path 'staged.txt'

        $StatusBefore = Get-GitStatus -RepoPath $script:RepoPath
        $StatusBefore.StagedCount | Should -BeGreaterOrEqual 1

        Reset-GitHead -RepoPath $script:RepoPath -Confirm:$false

        $StatusAfter = Get-GitStatus -RepoPath $script:RepoPath
        $StatusAfter.StagedCount | Should -Be 0
        $StatusAfter.UntrackedCount | Should -BeGreaterOrEqual 1
    }
}

Describe 'Reset-GitHead -Soft' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('First', 'Second')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Moves HEAD back but keeps changes staged' {
        $CommitsBefore = @(Get-GitLog -RepoPath $script:RepoPath)
        $FirstSha = ($CommitsBefore | Where-Object { $_.MessageShort -eq 'First' }).Sha

        Reset-GitHead -RepoPath $script:RepoPath -Revision $FirstSha -Soft -Confirm:$false

        $CommitsAfter = @(Get-GitLog -RepoPath $script:RepoPath)
        $CommitsAfter | Should -HaveCount 1
        $CommitsAfter[0].MessageShort | Should -BeExactly 'First'

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -BeGreaterOrEqual 1
    }
}

Describe 'Reset-GitHead -Hard' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('First', 'Second')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Moves HEAD back and discards all changes' {
        $CommitsBefore = @(Get-GitLog -RepoPath $script:RepoPath)
        $FirstSha = ($CommitsBefore | Where-Object { $_.MessageShort -eq 'First' }).Sha

        Reset-GitHead -RepoPath $script:RepoPath -Revision $FirstSha -Hard -Confirm:$false

        $CommitsAfter = @(Get-GitLog -RepoPath $script:RepoPath)
        $CommitsAfter | Should -HaveCount 1
        $CommitsAfter[0].MessageShort | Should -BeExactly 'First'

        $Status = Get-GitStatus -RepoPath $script:RepoPath
        $Status.StagedCount | Should -Be 0
        $Status.ModifiedCount | Should -Be 0
    }
}

Describe 'Reset-GitHead error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Reset-GitHead -RepoPath 'C:\nonexistent\repo\path' -Confirm:$false -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
