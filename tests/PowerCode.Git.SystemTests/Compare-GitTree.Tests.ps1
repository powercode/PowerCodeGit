#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Compare-GitTree cmdlet.
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

Describe 'Compare-GitTree basic usage' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit', 'Second commit')

        Push-Location -Path $script:RepoPath
        try {
            $script:Commits = git log --format='%H' | ForEach-Object { $_ }
            # Commits are newest-first: [0] = Second, [1] = Initial
            $script:BaseCommit = $script:Commits[1]
            $script:CompareCommit = $script:Commits[0]
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns GitDiffEntry objects' {
        $Results = @(Compare-GitTree -Base $script:BaseCommit -Compare $script:CompareCommit -RepoPath $script:RepoPath)
        $Results | Should -Not -BeNullOrEmpty
        $Results[0] | Should -BeOfType 'PowerCode.Git.Abstractions.Models.GitDiffEntry'
    }

    It 'Shows files added between commits' {
        $Results = @(Compare-GitTree -Base $script:BaseCommit -Compare $script:CompareCommit -RepoPath $script:RepoPath)
        $Results.Status | Should -Contain 'Added'
    }

    It 'GitDiffEntry has Patch content' {
        $Entry = Compare-GitTree -Base $script:BaseCommit -Compare $script:CompareCommit -RepoPath $script:RepoPath | Select-Object -First 1
        $Entry.Patch | Should -Not -BeNullOrEmpty
    }

    It 'GitDiffEntry has LinesAdded populated' {
        $Entry = Compare-GitTree -Base $script:BaseCommit -Compare $script:CompareCommit -RepoPath $script:RepoPath | Select-Object -First 1
        $Entry.LinesAdded | Should -BeGreaterThan 0
    }
}

Describe 'Compare-GitTree with branches' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git checkout -b feature 2>&1 | Out-Null
            Set-Content -Path 'feature-file.txt' -Value 'feature content'
            git add . 2>&1 | Out-Null
            git commit -m 'Add feature file' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Compares two branches by name' {
        $Results = @(Compare-GitTree -Base main -Compare feature -RepoPath $script:RepoPath)
        $Results | Should -Not -BeNullOrEmpty
        $Results.NewPath | Should -Contain 'feature-file.txt'
    }
}

Describe 'Compare-GitTree -Path filter' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            $script:BaseCommit = git rev-parse HEAD

            Set-Content -Path 'include-me.txt' -Value 'included'
            Set-Content -Path 'exclude-me.txt' -Value 'excluded'
            git add . 2>&1 | Out-Null
            git commit -m 'Add two files' 2>&1 | Out-Null

            $script:CompareCommit = git rev-parse HEAD
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Restricts output to specified paths' {
        $Results = @(Compare-GitTree -Base $script:BaseCommit -Compare $script:CompareCommit -Path 'include-me.txt' -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 1
        $Results[0].NewPath | Should -BeExactly 'include-me.txt'
    }
}

Describe 'Compare-GitTree -Where filter' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            $script:BaseCommit = git rev-parse HEAD

            Set-Content -Path 'alpha.txt' -Value 'alpha content'
            Set-Content -Path 'beta.txt' -Value 'beta content'
            git add . 2>&1 | Out-Null
            git commit -m 'Add alpha and beta' 2>&1 | Out-Null

            $script:CompareCommit = git rev-parse HEAD
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Filters entries via ScriptBlock predicate using $change variable' {
        $Results = @(Compare-GitTree -Base $script:BaseCommit -Compare $script:CompareCommit -RepoPath $script:RepoPath -Where { $change.NewPath -eq 'alpha.txt' })
        $Results | Should -HaveCount 1
        $Results[0].NewPath | Should -BeExactly 'alpha.txt'
    }

    It 'Filters entries via ScriptBlock predicate using $args[0]' {
        $Results = @(Compare-GitTree -Base $script:BaseCommit -Compare $script:CompareCommit -RepoPath $script:RepoPath -Where { $args[0].NewPath -eq 'beta.txt' })
        $Results | Should -HaveCount 1
        $Results[0].NewPath | Should -BeExactly 'beta.txt'
    }
}

Describe 'Compare-GitTree -Transform' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            $script:BaseCommit = git rev-parse HEAD

            Set-Content -Path 'transform-test.txt' -Value 'some content'
            git add . 2>&1 | Out-Null
            git commit -m 'Add transform-test file' 2>&1 | Out-Null

            $script:CompareCommit = git rev-parse HEAD
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Transforms output with ScriptBlock using $change variable' {
        $Results = @(Compare-GitTree -Base $script:BaseCommit -Compare $script:CompareCommit -RepoPath $script:RepoPath -Transform { $change.NewPath })
        $Results | Should -Contain 'transform-test.txt'
        $Results[0] | Should -BeOfType 'System.String'
    }
}

Describe 'Compare-GitTree identical commits' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            $script:CommitSha = git rev-parse HEAD
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns empty for identical base and compare' {
        $Results = @(Compare-GitTree -Base $script:CommitSha -Compare $script:CommitSha -RepoPath $script:RepoPath)
        $Results | Should -HaveCount 0
    }
}

Describe 'Compare-GitTree error handling' {
    It 'Produces a non-terminating error for an invalid repository path' {
        $Result = Compare-GitTree -Base HEAD -Compare HEAD -RepoPath $NonExistentRepoPath -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}
