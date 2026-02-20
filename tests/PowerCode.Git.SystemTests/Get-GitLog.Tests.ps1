#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitLog cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    against real git repositories created in temporary directories.
#>

BeforeAll {
    # Resolve the module path from the environment variable set by Invoke-SystemTests.ps1,
    # or fall back to discovering the versioned module layout under artifacts/module/.
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
        <#
        .SYNOPSIS
            Creates a temporary git repository with one or more commits.
        .PARAMETER CommitMessages
            An array of commit messages to create. Defaults to a single commit.
        .PARAMETER AuthorName
            The author name for commits. Defaults to 'Test Author'.
        .PARAMETER AuthorEmail
            The author email for commits. Defaults to 'test@example.com'.
        #>
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
        <#
        .SYNOPSIS
            Removes a temporary git repository, handling read-only files.
        .PARAMETER Path
            The path to the repository to remove.
        #>
        [CmdletBinding()]
        param(
            [Parameter(Mandatory)]
            [string]$Path
        )

        if (Test-Path -Path $Path) {
            # Git objects are often read-only; clear the attribute before deleting
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

Describe 'Get-GitLog basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns commits from a valid repository' {
        $Commits = Get-GitLog -RepoPath $script:RepoPath
        $Commits | Should -Not -BeNullOrEmpty
    }

    It 'Returns GitCommitInfo objects with expected properties' {
        $Commit = Get-GitLog -RepoPath $script:RepoPath | Select-Object -First 1

        $Commit.Sha | Should -Match '^[0-9a-f]{40}$'
        $Commit.ShortSha.Length | Should -Be 7
        $Commit.AuthorName | Should -BeExactly 'Test Author'
        $Commit.AuthorEmail | Should -BeExactly 'test@example.com'
        $Commit.AuthorDate | Should -BeOfType 'System.DateTimeOffset'
        $Commit.CommitterName | Should -BeExactly 'Test Author'
        $Commit.CommitterEmail | Should -BeExactly 'test@example.com'
        $Commit.CommitDate | Should -BeOfType 'System.DateTimeOffset'
        $Commit.MessageShort | Should -BeExactly 'Initial commit'
        $Commit.Message | Should -Match 'Initial commit'
        $Commit.ParentShas | Should -HaveCount 0
    }
}

Describe 'Get-GitLog -MaxCount' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('First', 'Second', 'Third')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns all commits when MaxCount is not specified' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath)
        $Commits | Should -HaveCount 3
    }

    It 'Limits results when MaxCount is specified' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -MaxCount 2)
        $Commits | Should -HaveCount 2
    }

    It 'Returns most recent commits first' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -MaxCount 1)
        $Commits[0].MessageShort | Should -BeExactly 'Third'
    }
}

Describe 'Get-GitLog -Author' {
    BeforeAll {
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -Path $script:RepoPath -ItemType Directory -Force | Out-Null

        Push-Location -Path $script:RepoPath
        try {
            git init --initial-branch main 2>&1 | Out-Null

            # Commit as Alice
            git config user.name 'Alice Smith'
            git config user.email 'alice@example.com'
            Set-Content -Path 'alice.txt' -Value 'alice work'
            git add . 2>&1 | Out-Null
            git commit -m 'Alice commit' 2>&1 | Out-Null

            # Commit as Bob
            git config user.name 'Bob Jones'
            git config user.email 'bob@example.com'
            Set-Content -Path 'bob.txt' -Value 'bob work'
            git add . 2>&1 | Out-Null
            git commit -m 'Bob commit' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Filters commits by author name' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -Author 'Alice')
        $Commits | Should -HaveCount 1
        $Commits[0].AuthorName | Should -BeExactly 'Alice Smith'
    }

    It 'Filters commits by author email' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -Author 'bob@example.com')
        $Commits | Should -HaveCount 1
        $Commits[0].AuthorEmail | Should -BeExactly 'bob@example.com'
    }

    It 'Returns nothing when author does not match' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -Author 'Nonexistent')
        $Commits | Should -HaveCount 0
    }
}

Describe 'Get-GitLog -MessagePattern' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('fix: resolve bug', 'feat: add feature', 'fix: another bug')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Filters commits by message pattern' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -MessagePattern 'fix:')
        $Commits | Should -HaveCount 2
    }

    It 'Performs case-insensitive matching' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -MessagePattern 'FIX:')
        $Commits | Should -HaveCount 2
    }

    It 'Returns nothing when pattern does not match' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -MessagePattern 'chore:')
        $Commits | Should -HaveCount 0
    }
}

