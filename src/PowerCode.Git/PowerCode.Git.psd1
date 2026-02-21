@{
    RootModule           = 'PowerCode.Git.dll'
    ModuleVersion        = '0.1.0'
    GUID                 = '86ed19db-80a7-48c4-a04e-1125b82f7cce'
    Author               = 'Staffan Gustafsson'
    CompanyName          = 'PowerCode'
    Copyright            = '(c) Staffan Gustafsson. All rights reserved.'
    Description          = 'PowerShell Git binary module implemented in C#.'
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
    )
    FunctionsToExport    = @()
    AliasesToExport      = @()
}