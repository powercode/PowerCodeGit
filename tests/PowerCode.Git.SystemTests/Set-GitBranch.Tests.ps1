#Requires -Modules Pester

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'Set-GitBranch -Description' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
        New-GitBranch -RepoPath $script:RepoPath -Name 'feature/login'
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Sets a branch description' {
        $Branch = Set-GitBranch -RepoPath $script:RepoPath -Name 'feature/login' -Description 'Login feature work'
        $Branch | Should -Not -BeNullOrEmpty
        $Branch.Name | Should -BeExactly 'feature/login'
        $Branch.Description | Should -BeExactly 'Login feature work'
    }

    It 'Description is readable via git config' {
        Push-Location $script:RepoPath
        try {
            $Value = git config --get branch.feature/login.description
            $Value | Should -BeExactly 'Login feature work'
        }
        finally {
            Pop-Location
        }
    }

    It 'Overwrites an existing description' {
        $Branch = Set-GitBranch -RepoPath $script:RepoPath -Name 'feature/login' -Description 'Updated description'
        $Branch.Description | Should -BeExactly 'Updated description'
    }
}

Describe 'Set-GitBranch -Remote' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath

        Push-Location $script:RepoPath
        try {
            git checkout -b feature/remote-test 2>&1 | Out-Null
            git push origin feature/remote-test 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Sets the upstream remote for a branch' {
        $Branch = Set-GitBranch -RepoPath $script:RepoPath -Name 'feature/remote-test' -Remote 'origin'
        $Branch | Should -Not -BeNullOrEmpty
        $Branch.Name | Should -BeExactly 'feature/remote-test'
    }

    It 'Remote is readable via git config' {
        Push-Location $script:RepoPath
        try {
            $Value = git config --get branch.feature/remote-test.remote
            $Value | Should -BeExactly 'origin'
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Set-GitBranch -Remote and -Description together' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath

        Push-Location $script:RepoPath
        try {
            git checkout -b feature/combined 2>&1 | Out-Null
            git push origin feature/combined 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Sets both remote and description in a single call' {
        $Branch = Set-GitBranch -RepoPath $script:RepoPath -Name 'feature/combined' -Remote 'origin' -Description 'Combined test'
        $Branch | Should -Not -BeNullOrEmpty
        $Branch.Name | Should -BeExactly 'feature/combined'
        $Branch.Description | Should -BeExactly 'Combined test'
    }

    It 'Both values are readable via git config' {
        Push-Location $script:RepoPath
        try {
            $Remote = git config --get branch.feature/combined.remote
            $Remote | Should -BeExactly 'origin'

            $Desc = git config --get branch.feature/combined.description
            $Desc | Should -BeExactly 'Combined test'
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Set-GitBranch -Upstream' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:RepoPath = $script:Repos.WorkingPath

        Push-Location $script:RepoPath
        try {
            git checkout -b feature/upstream-test 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.WorkingPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Sets the upstream merge ref for a branch' {
        $Branch = Set-GitBranch -RepoPath $script:RepoPath -Name 'feature/upstream-test' -Remote 'origin' -Upstream 'main'
        $Branch | Should -Not -BeNullOrEmpty
        $Branch.Name | Should -BeExactly 'feature/upstream-test'
    }

    It 'Merge ref is readable via git config' {
        Push-Location $script:RepoPath
        try {
            $Value = git config --get branch.feature/upstream-test.merge
            $Value | Should -BeExactly 'refs/heads/main'
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Set-GitBranch errors' {
    BeforeAll {
        $script:RepoPath = New-TestGitRepository -CommitMessages @('Initial commit')
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:RepoPath
    }

    It 'Fails when branch does not exist' {
        { Set-GitBranch -RepoPath $script:RepoPath -Name 'nonexistent' -Description 'test' -ErrorAction Stop } | Should -Throw
    }
}
