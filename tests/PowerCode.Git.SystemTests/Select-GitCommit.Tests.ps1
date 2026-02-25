#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Select-GitCommit cmdlet.
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

Describe 'Select-GitCommit output type' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Add TODO item')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns GitCommitInfo objects' {
        $Result = Select-GitCommit -Contains 'TODO' -RepoPath $script:RepoPath
        $Result | Should -BeOfType 'PowerCode.Git.Abstractions.Models.GitCommitInfo'
    }

    It 'Returns expected properties on matched commit' {
        $Result = Select-GitCommit -Contains 'TODO' -RepoPath $script:RepoPath

        $Result.Sha | Should -Match '^[0-9a-f]{40}$'
        $Result.ShortSha.Length | Should -Be 7
        $Result.AuthorName | Should -BeExactly 'Test Author'
        $Result.AuthorEmail | Should -BeExactly 'test@example.com'
        $Result.MessageShort | Should -BeExactly 'Add TODO item'
    }
}

Describe 'Select-GitCommit -Contains' {
    BeforeAll {
        # File content matches commit message, so 'TODO' appears in the diff of the second commit
        $script:RepoPath = New-TestGitRepository -CommitMessages @(
            'Initial commit',
            'Add TODO item',
            'Fix bug in module',
            'Add another TODO entry'
        )
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Finds commits whose diff contains the search pattern' {
        $Results = @(Select-GitCommit -Contains 'TODO' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 2
    }

    It 'Returns commits in newest-first order' {
        $Results = @(Select-GitCommit -Contains 'TODO' -RepoPath $script:RepoPath)
        $Results[0].MessageShort | Should -BeExactly 'Add another TODO entry'
        $Results[1].MessageShort | Should -BeExactly 'Add TODO item'
    }

    It 'Returns empty when no diff contains the search pattern' {
        $Results = @(Select-GitCommit -Contains 'NONEXISTENT_MARKER_XYZ' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 0
    }

    It 'Search is case-sensitive by default' {
        $Results = @(Select-GitCommit -Contains 'todo' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 0
    }
}

Describe 'Select-GitCommit -Match' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @(
            'Initial commit',
            'Add TODO item',
            'Add FIXME in handler',
            'Routine maintenance'
        )
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Finds commits matching a regex pattern' {
        $Results = @(Select-GitCommit -Match 'TODO|FIXME' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 2
    }

    It 'Handles anchored regex patterns' {
        $Results = @(Select-GitCommit -Match '^\+Add TODO' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 1
        $Results[0].MessageShort | Should -BeExactly 'Add TODO item'
    }
}

Describe 'Select-GitCommit -First' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @(
            'Add TODO first',
            'Add TODO second',
            'Add TODO third'
        )
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Limits the number of returned commits' {
        $Results = @(Select-GitCommit -Contains 'TODO' -First 2 -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 2
    }

    It 'Returns the most recent matching commits when limited' {
        $Results = @(Select-GitCommit -Contains 'TODO' -First 1 -RepoPath $script:RepoPath)
        $Results[0].MessageShort | Should -BeExactly 'Add TODO third'
    }
}

Describe 'Select-GitCommit -Where' {
    BeforeAll {
        # Create repo with two different author names
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -Path $script:RepoPath -ItemType Directory -Force | Out-Null
        Push-Location -Path $script:RepoPath
        try {
            git init --initial-branch main 2>&1 | Out-Null

            git config user.name 'Alice'
            git config user.email 'alice@example.com'
            New-Item -Path 'alice.txt' -Value 'Alice work' | Out-Null
            git add . 2>&1 | Out-Null
            git commit -m 'Alice commit' 2>&1 | Out-Null

            git config user.name 'Bob'
            git config user.email 'bob@example.com'
            New-Item -Path 'bob.txt' -Value 'Bob work' | Out-Null
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

    It 'Filters commits using a ScriptBlock predicate on the raw Commit object' {
        $Results = @(Select-GitCommit -RepoPath $script:RepoPath -Where {
            $args[0].Author.Name -eq 'Alice'
        })
        $Results | Should -HaveCount 1
        $Results[0].AuthorName | Should -BeExactly 'Alice'
    }

    It 'Returns empty when no commit satisfies the predicate' {
        $Results = @(Select-GitCommit -RepoPath $script:RepoPath -Where {
            $args[0].Author.Name -eq 'Nobody'
        })
        $Results | Should -HaveCount 0
    }

    It 'Can access full object graph (Parents.Count)' {
        # Only the second commit has a parent
        $Results = @(Select-GitCommit -RepoPath $script:RepoPath -Where {
            $args[0].Parents.Count -gt 0
        })
        $Results | Should -HaveCount 1
        $Results[0].MessageShort | Should -BeExactly 'Bob commit'
    }
}

Describe 'Select-GitCommit -Contains combined with -Where' {
    BeforeAll {
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -Path $script:RepoPath -ItemType Directory -Force | Out-Null
        Push-Location -Path $script:RepoPath
        try {
            git init --initial-branch main 2>&1 | Out-Null

            git config user.name 'Alice'
            git config user.email 'alice@example.com'
            Set-Content -Path 'a.txt' -Value 'Add TODO item'
            git add . 2>&1 | Out-Null
            git commit -m 'Alice TODO' 2>&1 | Out-Null

            git config user.name 'Bob'
            git config user.email 'bob@example.com'
            Set-Content -Path 'b.txt' -Value 'Add TODO item'
            git add . 2>&1 | Out-Null
            git commit -m 'Bob TODO' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Applies both -Contains and -Where predicate' {
        $Results = @(Select-GitCommit -Contains 'TODO' -RepoPath $script:RepoPath -Where {
            $args[0].Author.Name -eq 'Alice'
        })
        $Results | Should -HaveCount 1
        $Results[0].AuthorName | Should -BeExactly 'Alice'
    }
}

Describe 'Select-GitCommit -Path' {
    BeforeAll {
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -Path $script:RepoPath -ItemType Directory -Force | Out-Null
        Push-Location -Path $script:RepoPath
        try {
            git init --initial-branch main 2>&1 | Out-Null
            git config user.name 'Test Author'
            git config user.email 'test@example.com'

            # Commit 1: touches src.txt
            Set-Content -Path 'src.txt' -Value 'source content with TODO'
            git add . 2>&1 | Out-Null
            git commit -m 'Add src' 2>&1 | Out-Null

            # Commit 2: touches docs.txt only
            Set-Content -Path 'docs.txt' -Value 'docs content with TODO'
            git add . 2>&1 | Out-Null
            git commit -m 'Add docs' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Restricts candidates to commits touching the specified path' {
        $Results = @(Select-GitCommit -Contains 'TODO' -Path 'src.txt' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 1
        $Results[0].MessageShort | Should -BeExactly 'Add src'
    }
}

Describe 'Select-GitCommit -From' {
    BeforeAll {
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -Path $script:RepoPath -ItemType Directory -Force | Out-Null
        Push-Location -Path $script:RepoPath
        try {
            git init --initial-branch main 2>&1 | Out-Null
            git config user.name 'Test Author'
            git config user.email 'test@example.com'

            Set-Content -Path 'a.txt' -Value 'commit one with TODO'
            git add . 2>&1 | Out-Null
            git commit -m 'Commit one' 2>&1 | Out-Null

            Set-Content -Path 'b.txt' -Value 'commit two with TODO'
            git add . 2>&1 | Out-Null
            git commit -m 'Commit two' 2>&1 | Out-Null

            # Create a feature branch pointing at this commit
            git checkout -b feature 2>&1 | Out-Null
            Set-Content -Path 'c.txt' -Value 'commit three with TODO'
            git add . 2>&1 | Out-Null
            git commit -m 'Commit three (feature only)' 2>&1 | Out-Null

            # Return to main
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Walks commits reachable from the specified branch' {
        $Results = @(Select-GitCommit -Contains 'TODO' -From 'feature' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 3
    }

    It 'Excludes commits not reachable from the specified branch' {
        $Results = @(Select-GitCommit -Contains 'TODO' -From 'main' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 2
    }
}

Describe 'Select-GitCommit -From with tag' {
    BeforeAll {
        $script:RepoPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitTest_$([System.Guid]::NewGuid().ToString('N'))"
        New-Item -Path $script:RepoPath -ItemType Directory -Force | Out-Null
        Push-Location -Path $script:RepoPath
        try {
            git init --initial-branch main 2>&1 | Out-Null
            git config user.name 'Test Author'
            git config user.email 'test@example.com'

            Set-Content -Path 'a.txt' -Value 'commit one with TODO'
            git add . 2>&1 | Out-Null
            git commit -m 'Commit one' 2>&1 | Out-Null

            # Lightweight tag after first commit
            git tag v1.0 2>&1 | Out-Null

            Set-Content -Path 'b.txt' -Value 'commit two with TODO'
            git add . 2>&1 | Out-Null
            git commit -m 'Commit two' 2>&1 | Out-Null

            # Annotated tag after second commit
            git tag -a v2.0 -m 'Release v2.0' 2>&1 | Out-Null

            Set-Content -Path 'c.txt' -Value 'commit three with TODO'
            git add . 2>&1 | Out-Null
            git commit -m 'Commit three' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Walks commits reachable from a lightweight tag' {
        $Results = @(Select-GitCommit -Contains 'TODO' -From 'v1.0' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 1
        $Results[0].MessageShort | Should -BeExactly 'Commit one'
    }

    It 'Walks commits reachable from an annotated tag' {
        $Results = @(Select-GitCommit -Contains 'TODO' -From 'v2.0' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 2
    }
}

Describe 'Select-GitCommit error handling' {
    BeforeAll {
        . "$PSScriptRoot/SystemTest-Helpers.ps1"
    }

    It 'Writes a non-terminating error for an invalid repository path' {
        $Errors = @()
        Select-GitCommit -Contains 'test' -RepoPath $NonExistentRepoPath -ErrorVariable Errors -ErrorAction SilentlyContinue
        $Errors | Should -HaveCount 1
    }
}
