#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitPromptStatus cmdlet.
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

Describe 'Get-GitPromptStatus basic output' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns a GitPromptStatus object (Example 1)' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        $Result | Should -Not -BeNullOrEmpty
        $Result.GetType().Name | Should -BeExactly 'GitPromptStatus'
    }

    It 'PopulatesRepositoryPath' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        $Result.RepositoryPath | Should -BeExactly $script:RepoPath
    }

    It 'Reports the current branch name' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        $Result.BranchName | Should -BeExactly 'main'
    }

    It 'Reports zero counts on a clean repository' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        $Result.StagedCount    | Should -Be 0
        $Result.ModifiedCount  | Should -Be 0
        $Result.UntrackedCount | Should -Be 0
        $Result.StashCount     | Should -Be 0
    }

    It 'FormattedString contains the branch name' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        $Result.FormattedString | Should -Match 'main'
    }

    It 'ToString returns the FormattedString' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        "$Result" | Should -BeExactly $Result.FormattedString
    }
}

Describe 'Get-GitPromptStatus from current directory' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Gets prompt status from the current directory (Example 2)' {
        Push-Location -Path $script:RepoPath
        try {
            $Result = Get-GitPromptStatus
            $Result | Should -Not -BeNullOrEmpty
            $Result.BranchName | Should -BeExactly 'main'
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Get-GitPromptStatus with dirty working tree' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        # Create an untracked file and a staged change
        Set-Content -Path (Join-Path $script:RepoPath 'untracked.txt') -Value 'untracked'
        Set-Content -Path (Join-Path $script:RepoPath 'staged.txt') -Value 'staged'
        Push-Location -Path $script:RepoPath
        try {
            git add staged.txt 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Reports staged and untracked counts' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        $Result.StagedCount    | Should -Be 1
        $Result.UntrackedCount | Should -Be 1
    }

    It 'FormattedString includes + for staged count' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath -NoColor
        $Result.FormattedString | Should -Match '\+1'
    }

    It 'FormattedString includes ? for untracked count' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath -NoColor
        $Result.FormattedString | Should -Match '\?1'
    }
}

Describe 'Get-GitPromptStatus switch parameters' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        # Add an untracked file so counts are non-zero
        Set-Content -Path (Join-Path $script:RepoPath 'file.txt') -Value 'content'
        Push-Location -Path $script:RepoPath
        try {
            git add file.txt 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It '-NoColor produces no ANSI escape sequences (Example 3)' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath -NoColor
        $Result.FormattedString | Should -Not -Match "`e\["
    }

    It '-HideCounts omits the staged/modified/untracked segments (Example 4)' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath -HideCounts -NoColor
        $Result.FormattedString | Should -Not -Match '\+\d'
    }

    It '-HideUpstream omits the upstream provider icon' {
        # With no remote configured, the default git glyph is shown.
        # HideUpstream should remove the glyph entirely.
        $WithUpstream    = Get-GitPromptStatus -RepoPath $script:RepoPath -NoColor
        $WithoutUpstream = Get-GitPromptStatus -RepoPath $script:RepoPath -NoColor -HideUpstream
        # Hidden version should be shorter (no leading icon + space)
        $WithoutUpstream.FormattedString.Length | Should -BeLessThan $WithUpstream.FormattedString.Length
    }

    It '-HideStash omits the stash segment even when stash count is zero' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath -HideStash -NoColor
        # No stash glyph U+2691 in output
        $Result.FormattedString | Should -Not -Match [char]0x2691
    }
}

Describe 'Get-GitPromptStatus provider detection' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Detects unknown provider for a local bare remote' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        $Result.UpstreamProvider | Should -BeExactly 'Unknown'
    }

    It 'Detects GitHub provider from remote URL' {
        Push-Location -Path $script:RepoPath
        try {
            git remote set-url origin 'https://github.com/user/repo.git' 2>&1 | Out-Null
            $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
            $Result.UpstreamProvider | Should -BeExactly 'GitHub'
        }
        finally {
            Pop-Location
        }
    }

    It 'Detects GitLab provider from remote URL' {
        Push-Location -Path $script:RepoPath
        try {
            git remote set-url origin 'https://gitlab.com/user/repo.git' 2>&1 | Out-Null
            $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
            $Result.UpstreamProvider | Should -BeExactly 'GitLab'
        }
        finally {
            Pop-Location
        }
    }

    It 'Detects Bitbucket provider from remote URL' {
        Push-Location -Path $script:RepoPath
        try {
            git remote set-url origin 'https://bitbucket.org/user/repo.git' 2>&1 | Out-Null
            $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
            $Result.UpstreamProvider | Should -BeExactly 'Bitbucket'
        }
        finally {
            Pop-Location
        }
    }

    It 'Detects AzureDevOps provider from remote URL' {
        Push-Location -Path $script:RepoPath
        try {
            git remote set-url origin 'https://dev.azure.com/org/project/_git/repo' 2>&1 | Out-Null
            $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
            $Result.UpstreamProvider | Should -BeExactly 'AzureDevOps'
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Get-GitPromptStatus with upstream tracking' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Reports TrackedBranchName when upstream is configured' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        $Result.TrackedBranchName | Should -BeExactly 'origin/main'
    }

    It 'Reports AheadBy 0 and BehindBy 0 when in sync with upstream' {
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        $Result.AheadBy  | Should -Be 0
        $Result.BehindBy | Should -Be 0
    }

    It 'Reports AheadBy 1 after a local commit not yet pushed' {
        Push-Location -Path $script:RepoPath
        try {
            Set-Content -Path 'new.txt' -Value 'hello'
            git add new.txt 2>&1 | Out-Null
            git commit -m 'Unpushed commit' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
        $Result = Get-GitPromptStatus -RepoPath $script:RepoPath
        $Result.AheadBy | Should -Be 1
    }
}

Describe 'Get-GitPromptStatus in a prompt function' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Can be used in string interpolation inside a prompt function (Example 2)' {
        Push-Location -Path $script:RepoPath
        try {
            function prompt { "$(Get-GitPromptStatus) > " }
            $PromptOutput = prompt
            $PromptOutput | Should -Match 'main'
            $PromptOutput | Should -Match '> $'
        }
        finally {
            Pop-Location
        }
    }
}
