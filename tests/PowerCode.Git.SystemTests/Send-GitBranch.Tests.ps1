#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Send-GitBranch cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    by pushing to a local bare repository created in a temporary directory.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'Send-GitBranch basic push' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:WorkPath = $script:Repos.WorkingPath
        $script:BarePath = $script:Repos.BarePath

        # Create a new commit to push
        Push-Location -Path $script:WorkPath
        try {
            Set-Content -Path 'pushed.txt' -Value 'pushed content'
            git add . 2>&1 | Out-Null
            git commit -m 'Push test commit' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:WorkPath
        Remove-TestGitRepository -Path $script:BarePath
    }

    It 'Pushes the current branch and returns GitBranchInfo' {
        $Result = Send-GitBranch -RepoPath $script:WorkPath

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'main'
    }

    It 'The pushed commit is visible when cloning the bare repo' {
        $VerifyDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitVerify_$([System.Guid]::NewGuid().ToString('N'))"
        try {
            git clone $script:BarePath $VerifyDir 2>&1 | Out-Null
            $Commits = @(Get-GitLog -RepoPath $VerifyDir)
            $Messages = $Commits | ForEach-Object { $_.MessageShort }
            $Messages | Should -Contain 'Push test commit'
        }
        finally {
            Remove-TestGitRepository -Path $VerifyDir
        }
    }
}

Describe 'Send-GitBranch -SetUpstream' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:WorkPath = $script:Repos.WorkingPath
        $script:BarePath = $script:Repos.BarePath

        # Create a new branch with a commit
        Push-Location -Path $script:WorkPath
        try {
            git checkout -b feature/upstream-test 2>&1 | Out-Null
            Set-Content -Path 'feature.txt' -Value 'feature content'
            git add . 2>&1 | Out-Null
            git commit -m 'Feature commit' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:WorkPath
        Remove-TestGitRepository -Path $script:BarePath
    }

    It 'Pushes and sets the upstream tracking reference' {
        $Result = Send-GitBranch -RepoPath $script:WorkPath -SetUpstream

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -BeExactly 'feature/upstream-test'
        $Result.TrackedBranchName | Should -Not -BeNullOrEmpty
    }
}

Describe 'Send-GitBranch error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Send-GitBranch -RepoPath 'C:\nonexistent\repo\path' -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Send-GitBranch -Options' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemote
        $script:WorkPath = $script:Repos.WorkingPath
        $script:BarePath = $script:Repos.BarePath

        Push-Location -Path $script:WorkPath
        try {
            Set-Content -Path 'opts.txt' -Value 'via options'
            git add . 2>&1 | Out-Null
            git commit -m 'Options push commit' 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:WorkPath
        Remove-TestGitRepository -Path $script:BarePath
    }

    It 'Pushes the branch via a GitPushOptions object' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitPushOptions]@{
            RepositoryPath = $script:WorkPath
            RemoteName     = 'origin'
        }

        $Result = Send-GitBranch -Options $Opts

        $Result | Should -Not -BeNullOrEmpty
    }
}
