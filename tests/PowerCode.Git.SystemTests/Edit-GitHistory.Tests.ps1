#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Edit-GitHistory cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module against real git
    repositories created in temporary directories. Tests verify both the ScriptBlock
    delegate bridging across the Assembly Load Context boundary and the safety guards
    (-Force / -WhatIf) that protect against accidental history rewrites.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

# ─── Safety guard tests ─────────────────────────────────────────────────────────

Describe 'Edit-GitHistory safety guards' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Errors when -Force is absent and -WhatIf is not used' {
        { Edit-GitHistory -CommitFilter { $null } -RepoPath $script:RepoPath -ErrorAction Stop } |
            Should -Throw -ExceptionType ([System.Management.Automation.RuntimeException])
    }

    It 'Errors when no filter parameter is provided' {
        { Edit-GitHistory -Force -Confirm:$false -RepoPath $script:RepoPath -ErrorAction Stop } |
            Should -Throw
    }
}

# ─── -WhatIf dry-run tests ──────────────────────────────────────────────────────

Describe 'Edit-GitHistory -WhatIf dry run' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Second commit')
        $script:OriginalLog = @(Get-GitLog -RepoPath $script:RepoPath)
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns GitRewrittenCommitInfo objects describing what would change' {
        $Results = Edit-GitHistory -CommitFilter {
            @{ Author = @{ Name = $commit.Author.Name; Email = 'new@example.com'; When = $commit.Author.When } }
        } -WhatIf -RepoPath $script:RepoPath

        $Results | Should -Not -BeNullOrEmpty
    }

    It 'Does not modify the repository when -WhatIf is used' {
        Edit-GitHistory -CommitFilter {
            @{ Author = @{ Name = $commit.Author.Name; Email = 'new@example.com'; When = $commit.Author.When } }
        } -WhatIf -RepoPath $script:RepoPath

        $LogAfter = @(Get-GitLog -RepoPath $script:RepoPath)
        $LogAfter[0].Sha | Should -BeExactly $script:OriginalLog[0].Sha
        $LogAfter[0].AuthorEmail | Should -BeExactly 'test@example.com'
    }
}

# ─── CommitFilter tests ─────────────────────────────────────────────────────────

Describe 'Edit-GitHistory -CommitFilter' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Second commit') `
            -AuthorEmail 'old@corp.com'
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Rewrites author email across all commits' {
        Edit-GitHistory -CommitFilter {
            if ($commit.Author.Email -eq 'old@corp.com') {
                @{
                    Author    = @{ Name = $commit.Author.Name; Email = 'new@corp.com'; When = $commit.Author.When }
                    Committer = @{ Name = $commit.Committer.Name; Email = 'new@corp.com'; When = $commit.Committer.When }
                }
            }
        } -Force -Confirm:$false -RepoPath $script:RepoPath

        $LogAfter = @(Get-GitLog -RepoPath $script:RepoPath)
        $LogAfter | Should -HaveCount 2
        $LogAfter | ForEach-Object {
            $_.AuthorEmail | Should -BeExactly 'new@corp.com'
        }
    }

    It 'Emits a GitRewrittenCommitInfo per rewritten commit' {
        # Use a fresh repo so previous edits do not interfere.
        $FreshRepo = New-TestGitRepository -CommitMessages @('Commit A') -AuthorEmail 'a@b.com'

        try {
            $Results = Edit-GitHistory -CommitFilter {
                @{ Message = "REWRITTEN: $($commit.MessageShort)`n" }
            } -Force -Confirm:$false -RepoPath $FreshRepo

            $Results | Should -HaveCount 1
            $Results[0] | Should -BeOfType 'PowerCode.Git.Abstractions.Models.GitRewrittenCommitInfo'
            $Results[0].MessageModified | Should -BeTrue
        }
        finally {
            Remove-TestGitRepository -Path $FreshRepo
        }
    }

    It 'Returning $null from the filter keeps the commit unchanged' {
        $NoChangeRepo = New-TestGitRepository -CommitMessages @('Unchanged commit')

        try {
            $Before = @(Get-GitLog -RepoPath $NoChangeRepo)

            $Results = Edit-GitHistory -CommitFilter { $null } -Force -Confirm:$false -RepoPath $NoChangeRepo

            # No modified commits means empty results.
            $Results | Should -HaveCount 0

            $After = @(Get-GitLog -RepoPath $NoChangeRepo)
            $After[0].Sha | Should -BeExactly $Before[0].Sha
        }
        finally {
            Remove-TestGitRepository -Path $NoChangeRepo
        }
    }
}

# ─── TreeFilter tests ──────────────────────────────────────────────────────────

