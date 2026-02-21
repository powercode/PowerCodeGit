#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Set-GitTag cmdlet.
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

Describe 'Set-GitTag lightweight tag' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Creates a lightweight tag at HEAD' {
        $Tag = Set-GitTag -RepoPath $script:RepoPath -Name 'v1.0.0'
        $Tag | Should -Not -BeNullOrEmpty
        $Tag.Name | Should -BeExactly 'v1.0.0'
    }

    It 'Tag is not annotated' {
        $Tag = Get-GitTag -RepoPath $script:RepoPath | Where-Object { $_.Name -eq 'v1.0.0' }
        $Tag.IsAnnotated | Should -BeFalse
    }

    It 'Tag has a valid SHA' {
        $Tag = Get-GitTag -RepoPath $script:RepoPath | Where-Object { $_.Name -eq 'v1.0.0' }
        $Tag.Sha | Should -Match '^[0-9a-f]{40}$'
    }

    It 'Tag appears in Get-GitTag output' {
        $Tags = @(Get-GitTag -RepoPath $script:RepoPath)
        $Tags | Where-Object { $_.Name -eq 'v1.0.0' } | Should -Not -BeNullOrEmpty
    }
}

Describe 'Set-GitTag annotated tag' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Creates an annotated tag when -Message is provided' {
        $Tag = Set-GitTag -RepoPath $script:RepoPath -Name 'v2.0.0' -Message 'Release v2.0.0'
        $Tag | Should -Not -BeNullOrEmpty
        $Tag.Name | Should -BeExactly 'v2.0.0'
    }

    It 'Annotated tag has IsAnnotated true' {
        $Tag = Get-GitTag -RepoPath $script:RepoPath | Where-Object { $_.Name -eq 'v2.0.0' }
        $Tag.IsAnnotated | Should -BeTrue
    }

    It 'Annotated tag carries the message' {
        $Tag = Get-GitTag -RepoPath $script:RepoPath | Where-Object { $_.Name -eq 'v2.0.0' }
        $Tag.Message | Should -Match 'Release v2.0.0'
    }

    It 'Annotated tag has tagger metadata' {
        $Tag = Get-GitTag -RepoPath $script:RepoPath | Where-Object { $_.Name -eq 'v2.0.0' }
        $Tag.TaggerName | Should -Not -BeNullOrEmpty
        $Tag.TaggerEmail | Should -Not -BeNullOrEmpty
        $Tag.TagDate | Should -Not -BeNullOrEmpty
    }
}

Describe 'Set-GitTag -Target' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('First commit', 'Second commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Tags a specific commit via -Target' {
        Push-Location $script:RepoPath
        try {
            $FirstSha = git log --oneline | Select-Object -Last 1 | ForEach-Object { $_.Split(' ')[0] }
            $Tag = Set-GitTag -RepoPath $script:RepoPath -Name 'v0.1.0' -Target $FirstSha
            $Tag | Should -Not -BeNullOrEmpty
            $Tag.Name | Should -BeExactly 'v0.1.0'
            $Tag.ShortSha | Should -BeExactly $FirstSha
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Set-GitTag -Force overwrites existing tag' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')

        Push-Location $script:RepoPath
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

    It 'Fails without -Force when tag already exists' {
        { Set-GitTag -RepoPath $script:RepoPath -Name 'v1.0.0' -ErrorAction Stop } | Should -Throw
    }

    It 'Succeeds with -Force when tag already exists' {
        $Tag = Set-GitTag -RepoPath $script:RepoPath -Name 'v1.0.0' -Force
        $Tag | Should -Not -BeNullOrEmpty
        $Tag.Name | Should -BeExactly 'v1.0.0'
    }
}
