#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitLog cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerGit binary module
    against real git repositories created in temporary directories.
#>

BeforeAll {
    $RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path
    $ModulePath = Join-Path -Path $RepoRoot -ChildPath 'artifacts/bin/PowerGit/debug/PowerGit.psd1'

    if (-not (Test-Path -Path $ModulePath)) {
        throw "Module not found at '$ModulePath'. Build the solution before running system tests."
    }

    # Preload platform-specific native LibGit2Sharp binaries from
    # runtimes/{rid}/native without modifying global process PATH.
    $ModuleDir = Split-Path -Path $ModulePath -Parent

    $Rid = switch ([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture) {
        'X64' { 'win-x64' }
        'X86' { 'win-x86' }
        'Arm64' { 'win-arm64' }
        default { throw "Unsupported process architecture '$([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)' for system tests." }
    }

    $NativePath = Join-Path -Path $ModuleDir -ChildPath "runtimes/$Rid/native"
    if (-not (Test-Path -Path $NativePath)) {
        throw "Native runtime directory not found: '$NativePath'."
    }

    $NativeSupportLibrary = Join-Path -Path $NativePath -ChildPath 'getfilesiginforedist.dll'
    if (Test-Path -Path $NativeSupportLibrary) {
        [System.Runtime.InteropServices.NativeLibrary]::Load($NativeSupportLibrary) | Out-Null
    }

    $Git2Library = Get-ChildItem -Path $NativePath -Filter 'git2-*.dll' | Select-Object -First 1
    if ($null -eq $Git2Library) {
        throw "Could not locate git2 native library in '$NativePath'."
    }

    [System.Runtime.InteropServices.NativeLibrary]::Load($Git2Library.FullName) | Out-Null

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
    Remove-Module -Name PowerGit -Force -ErrorAction SilentlyContinue
}

Describe 'Get-GitLog basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns commits from a valid repository' {
        $Commits = Get-GitLog -Path $script:RepoPath
        $Commits | Should -Not -BeNullOrEmpty
    }

    It 'Returns GitCommitInfo objects with expected properties' {
        $Commit = Get-GitLog -Path $script:RepoPath | Select-Object -First 1

        $Commit.Sha | Should -Match '^[0-9a-f]{40}$'
        $Commit.ShortSha | Should -HaveLength 7
        $Commit.AuthorName | Should -BeExactly 'Test Author'
        $Commit.AuthorEmail | Should -BeExactly 'test@example.com'
        $Commit.AuthorDate | Should -BeOfType [System.DateTimeOffset]
        $Commit.CommitterName | Should -BeExactly 'Test Author'
        $Commit.CommitterEmail | Should -BeExactly 'test@example.com'
        $Commit.CommitDate | Should -BeOfType [System.DateTimeOffset]
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
        $Commits = @(Get-GitLog -Path $script:RepoPath)
        $Commits | Should -HaveCount 3
    }

    It 'Limits results when MaxCount is specified' {
        $Commits = @(Get-GitLog -Path $script:RepoPath -MaxCount 2)
        $Commits | Should -HaveCount 2
    }

    It 'Returns most recent commits first' {
        $Commits = @(Get-GitLog -Path $script:RepoPath -MaxCount 1)
        $Commits[0].MessageShort | Should -BeExactly 'Third'
    }
}

Describe 'Get-GitLog -Author' {
    BeforeAll {
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerGitTest_$([System.Guid]::NewGuid().ToString('N'))"
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
        $Commits = @(Get-GitLog -Path $script:RepoPath -Author 'Alice')
        $Commits | Should -HaveCount 1
        $Commits[0].AuthorName | Should -BeExactly 'Alice Smith'
    }

    It 'Filters commits by author email' {
        $Commits = @(Get-GitLog -Path $script:RepoPath -Author 'bob@example.com')
        $Commits | Should -HaveCount 1
        $Commits[0].AuthorEmail | Should -BeExactly 'bob@example.com'
    }

    It 'Returns nothing when author does not match' {
        $Commits = @(Get-GitLog -Path $script:RepoPath -Author 'Nonexistent')
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
        $Commits = @(Get-GitLog -Path $script:RepoPath -MessagePattern 'fix:')
        $Commits | Should -HaveCount 2
    }

    It 'Performs case-insensitive matching' {
        $Commits = @(Get-GitLog -Path $script:RepoPath -MessagePattern 'FIX:')
        $Commits | Should -HaveCount 2
    }

    It 'Returns nothing when pattern does not match' {
        $Commits = @(Get-GitLog -Path $script:RepoPath -MessagePattern 'chore:')
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
        $Commits = @(Get-GitLog -Path $script:RepoPath -Since $Yesterday)
        $Commits | Should -HaveCount 2
    }

    It 'Filters commits older than Until date' {
        # Using tomorrow should return all commits
        $Tomorrow = (Get-Date).AddDays(1)
        $Commits = @(Get-GitLog -Path $script:RepoPath -Until $Tomorrow)
        $Commits | Should -HaveCount 2
    }

    It 'Returns nothing when date range excludes all commits' {
        $FarFuture = (Get-Date).AddYears(100)
        $Commits = @(Get-GitLog -Path $script:RepoPath -Since $FarFuture)
        $Commits | Should -HaveCount 0
    }
}

Describe 'Get-GitLog -Branch' {
    BeforeAll {
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerGitTest_$([System.Guid]::NewGuid().ToString('N'))"
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
        $Commits = @(Get-GitLog -Path $script:RepoPath -Branch 'feature')
        $Commits | Should -HaveCount 2
        $Commits[0].MessageShort | Should -BeExactly 'Feature commit'
        $Commits[1].MessageShort | Should -BeExactly 'Main commit'
    }

    It 'Returns only main branch commits when branch is main' {
        $Commits = @(Get-GitLog -Path $script:RepoPath -Branch 'main')
        $Commits | Should -HaveCount 1
        $Commits[0].MessageShort | Should -BeExactly 'Main commit'
    }
}

Describe 'Get-GitLog error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Commits = Get-GitLog -Path 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Commits | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitLog with multiple parameters combined' {
    BeforeAll {
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerGitTest_$([System.Guid]::NewGuid().ToString('N'))"
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
        $Commits = @(Get-GitLog -Path $script:RepoPath -Author 'Alice' -MessagePattern 'fix:')
        $Commits | Should -HaveCount 1
        $Commits[0].MessageShort | Should -BeExactly 'fix: alice fix'
    }

    It 'Combines Author, MessagePattern, and MaxCount' {
        $Commits = @(Get-GitLog -Path $script:RepoPath -Author 'Alice' -MaxCount 1)
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
        $Commits = @(Get-GitLog -Path $script:RepoPath)
        $FirstCommit = $Commits | Where-Object { $_.MessageShort -eq 'First' }
        $FirstCommit.ParentShas | Should -HaveCount 0
    }

    It 'Second commit has one parent' {
        $Commits = @(Get-GitLog -Path $script:RepoPath)
        $SecondCommit = $Commits | Where-Object { $_.MessageShort -eq 'Second' }
        $SecondCommit.ParentShas | Should -HaveCount 1
    }

    It 'Parent SHA references the first commit' {
        $Commits = @(Get-GitLog -Path $script:RepoPath)
        $FirstCommit = $Commits | Where-Object { $_.MessageShort -eq 'First' }
        $SecondCommit = $Commits | Where-Object { $_.MessageShort -eq 'Second' }
        $SecondCommit.ParentShas[0] | Should -BeExactly $FirstCommit.Sha
    }
}
