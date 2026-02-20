#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitTag cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    against real git repositories created in temporary directories.
#>

BeforeAll {
    if ($env:PowerCode.Git_MODULE_PATH -and (Test-Path -Path $env:PowerCode.Git_MODULE_PATH)) {
        $ModulePath = $env:PowerCode.Git_MODULE_PATH
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

Describe 'Get-GitTag no tags' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns empty when there are no tags' {
        $Tags = @(Get-GitTag -RepoPath $script:RepoPath)
        $Tags | Should -HaveCount 0
    }
}

Describe 'Get-GitTag lightweight tags' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git tag v1.0.0 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Lists lightweight tags' {
        $Tags = @(Get-GitTag -RepoPath $script:RepoPath)
        $Tags | Should -HaveCount 1
        $Tags[0].Name | Should -BeExactly 'v1.0.0'
    }

    It 'Lightweight tag is not annotated' {
        $Tag = Get-GitTag -RepoPath $script:RepoPath | Select-Object -First 1
        $Tag.IsAnnotated | Should -BeFalse
    }

    It 'Tag has a valid SHA' {
        $Tag = Get-GitTag -RepoPath $script:RepoPath | Select-Object -First 1
        $Tag.Sha | Should -Match '^[0-9a-f]{40}$'
        $Tag.ShortSha.Length | Should -Be 7
    }
}

Describe 'Get-GitTag annotated tags' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git tag -a v2.0.0 -m 'Release v2.0.0' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Lists annotated tags' {
        $Tags = @(Get-GitTag -RepoPath $script:RepoPath)
        $Tags | Should -HaveCount 1
        $Tags[0].Name | Should -BeExactly 'v2.0.0'
    }

    It 'Annotated tag has metadata' {
        $Tag = Get-GitTag -RepoPath $script:RepoPath | Select-Object -First 1
        $Tag.IsAnnotated | Should -BeTrue
        $Tag.TaggerName | Should -Not -BeNullOrEmpty
        $Tag.TaggerEmail | Should -Not -BeNullOrEmpty
        $Tag.Message | Should -Match 'Release v2.0.0'
    }
}

Describe 'Get-GitTag multiple tags' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git tag v1.0.0 2>&1 | Out-Null
            git tag -a v2.0.0 -m 'Annotated release' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns all tags' {
        $Tags = @(Get-GitTag -RepoPath $script:RepoPath)
        $Tags | Should -HaveCount 2
    }
}

Describe 'Get-GitTag error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitTag -RepoPath 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
