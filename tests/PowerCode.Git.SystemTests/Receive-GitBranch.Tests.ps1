#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Receive-GitBranch cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    by pulling from a local bare repository created in a temporary directory.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"

    function New-TestRepoWithRemoteAndPushedCommit {
        <#
        .SYNOPSIS
            Creates two working repositories sharing a local bare remote.
            The "pusher" repo pushes a commit that the "puller" repo can pull.
        .OUTPUTS
            A hashtable with keys PullerPath, PusherPath, and BarePath.
        #>
        [CmdletBinding()]
        param()

        $BareDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitBare_$([System.Guid]::NewGuid().ToString('N'))"
        $PusherDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitPusher_$([System.Guid]::NewGuid().ToString('N'))"
        $PullerDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitPuller_$([System.Guid]::NewGuid().ToString('N'))"

        # Create bare repo
        git init --bare --initial-branch main $BareDir 2>&1 | Out-Null

        # Clone to pusher and create initial commit
        git clone $BareDir $PusherDir 2>&1 | Out-Null
        Push-Location -Path $PusherDir
        try {
            git config user.name 'Test Author'
            git config user.email 'test@example.com'
            Set-Content -Path 'README.md' -Value '# Test Repo'
            git add . 2>&1 | Out-Null
            git commit -m 'Initial commit' 2>&1 | Out-Null
            git push origin main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        # Clone to puller
        git clone $BareDir $PullerDir 2>&1 | Out-Null
        Push-Location -Path $PullerDir
        try {
            git config user.name 'Test Author'
            git config user.email 'test@example.com'
        }
        finally {
            Pop-Location
        }

        # Push a second commit from pusher so that puller is behind
        Push-Location -Path $PusherDir
        try {
            Set-Content -Path 'new-file.txt' -Value 'new content'
            git add . 2>&1 | Out-Null
            git commit -m 'Second commit from pusher' 2>&1 | Out-Null
            git push origin main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        return @{
            PullerPath = $PullerDir
            PusherPath = $PusherDir
            BarePath   = $BareDir
        }
    }
}

AfterAll {
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'Receive-GitBranch basic pull' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemoteAndPushedCommit
        $script:PullerPath = $script:Repos.PullerPath
        $script:PusherPath = $script:Repos.PusherPath
        $script:BarePath = $script:Repos.BarePath
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:PullerPath
        Remove-TestGitRepository -Path $script:PusherPath
        Remove-TestGitRepository -Path $script:BarePath
    }

    It 'Pulls remote changes and returns GitCommitInfo' {
        $Result = Receive-GitBranch -RepoPath $script:PullerPath

        $Result | Should -Not -BeNullOrEmpty
        $Result.Sha | Should -Match '^[0-9a-f]{40}$'
    }

    It 'The puller has the new commit after pulling' {
        $Commits = @(Get-GitLog -RepoPath $script:PullerPath)
        $Messages = $Commits | ForEach-Object { $_.MessageShort }
        $Messages | Should -Contain 'Second commit from pusher'
    }

    It 'The pulled file exists in the working tree' {
        $FilePath = Join-Path -Path $script:PullerPath -ChildPath 'new-file.txt'
        Test-Path -Path $FilePath | Should -BeTrue
    }
}

