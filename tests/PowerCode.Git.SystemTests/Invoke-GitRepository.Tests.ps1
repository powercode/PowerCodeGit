#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Invoke-GitRepository cmdlet.
.DESCRIPTION
    End-to-end tests that exercise Invoke-GitRepository against real git
    repositories created in temporary directories. Validates that the
    LibGit2Sharp Repository object is accessible via $repo inside the
    ScriptBlock and that results are streamed correctly to the pipeline.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'Invoke-GitRepository basic access' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Second commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns a result from a simple ScriptBlock' {
        $Result = Invoke-GitRepository -RepoPath $script:RepoPath -Action { 'hello' }
        $Result | Should -Be 'hello'
    }

    It 'Provides $repo as a non-null object inside the ScriptBlock' {
        $Result = Invoke-GitRepository -RepoPath $script:RepoPath -Action { $repo }
        $Result | Should -Not -BeNullOrEmpty
    }

    It 'Provides the Repository type via $repo' {
        $Result = Invoke-GitRepository -RepoPath $script:RepoPath -Action { $repo.GetType().Name }
        $Result | Should -Be 'Repository'
    }

    It 'Exposes repository via $args[0]' {
        $Result = Invoke-GitRepository -RepoPath $script:RepoPath -Action { $args[0].GetType().Name }
        $Result | Should -Be 'Repository'
    }

    It 'Reads HEAD SHA via $repo.Head.Tip.Sha' {
        $Sha = Invoke-GitRepository -RepoPath $script:RepoPath -Action { $repo.Head.Tip.Sha }
        $Sha | Should -Match '^[0-9a-f]{40}$'
    }

    It 'Reads HEAD friendly name via $repo.Head.FriendlyName' {
        $Branch = Invoke-GitRepository -RepoPath $script:RepoPath -Action { $repo.Head.FriendlyName }
        $Branch | Should -Be 'main'
    }

    It 'Reads commit count via $repo.Commits' {
        $Count = Invoke-GitRepository -RepoPath $script:RepoPath -Action {
            @($repo.Commits).Count
        }
        $Count | Should -Be 2
    }
}

Describe 'Invoke-GitRepository pipeline output' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('First', 'Second', 'Third')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Streams multiple objects to the pipeline' {
        $Results = @(Invoke-GitRepository -RepoPath $script:RepoPath -Action {
            $repo.Refs | ForEach-Object { $_.CanonicalName }
        })
        $Results | Should -Not -BeNullOrEmpty
        $Results | Should -BeOfType 'string'
    }

    It 'Returns custom objects from a projection ScriptBlock' {
        $Result = Invoke-GitRepository -RepoPath $script:RepoPath -Action {
            [pscustomobject]@{
                Branch = $repo.Head.FriendlyName
                Sha    = $repo.Head.Tip.Sha.Substring(0, 7)
            }
        }
        $Result.Branch | Should -Be 'main'
        $Result.Sha    | Should -Match '^[0-9a-f]{7}$'
    }

    It 'Streams one object per iteration in a loop' {
        $Results = @(Invoke-GitRepository -RepoPath $script:RepoPath -Action {
            foreach ($commit in $repo.Commits) {
                $commit.MessageShort
            }
        })
        $Results | Should -HaveCount 3
        $Results | Should -Contain 'Third'
    }
}

Describe 'Invoke-GitRepository path resolution' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Uses current directory when -RepoPath is omitted' {
        Push-Location -Path $script:RepoPath
        try {
            $Sha = Invoke-GitRepository { $repo.Head.Tip.Sha }
            $Sha | Should -Match '^[0-9a-f]{40}$'
        }
        finally {
            Pop-Location
        }
    }

    It 'Accepts -RepositoryPath alias for -RepoPath' {
        $Sha = Invoke-GitRepository -RepositoryPath $script:RepoPath -Action { $repo.Head.Tip.Sha }
        $Sha | Should -Match '^[0-9a-f]{40}$'
    }
}

Describe 'Invoke-GitRepository error handling' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Writes a non-terminating error for an invalid repository path' {
        $Errors = @()
        Invoke-GitRepository -RepoPath $NonExistentRepoPath -Action { 'should not run' } `
            -ErrorAction SilentlyContinue `
            -ErrorVariable Errors
        $Errors | Should -Not -BeNullOrEmpty
    }

    It 'Propagates ScriptBlock errors to the error stream' {
        $Errors = @()
        Invoke-GitRepository -RepoPath $script:RepoPath -Action { throw 'test error' } `
            -ErrorAction SilentlyContinue `
            -ErrorVariable Errors
        $Errors | Should -Not -BeNullOrEmpty
    }

    It 'Continues executing after a ScriptBlock error with -ErrorAction SilentlyContinue' {
        $Result = Invoke-GitRepository -RepoPath $script:RepoPath -Action { throw 'boom' } `
            -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
    }
}

# Example 1 — List all remote URLs
Describe 'Invoke-GitRepository — remote URL enumeration' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Returns a custom object per remote with Name and Url properties' {
        $Results = @(Invoke-GitRepository -RepoPath $script:Repos.WorkingPath -Action {
            $repo.Network.Remotes | ForEach-Object {
                [pscustomobject]@{ Name = $_.Name; Url = $_.Url; PushUrl = $_.PushUrl }
            }
        })
        $Results | Should -Not -BeNullOrEmpty
        $Results[0].Name | Should -Be 'origin'
        $Results[0].Url  | Should -Not -BeNullOrEmpty
    }
}

# Example 3 — Count refs with explicit -RepoPath
Describe 'Invoke-GitRepository — ref counting with explicit RepoPath' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Second commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Counts refs and reports HEAD SHA via a foreach loop' {
        $Result = Invoke-GitRepository -RepoPath $script:RepoPath -Action {
            $count = 0
            foreach ($ref in $repo.Refs) { $count++ }
            [pscustomobject]@{ RefCount = $count; HeadSha = $repo.Head.Tip.Sha }
        }
        $Result.RefCount | Should -BeGreaterThan 0
        $Result.HeadSha  | Should -Match '^[0-9a-f]{40}$'
    }
}
