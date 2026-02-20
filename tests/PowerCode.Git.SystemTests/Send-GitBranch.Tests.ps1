#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Send-GitBranch cmdlet.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    by pushing to a local bare repository created in a temporary directory.
#>

BeforeAll {
    if ($env:POWERCODE_GIT_MODULE_PATH -and (Test-Path -Path $env:POWERCODE_GIT_MODULE_PATH)) {
        $ModulePath = $env:POWERCODE_GIT_MODULE_PATH
    }
    else {
        $RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path
        $ModuleLayoutDir = Join-Path -Path $RepoRoot -ChildPath 'artifacts/module/PowerCode.Git'
        $VersionedDir = Get-ChildItem -Path $ModuleLayoutDir -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $VersionedDir) {
            throw "No versioned module folder found under '$ModuleLayoutDir'. Build the solution before running system tests."
        }
        $ModulePath = Join-Path -Path $VersionedDir.FullName -ChildPath 'PowerCode.Git.psd1'
    }

    if (-not (Test-Path -Path $ModulePath)) {
        throw "Module not found at '$ModulePath'. Build the solution before running system tests."
    }

    Import-Module -Name $ModulePath -Force -ErrorAction Stop

    function New-TestRepoWithRemote {
        <#
        .SYNOPSIS
            Creates a working repository with a local bare remote, suitable
            for push/pull testing.
        .OUTPUTS
            A hashtable with keys WorkingPath and BarePath.
        #>
        [CmdletBinding()]
        param()

        $BareDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitBare_$([System.Guid]::NewGuid().ToString('N'))"
        $WorkDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "PowerCode.GitWork_$([System.Guid]::NewGuid().ToString('N'))"

        # Create bare repo
        git init --bare --initial-branch main $BareDir 2>&1 | Out-Null

        # Clone it to get a working copy with origin set
        git clone $BareDir $WorkDir 2>&1 | Out-Null

        Push-Location -Path $WorkDir
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

        return @{
            WorkingPath = $WorkDir
            BarePath    = $BareDir
        }
    }

    function Remove-TestGitRepository {
        [CmdletBinding()]
        param(
            [Parameter(Mandatory)]
            [string]$Path
        )

        if (Test-Path -Path $Path) {
            Get-ChildItem -Path $Path -Recurse -Force | ForEach-Object {
                if ($_.Attributes -band [System.IO.FileAttributes]::ReadOnly) {
                    $_.Attributes = $_.Attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly)
                }
            }
            Remove-Item -Path $Path -Recurse -Force
        }
    }
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
