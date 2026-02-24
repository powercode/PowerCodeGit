#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the New-GitWorktree cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    against real git repositories created in temporary directories.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'New-GitWorktree basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_newwt_$([System.Guid]::NewGuid().ToString('N'))"
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree remove $script:WorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Creates a new worktree and returns GitWorktreeInfo' {
        $Result = New-GitWorktree -RepoPath $script:RepoPath -Name 'new-wt' -Path $script:WorktreePath

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'new-wt'
    }

    It 'The new worktree appears in Get-GitWorktree' {
        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Found = $Worktrees | Where-Object { $_.Name -eq 'new-wt' }
        $Found | Should -Not -BeNullOrEmpty
    }
}

Describe 'New-GitWorktree with branch' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_branchwt_$([System.Guid]::NewGuid().ToString('N'))"

        Push-Location -Path $script:RepoPath
        try {
            git branch feature 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree remove $script:WorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # Help Example 2 - Create a worktree for an existing branch
    It 'Creates a worktree for a specific branch with a different name' {
        $Result = New-GitWorktree -RepoPath $script:RepoPath -Name 'feature-wt' -Path $script:WorktreePath -Branch 'feature'

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'feature-wt'
    }
}

Describe 'New-GitWorktree rejects -Branch equal to -Name' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_samebranchwt_$([System.Guid]::NewGuid().ToString('N'))"
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Throws when -Name and -Branch are identical' {
        { New-GitWorktree -RepoPath $script:RepoPath -Name 'main' -Path $script:WorktreePath -Branch 'main' } |
            Should -Throw
    }
}

Describe 'New-GitWorktree with -Locked' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_lockedwt_$([System.Guid]::NewGuid().ToString('N'))"
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree remove $script:WorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Creates a locked worktree' {
        $Result = New-GitWorktree -RepoPath $script:RepoPath -Name 'locked-wt' -Path $script:WorktreePath -Locked

        $Result | Should -Not -BeNullOrEmpty
        $Result.IsLocked | Should -BeTrue
    }
}

Describe 'New-GitWorktree -WhatIf' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_whatifwt_$([System.Guid]::NewGuid().ToString('N'))"
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Does not create worktree with -WhatIf' {
        New-GitWorktree -RepoPath $script:RepoPath -Name 'whatif-wt' -Path $script:WorktreePath -WhatIf

        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktrees | Where-Object { $_.Name -eq 'whatif-wt' } | Should -BeNullOrEmpty
    }
}

Describe 'New-GitWorktree pipeline with explicit path' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_pipewt_$([System.Guid]::NewGuid().ToString('N'))"

        Push-Location -Path $script:RepoPath
        try {
            git branch feature 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree remove $script:WorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # Help Example 3 - Pipe a branch to create a worktree with explicit path
    It 'Creates a worktree from piped branch with explicit path' {
        $Result = Get-GitBranch -RepoPath $script:RepoPath -Include 'feature' | New-GitWorktree -RepoPath $script:RepoPath -Path $script:WorktreePath

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'feature.wt'
    }

    It 'The piped worktree appears in Get-GitWorktree' {
        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Found = $Worktrees | Where-Object { $_.Name -eq 'feature.wt' }
        $Found | Should -Not -BeNullOrEmpty
    }
}

Describe 'New-GitWorktree pipeline with default path' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:RepoName = Split-Path -Path $script:RepoPath -Leaf
        $script:DefaultWorktreePath = Join-Path -Path (Split-Path -Path $script:RepoPath -Parent) -ChildPath "$($script:RepoName)-main"

        Push-Location -Path $script:RepoPath
        try {
            git branch develop 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        $script:DefaultWorktreePath = Join-Path -Path (Split-Path -Path $script:RepoPath -Parent) -ChildPath "$($script:RepoName)-develop"
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree remove $script:DefaultWorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:DefaultWorktreePath) {
            Remove-Item -Path $script:DefaultWorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # Help Example 4 - Pipe a branch to create a worktree with default path
    It 'Creates a worktree from piped branch with default path next to repo' {
        $Result = Get-GitBranch -RepoPath $script:RepoPath -Include 'develop' | New-GitWorktree -RepoPath $script:RepoPath

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'develop.wt'
        $Result.Path | Should -BeExactly $script:DefaultWorktreePath
    }
}

