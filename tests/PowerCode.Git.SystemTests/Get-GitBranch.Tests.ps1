#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitBranch cmdlet.
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

Describe 'Get-GitBranch basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns branch objects from a valid repository' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath)
        $Branches | Should -Not -BeNullOrEmpty
    }

    It 'Lists the main branch' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath)
        $Branches | Where-Object { $_.Name -eq 'main' } | Should -Not -BeNullOrEmpty
    }

    It 'Marks HEAD branch with IsHead' {
        $HeadBranch = Get-GitBranch -RepoPath $script:RepoPath | Where-Object { $_.IsHead }
        $HeadBranch | Should -Not -BeNullOrEmpty
        $HeadBranch.Name | Should -BeExactly 'main'
    }
}

Describe 'Get-GitBranch multiple branches' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature 2>&1 | Out-Null
            git checkout -b develop 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Lists all local branches' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath | Where-Object { -not $_.IsRemote })
        $Branches.Count | Should -BeGreaterOrEqual 3
    }

    It 'Branch has TipSha populated' {
        $Branch = Get-GitBranch -RepoPath $script:RepoPath | Select-Object -First 1
        $Branch.TipSha | Should -Match '^[0-9a-f]{40}$'
        $Branch.TipShortSha.Length | Should -Be 7
    }
}

Describe 'Get-GitBranch error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Get-GitBranch -RepoPath 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitBranch -Remote flag' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Returns only remote-tracking branches when -Remote is specified' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Remote)
        $Branches | Should -Not -BeNullOrEmpty
        $Branches | Where-Object { -not $_.IsRemote } | Should -BeNullOrEmpty
    }

    It 'Returns no local branches when -Remote is specified' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Remote)
        foreach ($Branch in $Branches) {
            $Branch.IsRemote | Should -BeTrue -Because "All returned branches should be remote-tracking"
        }
    }
}

Describe 'Get-GitBranch -All flag' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Returns both local and remote-tracking branches when -All is specified' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -All)
        $LocalBranches = @($Branches | Where-Object { -not $_.IsRemote })
        $RemoteBranches = @($Branches | Where-Object { $_.IsRemote })
        $LocalBranches | Should -Not -BeNullOrEmpty
        $RemoteBranches | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitBranch -Pattern filter' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature/login  2>&1 | Out-Null
            git checkout -b feature/dashboard 2>&1 | Out-Null
            git checkout -b bugfix/crash 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns only feature/* branches when -Pattern feature/* is specified' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Pattern 'feature/*')
        $Branches.Count | Should -Be 2
        $Branches | ForEach-Object { $_.Name | Should -BeLike 'feature/*' }
    }

    It 'Returns no results when pattern does not match any branch' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Pattern 'release/*')
        $Branches | Should -BeNullOrEmpty
    }
}

Describe 'Get-GitBranch -Merged and -NoMerged filters' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            # Create merged-branch: branch off, commit, merge back to main
            git checkout -b merged-branch 2>&1 | Out-Null
            Set-Content -Path 'merged.txt' -Value 'merged'
            git add . 2>&1 | Out-Null
            git commit -m 'Merged feature' 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
            git merge --ff-only merged-branch 2>&1 | Out-Null

            # Create unmerged-branch: branch off, commit but do NOT merge
            git checkout -b unmerged-branch 2>&1 | Out-Null
            Set-Content -Path 'unmerged.txt' -Value 'unmerged'
            git add . 2>&1 | Out-Null
            git commit -m 'Unmerged feature' 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It '-Merged HEAD shows merged-branch' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Merged HEAD)
        $Branches | Where-Object { $_.Name -eq 'merged-branch' } | Should -Not -BeNullOrEmpty
    }

    It '-Merged HEAD excludes unmerged-branch' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Merged HEAD)
        $Branches | Where-Object { $_.Name -eq 'unmerged-branch' } | Should -BeNullOrEmpty
    }

    It '-NoMerged HEAD shows unmerged-branch' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -NoMerged HEAD)
        $Branches | Where-Object { $_.Name -eq 'unmerged-branch' } | Should -Not -BeNullOrEmpty
    }

    It '-NoMerged HEAD excludes merged-branch' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -NoMerged HEAD)
        $Branches | Where-Object { $_.Name -eq 'merged-branch' } | Should -BeNullOrEmpty
    }
}

Describe 'Get-GitBranch -Options catch-all' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature/x 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Accepts a GitBranchListOptions object via -Options' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitBranchListOptions]::new()
        $Opts.RepositoryPath = $script:RepoPath
        $Opts.Pattern = 'feature/*'

        $Branches = @(Get-GitBranch -Options $Opts)
        $Branches.Count | Should -Be 1
        $Branches[0].Name | Should -Be 'feature/x'
    }
}

Describe 'Get-GitBranch -Include filter' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature/login     2>&1 | Out-Null
            git checkout -b feature/dashboard 2>&1 | Out-Null
            git checkout -b bugfix/crash      2>&1 | Out-Null
            git checkout -b release/v1        2>&1 | Out-Null
            git checkout main                 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns only branches matching a single include pattern' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Include 'feature/*')
        $Branches.Count | Should -Be 2
        $Branches | ForEach-Object { $_.Name | Should -BeLike 'feature/*' }
    }

    It 'Returns branches matching any of multiple include patterns' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Include 'feature/*', 'bugfix/*')
        $Branches.Count | Should -Be 3
        $Branches | ForEach-Object { $_.Name | Should -Match '^(feature|bugfix)/' }
    }

    It 'Returns no branches when include pattern matches nothing' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Include 'hotfix/*')
        $Branches | Should -BeNullOrEmpty
    }
}

Describe 'Get-GitBranch -Exclude filter' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature/login     2>&1 | Out-Null
            git checkout -b feature/dashboard 2>&1 | Out-Null
            git checkout -b bugfix/crash      2>&1 | Out-Null
            git checkout main                 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Removes branches matching the exclude pattern' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Exclude 'bugfix/*')
        $Branches | Where-Object { $_.Name -like 'bugfix/*' } | Should -BeNullOrEmpty
        $Branches.Count | Should -Be 3 # main, feature/login, feature/dashboard
    }

    It 'Removes branches matching any of multiple exclude patterns' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Exclude 'feature/*', 'bugfix/*')
        $Branches.Count | Should -Be 1
        $Branches[0].Name | Should -Be 'main'
    }
}

Describe 'Get-GitBranch -Include and -Exclude combined' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature/login     2>&1 | Out-Null
            git checkout -b feature/dashboard 2>&1 | Out-Null
            git checkout -b feature/temp      2>&1 | Out-Null
            git checkout -b bugfix/crash      2>&1 | Out-Null
            git checkout main                 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Include then exclude narrows results correctly' {
        $Branches = @(Get-GitBranch -RepoPath $script:RepoPath -Include 'feature/*' -Exclude 'feature/temp')
        $Branches.Count | Should -Be 2
        $Branches | ForEach-Object { $_.Name | Should -BeLike 'feature/*' }
        $Branches | Where-Object { $_.Name -eq 'feature/temp' } | Should -BeNullOrEmpty
    }
}
