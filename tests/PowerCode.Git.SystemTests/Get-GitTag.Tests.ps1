#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Get-GitTag cmdlet.
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
        $Result = Get-GitTag -RepoPath $NonExistentRepoPath -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-GitTag -Include' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git tag v1.0.0 2>&1 | Out-Null
            git tag v1.1.0 2>&1 | Out-Null
            git tag -a release-2.0 -m 'Major release' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns only matching tags when -Include is specified' {
        $Tags = @(Get-GitTag -RepoPath $script:RepoPath -Include 'v*')
        $Tags | Should -HaveCount 2
        $Tags | ForEach-Object { $_.Name | Should -Match '^v' }
    }

    It 'Accepts -Include as positional argument' {
        $Tags = @(Get-GitTag -RepoPath $script:RepoPath 'v1.*')
        $Tags | Should -HaveCount 2
        $Tags | ForEach-Object { $_.Name | Should -Match '^v1' }
    }
}

Describe 'Get-GitTag -Exclude' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git tag v1.0.0 2>&1 | Out-Null
            git tag v1.1.0 2>&1 | Out-Null
            git tag -a release-2.0 -m 'Major release' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Excludes tags matching the pattern' {
        $Tags = @(Get-GitTag -RepoPath $script:RepoPath -Exclude 'v*')
        $Tags | Should -HaveCount 1
        $Tags[0].Name | Should -BeExactly 'release-2.0'
    }

    It 'Combines -Include and -Exclude' {
        $Tags = @(Get-GitTag -RepoPath $script:RepoPath -Include 'v*' -Exclude 'v1.1*')
        $Tags | Should -HaveCount 1
        $Tags[0].Name | Should -BeExactly 'v1.0.0'
    }
}

Describe 'Get-GitTag -Options' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location -Path $script:RepoPath
        try {
            git tag v1.0.0 2>&1 | Out-Null
            git tag v2.0.0 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Returns tags matching the pattern in the Options object' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitTagListOptions]@{
            RepositoryPath = $script:RepoPath
            Pattern        = 'v1*'
        }

        $Tags = @(Get-GitTag -Options $Opts)
        $Tags | Should -HaveCount 1
        $Tags[0].Name | Should -BeExactly 'v1.0.0'
    }
}