Describe 'New-GitWorktree positional branch — default name and path' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:RepoName = Split-Path -Path $script:RepoPath -Leaf
        $script:DefaultWorktreePath = Join-Path -Path (Split-Path -Path $script:RepoPath -Parent) -ChildPath "$($script:RepoName)-feature"

        Push-Location -Path $script:RepoPath
        try {
            git branch feature 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree remove $script:DefaultWorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:DefaultWorktreePath) {
            Remove-Item -Path $script:DefaultWorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # Help Example 1 - Positional branch derives name (<branch>.wt) and path (../repo-branch)
    It 'Creates a worktree using positional branch argument with derived name and path' {
        $Result = New-GitWorktree -RepoPath $script:RepoPath feature

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'feature.wt'
        $Result.Path | Should -BeExactly $script:DefaultWorktreePath
    }

    It 'The derived worktree appears in Get-GitWorktree' {
        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktrees | Where-Object { $_.Name -eq 'feature.wt' } | Should -Not -BeNullOrEmpty
    }
}

Describe 'New-GitWorktree positional branch with slashes — safe name' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:RepoName = Split-Path -Path $script:RepoPath -Leaf
        $script:DefaultWorktreePath = Join-Path -Path (Split-Path -Path $script:RepoPath -Parent) -ChildPath "$($script:RepoName)-feature-login"

        Push-Location -Path $script:RepoPath
        try {
            git branch 'feature/login' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree remove $script:DefaultWorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:DefaultWorktreePath) {
            Remove-Item -Path $script:DefaultWorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Replaces slashes with dashes in the derived worktree name and path' {
        $Result = New-GitWorktree -RepoPath $script:RepoPath 'feature/login'

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'feature-login.wt'
        $Result.Path | Should -BeExactly $script:DefaultWorktreePath
    }
}

Describe 'New-GitWorktree positional branch with explicit -Name override' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_branchname_$([System.Guid]::NewGuid().ToString('N'))"

        Push-Location -Path $script:RepoPath
        try {
            git branch feature 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree remove $script:WorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # Help Example 2 - Positional branch with explicit -Name and -Path
    It 'Uses the provided -Name and -Path when both are supplied alongside a positional branch' {
        $Result = New-GitWorktree -RepoPath $script:RepoPath feature -Name 'my-wt' -Path $script:WorktreePath

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'my-wt'
        $Result.Path | Should -BeExactly $script:WorktreePath
    }
}

Describe 'New-GitWorktree positional branch with explicit -Path override' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_branchpath_$([System.Guid]::NewGuid().ToString('N'))"

        Push-Location -Path $script:RepoPath
        try {
            git branch hotfix 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Push-Location -Path $script:RepoPath
        try {
            git worktree remove $script:WorktreePath --force 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Derives name from branch but uses the explicit -Path' {
        $Result = New-GitWorktree -RepoPath $script:RepoPath hotfix -Path $script:WorktreePath

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'hotfix.wt'
        $Result.Path | Should -BeExactly $script:WorktreePath
    }
}

Describe 'New-GitWorktree fails on non-empty target directory' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        $script:WorktreePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_nonempty_$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -Path $script:WorktreePath -ItemType Directory -Force | Out-Null
        Set-Content -Path (Join-Path -Path $script:WorktreePath -ChildPath 'existing.txt') -Value 'content'
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
        if (Test-Path -Path $script:WorktreePath) {
            Remove-Item -Path $script:WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Throws when the target directory is non-empty' {
        { New-GitWorktree -RepoPath $script:RepoPath -Name 'test-wt' -Path $script:WorktreePath } |
            Should -Throw '*already exists and is not empty*'
    }

    It 'Does not emit a worktree entry after the failure' {
        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktrees | Where-Object { $_.Name -eq 'test-wt' } | Should -BeNullOrEmpty
    }
}

Describe 'New-GitWorktree positional branch -WhatIf' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git branch feature 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Does not create a worktree when -WhatIf is passed with a positional branch' {
        $RepoName = Split-Path -Path $script:RepoPath -Leaf
        $ExpectedPath = Join-Path -Path (Split-Path -Path $script:RepoPath -Parent) -ChildPath "$RepoName-feature"

        New-GitWorktree -RepoPath $script:RepoPath feature -WhatIf

        $Worktrees = @(Get-GitWorktree -RepoPath $script:RepoPath)
        $Worktrees | Where-Object { $_.Name -eq 'feature.wt' } | Should -BeNullOrEmpty
        Test-Path -Path $ExpectedPath | Should -BeFalse
    }
}