Describe 'Edit-GitHistory -TreeFilter' {
    It 'Removes files matching the filter from all commits' {
        $RepoPath = New-TestGitRepository -CommitMessages @('Add files')

        # Add a .secret file alongside the regular file and commit it separately.
        $SecretFile = Join-Path -Path $RepoPath -ChildPath 'passwords.secret'
        Set-Content -Path $SecretFile -Value 'hunter2'
        Push-Location -Path $RepoPath
        try {
            git add . 2>&1 | Out-Null
            git commit -m 'Accidentally committed secret' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        try {
            Edit-GitHistory -TreeFilter {
                -not ($_.Path -like '*.secret')
            } -Force -Confirm:$false -RepoPath $RepoPath

            # The secret file must have been removed from all commits.
            # Check by inspecting the HEAD tree using git ls-tree.
            $LsTree = git -C $RepoPath ls-tree -r HEAD --name-only
            $LsTree | Should -Not -Contain 'passwords.secret'
        }
        finally {
            Remove-TestGitRepository -Path $RepoPath
        }
    }

    It 'Emits TreeModified = $true for commits where files were removed' {
        $RepoPath = New-TestGitRepository -CommitMessages @('Add files')

        $RemoveFile = Join-Path -Path $RepoPath -ChildPath 'remove-me.tmp'
        Set-Content -Path $RemoveFile -Value 'temp'
        Push-Location -Path $RepoPath
        try {
            git add . 2>&1 | Out-Null
            git commit -m 'Add temp file' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        try {
            $Results = Edit-GitHistory -TreeFilter {
                -not ($_.Path -like '*.tmp')
            } -Force -Confirm:$false -RepoPath $RepoPath

            $Modified = @($Results | Where-Object TreeModified)
            $Modified | Should -HaveCount 1
        }
        finally {
            Remove-TestGitRepository -Path $RepoPath
        }
    }
}

# ─── -PruneEmptyCommits tests ──────────────────────────────────────────────────

Describe 'Edit-GitHistory -PruneEmptyCommits' {
    It 'Removes commits that become empty after TreeFilter is applied' {
        $RepoPath = New-TestGitRepository -CommitMessages @('Initial')

        # Add a commit that only contains a .tmp file.
        $TmpFile = Join-Path -Path $RepoPath -ChildPath 'temp.tmp'
        Set-Content -Path $TmpFile -Value 'temp only'
        Push-Location -Path $RepoPath
        try {
            git add . 2>&1 | Out-Null
            git commit -m 'Temp-only commit' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        $CountBefore = @(Get-GitLog -RepoPath $RepoPath).Count

        try {
            Edit-GitHistory -TreeFilter {
                -not ($_.Path -like '*.tmp')
            } -PruneEmptyCommits -Force -Confirm:$false -RepoPath $RepoPath

            $CountAfter = @(Get-GitLog -RepoPath $RepoPath).Count
            $CountAfter | Should -BeLessThan $CountBefore
        }
        finally {
            Remove-TestGitRepository -Path $RepoPath
        }
    }
}

# ─── Backup refs tests ─────────────────────────────────────────────────────────

Describe 'Edit-GitHistory backup refs' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Creates backup refs under the default namespace after a rewrite' {
        Edit-GitHistory -CommitFilter {
            @{ Message = "Backup test`n" }
        } -Force -Confirm:$false -RepoPath $script:RepoPath

        $Refs = git -C $script:RepoPath for-each-ref --format='%(refname)' 'refs/original/'
        $Refs | Should -Not -BeNullOrEmpty
    }

    It 'Uses the custom -BackupNamespace when specified' {
        $AltRepoPath = New-TestGitRepository -CommitMessages @('Initial')

        try {
            Edit-GitHistory -CommitFilter {
                @{ Message = "Alt backup`n" }
            } -BackupNamespace 'refs/backup/' -Force -Confirm:$false -RepoPath $AltRepoPath

            $Refs = git -C $AltRepoPath for-each-ref --format='%(refname)' 'refs/backup/'
            $Refs | Should -Not -BeNullOrEmpty
        }
        finally {
            Remove-TestGitRepository -Path $AltRepoPath
        }
    }
}

# ─── TagNameRewriter tests ─────────────────────────────────────────────────────

Describe 'Edit-GitHistory -TagNameRewriter' {
    It 'Renames tags pointing to rewritten commits' {
        $RepoPath = New-TestGitRepository -CommitMessages @('Tagged commit')

        Push-Location -Path $RepoPath
        try {
            git tag v1.0 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        try {
            Edit-GitHistory -CommitFilter {
                @{ Message = "Rewritten`n" }
            } -TagNameRewriter {
                "release/$($args[0].TrimStart('v'))"
            } -Force -Confirm:$false -RepoPath $RepoPath

            $Tags = git -C $RepoPath tag --list
            $Tags | Should -Contain 'release/1.0'
        }
        finally {
            Remove-TestGitRepository -Path $RepoPath
        }
    }
}