Describe 'Get-GitLog -Since and -Until' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Old commit', 'Recent commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Filters commits newer than Since date' {
        # All commits are from today, so using yesterday should return all
        $Yesterday = (Get-Date).AddDays(-1)
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -Since $Yesterday)
        $Commits | Should -HaveCount 2
    }

    It 'Filters commits older than Until date' {
        # Using tomorrow should return all commits
        $Tomorrow = (Get-Date).AddDays(1)
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -Until $Tomorrow)
        $Commits | Should -HaveCount 2
    }

    It 'Returns nothing when date range excludes all commits' {
        $FarFuture = (Get-Date).AddYears(100)
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -Since $FarFuture)
        $Commits | Should -HaveCount 0
    }
}

Describe 'Get-GitLog -Branch' {
    BeforeAll {
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -Path $script:RepoPath -ItemType Directory -Force | Out-Null

        Push-Location -Path $script:RepoPath
        try {
            git init --initial-branch main 2>&1 | Out-Null
            git config user.name 'Test Author'
            git config user.email 'test@example.com'

            Set-Content -Path 'main.txt' -Value 'main content'
            git add . 2>&1 | Out-Null
            git commit -m 'Main commit' 2>&1 | Out-Null

            git checkout -b feature 2>&1 | Out-Null
            Set-Content -Path 'feature.txt' -Value 'feature content'
            git add . 2>&1 | Out-Null
            git commit -m 'Feature commit' 2>&1 | Out-Null

            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns commits from the specified branch' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -Branch 'feature')
        $Commits | Should -HaveCount 2
        $Commits[0].MessageShort | Should -BeExactly 'Feature commit'
        $Commits[1].MessageShort | Should -BeExactly 'Main commit'
    }

    It 'Returns only main branch commits when branch is main' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -Branch 'main')
        $Commits | Should -HaveCount 1
        $Commits[0].MessageShort | Should -BeExactly 'Main commit'
    }
}

Describe 'Get-GitLog error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Commits = Get-GitLog -RepoPath 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Commits | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitLog with multiple parameters combined' {
    BeforeAll {
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -Path $script:RepoPath -ItemType Directory -Force | Out-Null

        Push-Location -Path $script:RepoPath
        try {
            git init --initial-branch main 2>&1 | Out-Null

            git config user.name 'Alice'
            git config user.email 'alice@example.com'
            Set-Content -Path 'a1.txt' -Value 'a1'
            git add . 2>&1 | Out-Null
            git commit -m 'fix: alice fix' 2>&1 | Out-Null

            git config user.name 'Bob'
            git config user.email 'bob@example.com'
            Set-Content -Path 'b1.txt' -Value 'b1'
            git add . 2>&1 | Out-Null
            git commit -m 'fix: bob fix' 2>&1 | Out-Null

            git config user.name 'Alice'
            git config user.email 'alice@example.com'
            Set-Content -Path 'a2.txt' -Value 'a2'
            git add . 2>&1 | Out-Null
            git commit -m 'feat: alice feature' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Combines Author and MessagePattern filters' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -Author 'Alice' -MessagePattern 'fix:')
        $Commits | Should -HaveCount 1
        $Commits[0].MessageShort | Should -BeExactly 'fix: alice fix'
    }

    It 'Combines Author, MessagePattern, and MaxCount' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath -Author 'Alice' -MaxCount 1)
        $Commits | Should -HaveCount 1
        $Commits[0].MessageShort | Should -BeExactly 'feat: alice feature'
    }
}

Describe 'Get-GitLog parent commit tracking' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('First', 'Second')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'First commit has no parents' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath)
        $FirstCommit = $Commits | Where-Object { $_.MessageShort -eq 'First' }
        $FirstCommit.ParentShas | Should -HaveCount 0
    }

    It 'Second commit has one parent' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath)
        $SecondCommit = $Commits | Where-Object { $_.MessageShort -eq 'Second' }
        $SecondCommit.ParentShas | Should -HaveCount 1
    }

    It 'Parent SHA references the first commit' {
        $Commits = @(Get-GitLog -RepoPath $script:RepoPath)
        $FirstCommit = $Commits | Where-Object { $_.MessageShort -eq 'First' }
        $SecondCommit = $Commits | Where-Object { $_.MessageShort -eq 'Second' }
        $SecondCommit.ParentShas[0] | Should -BeExactly $FirstCommit.Sha
    }
}
