@{
    RootModule           = 'PowerCode.Git.dll'
    ModuleVersion        = '0.1.0'
    GUID                 = '86ed19db-80a7-48c4-a04e-1125b82f7cce'
    Author               = 'Staffan Gustafsson'
    CompanyName          = 'PowerCode'
    Copyright            = '(c) Staffan Gustafsson. All rights reserved.'
    Description          = 'A PowerShell module for Git that provides discoverability through standard PowerShell noun-verb naming, rich pipeline support for composing commands, and tab completion for parameters such as branch names, tags, and remotes.'
    PowerShellVersion    = '7.4'
    CompatiblePSEditions = @('Core')
    FormatsToProcess     = @('PowerCode.Git.Format.ps1xml')    
    CmdletsToExport      = @(
        'Get-GitLog'
        'Get-GitStatus'
        'Get-GitDiff'
        'Get-GitBranch'
        'Switch-GitBranch'
        'Get-GitTag'
        'Set-GitTag'
        'Add-GitItem'
        'Save-GitCommit'
        'New-GitBranch'
        'Remove-GitBranch'
        'Reset-GitHead'
        'Copy-GitRepository'
        'Send-GitBranch'
        'Receive-GitBranch'
        'Get-GitWorktree'
        'New-GitWorktree'
        'Remove-GitWorktree'
        'Lock-GitWorktree'
        'Unlock-GitWorktree'
        'Restore-GitItem'
        'Start-GitRebase'
        'Resume-GitRebase'
        'Stop-GitRebase'
        'Get-GitCommitFile'
        'Set-GitConfiguration'
        'Get-GitConfiguration'
        'Get-GitModuleConfiguration'
        'Set-GitModuleConfiguration'
    )
    FunctionsToExport    = @()
    AliasesToExport      = @()

    PrivateData          = @{
            PSData = @{
                ProjectUri = 'https://github.com/powercode/PowerCodeGit'
                LicenseUri = 'https://github.com/powercode/PowerCodeGit/blob/main/LICENSE'
                Tags       = @('Git', 'VersionControl', 'SourceControl')
        }
    }
}