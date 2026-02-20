#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Save-GitCommit cmdlet.
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

Describe 'Save-GitCommit basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Creates a commit with the given message and returns GitCommitInfo' {
        Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'new.txt') -Value 'content'
        Add-GitItem -RepoPath $script:RepoPath -Path 'new.txt'

        $Result = Save-GitCommit -RepoPath $script:RepoPath -Message 'Add new file'

        $Result | Should -Not -BeNullOrEmpty
        $Result.MessageShort | Should -BeExactly 'Add new file'
        $Result.Sha | Should -Match '^[0-9a-f]{40}$'
    }

    It 'The new commit appears in Get-GitLog' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -MaxCount 1)
        $Commits[0].MessageShort | Should -BeExactly 'Add new file'
    }

    It 'Reports the correct author from git config' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -MaxCount 1)
        $Commits[0].AuthorName | Should -BeExactly 'Test Author'
        $Commits[0].AuthorEmail | Should -BeExactly 'test@example.com'
    }
}

Describe 'Save-GitCommit -Amend' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Amends the previous commit with a new message' {
        $Result = Save-GitCommit -RepoPath $script:RepoPath -Message 'Amended message' -Amend

        $Result | Should -Not -BeNullOrEmpty
        $Result.MessageShort | Should -BeExactly 'Amended message'

        $Commits = @(Get-GitLog -RepoPath $script:RepoPath)
        $Commits | Should -HaveCount 1
        $Commits[0].MessageShort | Should -BeExactly 'Amended message'
    }
}

Describe 'Save-GitCommit -AllowEmpty' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Creates an empty commit when -AllowEmpty is specified' {
        $Result = Save-GitCommit -RepoPath $script:RepoPath -Message 'Empty commit' -AllowEmpty

        $Result | Should -Not -BeNullOrEmpty
        $Result.MessageShort | Should -BeExactly 'Empty commit'

        $Commits = @(Get-GitLog -RepoPath $script:RepoPath)
        $Commits | Should -HaveCount 2
    }
}

Describe 'Save-GitCommit error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Save-GitCommit -RepoPath 'C:\nonexistent\repo\path' -Message 'test' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