Describe 'Receive-GitBranch -Prune' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemoteAndPushedCommit

        # Create and push a branch from pusher, then delete it
        Push-Location -Path $script:Repos.PusherPath
        try {
            git checkout -b temp-branch 2>&1 | Out-Null
            git push origin temp-branch 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        # Fetch temp-branch in puller so it knows about it
        Push-Location -Path $script:Repos.PullerPath
        try {
            git fetch origin 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        # Delete temp-branch from the bare remote
        Push-Location -Path $script:Repos.PusherPath
        try {
            git push origin --delete temp-branch 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.PullerPath
        Remove-TestGitRepository -Path $script:Repos.PusherPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Prunes stale remote-tracking branches' {
        Receive-GitBranch -RepoPath $script:Repos.PullerPath -Prune | Out-Null

        Push-Location -Path $script:Repos.PullerPath
        try {
            $RemoteBranches = git branch -r 2>&1
            $RemoteBranches | Should -Not -Match 'temp-branch'
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Receive-GitBranch error handling' {
    It 'Produces a non-terminating error for an invalid path' {
        $Result = Receive-GitBranch -RepoPath $NonExistentRepoPath -ErrorVariable GitErrors -ErrorAction SilentlyContinue
        $Result | Should -BeNullOrEmpty
        $GitErrors | Should -Not -BeNullOrEmpty
    }
}

Describe 'Receive-GitBranch -Options' {
    BeforeAll {
        $script:Repos = New-TestRepoWithRemoteAndPushedCommit
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:Repos.PullerPath
        Remove-TestGitRepository -Path $script:Repos.PusherPath
        Remove-TestGitRepository -Path $script:Repos.BarePath
    }

    It 'Pulls using a GitPullOptions object' {
        $Opts = [PowerCode.Git.Abstractions.Models.GitPullOptions]@{
            RepositoryPath = $script:Repos.PullerPath
            RemoteName     = 'origin'
        }

        $Result = Receive-GitBranch -Options $Opts

        $Result | Should -Not -BeNullOrEmpty
    }
}

Describe 'Receive-GitBranch pipeline -Action Create' {
    BeforeAll {
        $BareDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitBare_$([System.Guid]::NewGuid().ToString('N'))"
        $WorkDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitWork_$([System.Guid]::NewGuid().ToString('N'))"

        git init --bare --initial-branch main $BareDir 2>&1 | Out-Null

        git clone $BareDir $WorkDir 2>&1 | Out-Null
        Push-Location -Path $WorkDir
        try {
            git config user.name 'Test Author'
            git config user.email 'test@example.com'
            Set-Content -Path 'README.md' -Value '# Test'
            git add . 2>&1 | Out-Null
            git commit -m 'Initial commit' 2>&1 | Out-Null
            git checkout -b feature/alpha 2>&1 | Out-Null
            git push origin main feature/alpha 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
            # Delete the local feature/alpha so it only exists as a remote-tracking branch
            git branch -D feature/alpha 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        $script:BareDir = $BareDir
        $script:WorkDir = $WorkDir
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:WorkDir
        Remove-TestGitRepository -Path $script:BareDir
    }

    It 'Creates a local tracking branch from a remote-tracking branch' {
        $RemoteBranches = Get-GitBranch -Remote -RepoPath $script:WorkDir
        $FeatureBranch  = $RemoteBranches | Where-Object { $_.LocalName -eq 'feature/alpha' }
        $FeatureBranch  | Should -Not -BeNullOrEmpty

        $Result = $FeatureBranch | Receive-GitBranch -Action Create -RepoPath $script:WorkDir

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -Be 'feature/alpha'
        $Result.IsRemote | Should -BeFalse
    }

    It 'Skips existing local branch when Action is Create' {
        # feature/alpha was created by the previous test; running again should skip silently
        $FeatureBranch = Get-GitBranch -Remote -RepoPath $script:WorkDir |
            Where-Object { $_.LocalName -eq 'feature/alpha' }

        $Result = $FeatureBranch | Receive-GitBranch -Action Create -RepoPath $script:WorkDir

        # No output because branch already existed
        $Result | Should -BeNullOrEmpty
    }

    It 'Writes an error when a local branch is piped' {
        $LocalBranch = Get-GitBranch -RepoPath $script:WorkDir | Select-Object -First 1

        { $LocalBranch | Receive-GitBranch -Action Create -RepoPath $script:WorkDir -ErrorAction Stop } |
            Should -Throw
    }
}

Describe 'Receive-GitBranch pipeline -Action UpdateOnly' {
    BeforeAll {
        $BareDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitBare_$([System.Guid]::NewGuid().ToString('N'))"
        $WorkDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitWork_$([System.Guid]::NewGuid().ToString('N'))"

        git init --bare --initial-branch main $BareDir 2>&1 | Out-Null

        git clone $BareDir $WorkDir 2>&1 | Out-Null
        Push-Location -Path $WorkDir
        try {
            git config user.name 'Test Author'
            git config user.email 'test@example.com'
            Set-Content -Path 'README.md' -Value '# Test'
            git add . 2>&1 | Out-Null
            git commit -m 'Initial commit' 2>&1 | Out-Null
            git push origin main 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        $script:BareDir = $BareDir
        $script:WorkDir = $WorkDir
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:WorkDir
        Remove-TestGitRepository -Path $script:BareDir
    }

    It 'Skips checked-out branches and produces no output' {
        # The only remote branch is origin/main, whose local counterpart 'main' is the
        # current HEAD. FastForward is skipped for checked-out branches → no output, no error.
        $RemoteBranches = Get-GitBranch -Remote -RepoPath $script:WorkDir
        $Result = $RemoteBranches | Receive-GitBranch -Action UpdateOnly -RepoPath $script:WorkDir

        $Result | Should -BeNullOrEmpty
    }
}

Describe 'Receive-GitBranch pipeline -Action CreateOrUpdate' {
    BeforeAll {
        $BareDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitBare_$([System.Guid]::NewGuid().ToString('N'))"
        $WorkDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitWork_$([System.Guid]::NewGuid().ToString('N'))"

        git init --bare --initial-branch main $BareDir 2>&1 | Out-Null

        git clone $BareDir $WorkDir 2>&1 | Out-Null
        Push-Location -Path $WorkDir
        try {
            git config user.name 'Test Author'
            git config user.email 'test@example.com'
            Set-Content -Path 'README.md' -Value '# Test'
            git add . 2>&1 | Out-Null
            git commit -m 'Initial commit' 2>&1 | Out-Null
            git checkout -b feature/beta 2>&1 | Out-Null
            git push origin main feature/beta 2>&1 | Out-Null
            git checkout main 2>&1 | Out-Null
            git branch -D feature/beta 2>&1 | Out-Null
        }
        finally {
            Pop-Location
        }

        $script:BareDir = $BareDir
        $script:WorkDir = $WorkDir
    }

    AfterAll {
        Remove-TestGitRepository -Path $script:WorkDir
        Remove-TestGitRepository -Path $script:BareDir
    }

    It 'Creates missing local branch and returns GitBranchInfo' {
        $FeatureBranch = Get-GitBranch -Remote -RepoPath $script:WorkDir |
            Where-Object { $_.LocalName -eq 'feature/beta' }

        $Result = $FeatureBranch | Receive-GitBranch -Action CreateOrUpdate -RepoPath $script:WorkDir

        $Result | Should -Not -BeNullOrEmpty
        $Result.Name | Should -Be 'feature/beta'
        $Result.IsRemote | Should -BeFalse
    }
}

