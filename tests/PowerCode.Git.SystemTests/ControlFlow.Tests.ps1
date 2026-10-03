#Requires -Modules Pester

BeforeAll {
    . "$PSScriptRoot\SystemTest-Helpers.ps1"
    $script:RepoPath = New-TestGitRepository -CommitMessages @('First', 'Second', 'Third')
    Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'first.txt') -Value 'first'
    Set-Content -Path (Join-Path -Path $script:RepoPath -ChildPath 'second.txt') -Value 'second'
    git -C $script:RepoPath add .
    git -C $script:RepoPath commit -m 'Multiple files' | Out-Null
    git -C $script:RepoPath branch feature
    git -C $script:RepoPath config controlflow.first one
    git -C $script:RepoPath config controlflow.second two
}

AfterAll {
    Remove-TestGitRepository -Path $script:RepoPath
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'Git cmdlet pipeline control flow' {
    It 'ProcessRecord_<CommandName>EarlyPipelineStop_ReturnsOneObjectWithoutErrors' -TestCases @(
        @{ CommandName = 'Get-GitLog'; Parameters = @{} }
        @{ CommandName = 'Get-GitBranch'; Parameters = @{} }
        @{ CommandName = 'Get-GitConfiguration'; Parameters = @{} }
        @{ CommandName = 'Get-GitCommitFile'; Parameters = @{} }
        @{ CommandName = 'Select-GitCommit'; Parameters = @{ Where = { $true } } }
        @{ CommandName = 'Compare-GitTree'; Parameters = @{ Base = 'HEAD~2'; Compare = 'HEAD' } }
        @{ CommandName = 'Invoke-GitRepository'; Parameters = @{ Action = { 1; 2; 3 } } }
    ) {
        param($CommandName, $Parameters)

        $gitErrors = @()
        $result = @(& $CommandName @Parameters -RepoPath $script:RepoPath `
            -ErrorAction Stop -ErrorVariable gitErrors | Select-Object -First 1)

        $result | Should -HaveCount 1
        $gitErrors | Should -BeNullOrEmpty
    }

    It 'ProcessRecord_CopyGitRepositoryEarlyPipelineStop_ClonesWithoutErrors' {
        $clonePath = Join-Path -Path $TestDrive -ChildPath 'clone'
        $gitErrors = @()
        $result = @(Copy-GitRepository -Url $script:RepoPath -LocalPath $clonePath `
            -ErrorAction Stop -ErrorVariable gitErrors | Select-Object -First 1)

        $result | Should -HaveCount 1
        $gitErrors | Should -BeNullOrEmpty
        Test-Path -Path (Join-Path -Path $clonePath -ChildPath '.git') | Should -BeTrue
    }
}

Describe 'Git cmdlet script-block control flow' {
    It 'ProcessRecord_<CommandName><ParameterName>ActionPreferenceStop_PropagatesDespiteSilentlyContinue' -TestCases @(
        @{ CommandName = 'Invoke-GitRepository'; ParameterName = 'Action'; Parameters = @{} }
        @{ CommandName = 'Select-GitCommit'; ParameterName = 'Where'; Parameters = @{} }
        @{ CommandName = 'Compare-GitTree'; ParameterName = 'Where'; Parameters = @{ Base = 'HEAD~2'; Compare = 'HEAD' } }
        @{ CommandName = 'Compare-GitTree'; ParameterName = 'Transform'; Parameters = @{ Base = 'HEAD~2'; Compare = 'HEAD' } }
    ) {
        param($CommandName, $ParameterName, $Parameters)

        $Parameters[$ParameterName] = { Write-Error 'control-flow sentinel' -ErrorAction Stop }

        {
            & $CommandName @Parameters -RepoPath $script:RepoPath -ErrorAction SilentlyContinue
        } | Should -Throw -ExpectedMessage '*control-flow sentinel*'
    }

    It 'ProcessRecord_<CommandName><ParameterName>OrdinaryScriptError_PreservesNonTerminatingError' -TestCases @(
        @{ CommandName = 'Invoke-GitRepository'; ParameterName = 'Action'; Parameters = @{} }
        @{ CommandName = 'Select-GitCommit'; ParameterName = 'Where'; Parameters = @{} }
        @{ CommandName = 'Compare-GitTree'; ParameterName = 'Where'; Parameters = @{ Base = 'HEAD~2'; Compare = 'HEAD' } }
        @{ CommandName = 'Compare-GitTree'; ParameterName = 'Transform'; Parameters = @{ Base = 'HEAD~2'; Compare = 'HEAD' } }
    ) {
        param($CommandName, $ParameterName, $Parameters)

        $Parameters[$ParameterName] = { throw 'ordinary script failure' }
        $gitErrors = @()
        $result = @(& $CommandName @Parameters -RepoPath $script:RepoPath `
            -ErrorAction SilentlyContinue -ErrorVariable gitErrors)

        $result | Should -HaveCount 0
        $gitErrors | Should -Not -BeNullOrEmpty
        $gitErrors[-1].Exception.Message | Should -BeLike '*ordinary script failure*'
    }
}
