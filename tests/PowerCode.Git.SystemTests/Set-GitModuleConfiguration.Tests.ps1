#Requires -Modules Pester

<#
.SYNOPSIS
    Pester system tests for the Set-GitModuleConfiguration and Get-GitModuleConfiguration cmdlets.
.DESCRIPTION
    End-to-end tests that exercise the PowerCode.Git binary module
    module configuration commands.
#>

BeforeAll {
    . "$PSScriptRoot/SystemTest-Helpers.ps1"
}

AfterAll {
    Set-GitModuleConfiguration -Reset
    Remove-Module -Name PowerCode.Git -Force -ErrorAction SilentlyContinue
}

Describe 'Set-GitModuleConfiguration -LogMaxCount' {
    AfterEach {
        Set-GitModuleConfiguration -Reset
    }

    It 'Sets the default LogMaxCount' {
        Set-GitModuleConfiguration -LogMaxCount 50

        $Config = Get-GitModuleConfiguration
        $Config.LogMaxCount | Should -Be 50
    }
}

Describe 'Set-GitModuleConfiguration -BranchIncludeDescription' {
    AfterEach {
        Set-GitModuleConfiguration -Reset
    }

    It 'Enables branch descriptions by default' {
        Set-GitModuleConfiguration -BranchIncludeDescription $true

        $Config = Get-GitModuleConfiguration
        $Config.BranchIncludeDescription | Should -BeTrue
    }
}

Describe 'Set-GitModuleConfiguration -Reset' {
    It 'Clears all configuration values' {
        Set-GitModuleConfiguration -LogMaxCount 50 -BranchIncludeDescription $true
        Set-GitModuleConfiguration -Reset

        $Config = Get-GitModuleConfiguration
        $Config.LogMaxCount | Should -BeNullOrEmpty
        $Config.DiffContext | Should -BeNullOrEmpty
        $Config.BranchReferenceBranch | Should -BeNullOrEmpty
        $Config.BranchIncludeDescription | Should -BeNullOrEmpty
    }
}

Describe 'Get-GitModuleConfiguration' {
    AfterEach {
        Set-GitModuleConfiguration -Reset
    }

    It 'Returns the current module configuration' {
        $Config = Get-GitModuleConfiguration

        $Config | Should -Not -BeNullOrEmpty
        $Config | Should -BeOfType 'PowerCode.Git.ModuleConfiguration'
    }

    It 'Reflects a specific setting value' {
        Set-GitModuleConfiguration -LogMaxCount 25

        $Value = (Get-GitModuleConfiguration).LogMaxCount
        $Value | Should -Be 25
    }
}
