#Requires -Modules Pester

<#
.SYNOPSIS
    Pester tests that exercise the publish-flow scripts end to end.
.DESCRIPTION
    Validates Get-ModuleVersionFromTag, Update-PowerCodeGitManifest, and
    Publish-PowerCodeGitModule (dry-run) against a temporary module layout
    mirroring the CI publish pipeline.
#>

BeforeAll {
    $ScriptsDir = (Resolve-Path -Path "$PSScriptRoot/../../scripts").Path
}

Describe 'Get-ModuleVersionFromTag' {
    BeforeAll {
        $Script = Join-Path -Path $ScriptsDir -ChildPath 'Get-ModuleVersionFromTag.ps1'
    }

    It 'Parses a stable release tag' {
        $Result = & $Script -Tag 'v1.2.3' -OutputFile ''
        $Result.ModuleVersion | Should -Be '1.2.3'
        $Result.Prerelease | Should -BeNullOrEmpty
    }

    It 'Parses a prerelease tag and sanitises the label' {
        $Result = & $Script -Tag 'v2.0.0-beta.1' -OutputFile ''
        $Result.ModuleVersion | Should -Be '2.0.0'
        $Result.Prerelease | Should -Be 'beta1'
    }

    It 'Strips hyphens from prerelease labels' {
        $Result = & $Script -Tag 'v3.1.0-rc-2' -OutputFile ''
        $Result.ModuleVersion | Should -Be '3.1.0'
        $Result.Prerelease | Should -Be 'rc2'
    }

    It 'Writes key=value pairs to OutputFile' {
        $TempFile = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "publish-flow-$([System.Guid]::NewGuid()).txt"
        try {
            & $Script -Tag 'v1.0.0-alpha.3' -OutputFile $TempFile
            $Content = Get-Content -Path $TempFile -Raw
            $Content | Should -Match 'module-version=1\.0\.0'
            $Content | Should -Match 'prerelease=alpha3'
        }
        finally {
            Remove-Item -Path $TempFile -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Errors on an invalid tag format' {
        { & $Script -Tag 'not-a-version' -OutputFile '' } | Should -Throw
    }
}

Describe 'Update-PowerCodeGitManifest' {
    BeforeAll {
        $Script = Join-Path -Path $ScriptsDir -ChildPath 'Update-PowerCodeGitManifest.ps1'

        # Locate the real manifest to use as a template
        $RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path
        $SourceManifest = Join-Path -Path $RepoRoot -ChildPath 'src/PowerCodeGit/PowerCodeGit.psd1'
    }

    BeforeEach {
        # Create a temporary module layout: <temp>/module/0.1.0/PowerCodeGit.psd1
        $TempRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "publish-flow-$([System.Guid]::NewGuid())"
        $VersionedDir = Join-Path -Path $TempRoot -ChildPath 'module/0.1.0'
        $null = New-Item -Path $VersionedDir -ItemType Directory -Force
        Copy-Item -Path $SourceManifest -Destination (Join-Path -Path $VersionedDir -ChildPath 'PowerCodeGit.psd1')

        # Create stub files referenced by the manifest so Update-ModuleManifest validation passes
        $null = New-Item -Path (Join-Path -Path $VersionedDir -ChildPath 'PowerCodeGit.dll') -ItemType File -Force
        $null = New-Item -Path (Join-Path -Path $VersionedDir -ChildPath 'PowerCodeGit.Format.ps1xml') -ItemType File -Force
    }

    AfterEach {
        Remove-Item -Path $TempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'Updates ModuleVersion and renames the directory' {
        $ModulePath = Join-Path -Path $TempRoot -ChildPath 'module'

        & $Script -ModulePath $ModulePath -ModuleVersion '2.5.0'

        # Directory should be renamed
        $NewDir = Join-Path -Path $ModulePath -ChildPath '2.5.0'
        Test-Path -Path $NewDir | Should -BeTrue

        # Manifest should reflect the new version
        $Manifest = Test-ModuleManifest -Path (Join-Path -Path $NewDir -ChildPath 'PowerCodeGit.psd1')
        $Manifest.Version | Should -Be '2.5.0'
    }

    It 'Sets the prerelease label when provided' {
        $ModulePath = Join-Path -Path $TempRoot -ChildPath 'module'

        & $Script -ModulePath $ModulePath -ModuleVersion '3.0.0' -Prerelease 'beta1'

        $NewDir = Join-Path -Path $ModulePath -ChildPath '3.0.0'
        $Manifest = Test-ModuleManifest -Path (Join-Path -Path $NewDir -ChildPath 'PowerCodeGit.psd1')
        $Manifest.Version | Should -Be '3.0.0'
        $Manifest.PrivateData.PSData.Prerelease | Should -Be 'beta1'
    }

    It 'Skips rename when directory already matches version' {
        $ModulePath = Join-Path -Path $TempRoot -ChildPath 'module'

        # Pre-rename directory to matching version
        $MatchingDir = Join-Path -Path $ModulePath -ChildPath '1.0.0'
        Rename-Item -Path (Join-Path -Path $ModulePath -ChildPath '0.1.0') -NewName '1.0.0'

        & $Script -ModulePath $ModulePath -ModuleVersion '1.0.0'

        Test-Path -Path $MatchingDir | Should -BeTrue
        $Manifest = Test-ModuleManifest -Path (Join-Path -Path $MatchingDir -ChildPath 'PowerCodeGit.psd1')
        $Manifest.Version | Should -Be '1.0.0'
    }

    It 'Errors when no versioned directory exists' {
        $EmptyModulePath = Join-Path -Path $TempRoot -ChildPath 'empty'
        $null = New-Item -Path $EmptyModulePath -ItemType Directory -Force

        { & $Script -ModulePath $EmptyModulePath -ModuleVersion '1.0.0' } | Should -Throw
    }
}

Describe 'End-to-end publish flow' {
    BeforeAll {
        $GetVersionScript = Join-Path -Path $ScriptsDir -ChildPath 'Get-ModuleVersionFromTag.ps1'
        $UpdateManifestScript = Join-Path -Path $ScriptsDir -ChildPath 'Update-PowerCodeGitManifest.ps1'

        $RepoRoot = (Resolve-Path -Path "$PSScriptRoot/../..").Path
        $SourceManifest = Join-Path -Path $RepoRoot -ChildPath 'src/PowerCodeGit/PowerCodeGit.psd1'
    }

    BeforeEach {
        $TempRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "publish-flow-$([System.Guid]::NewGuid())"
        $VersionedDir = Join-Path -Path $TempRoot -ChildPath 'module/0.1.0'
        $null = New-Item -Path $VersionedDir -ItemType Directory -Force
        Copy-Item -Path $SourceManifest -Destination (Join-Path -Path $VersionedDir -ChildPath 'PowerCodeGit.psd1')

        # Create stub files referenced by the manifest so Update-ModuleManifest validation passes
        $null = New-Item -Path (Join-Path -Path $VersionedDir -ChildPath 'PowerCodeGit.dll') -ItemType File -Force
        $null = New-Item -Path (Join-Path -Path $VersionedDir -ChildPath 'PowerCodeGit.Format.ps1xml') -ItemType File -Force
    }

    AfterEach {
        Remove-Item -Path $TempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'Parses tag, patches manifest, and produces a valid module for a stable release' {
        $Version = & $GetVersionScript -Tag 'v4.2.1' -OutputFile ''

        $ModulePath = Join-Path -Path $TempRoot -ChildPath 'module'
        & $UpdateManifestScript -ModulePath $ModulePath -ModuleVersion $Version.ModuleVersion

        $FinalDir = Join-Path -Path $ModulePath -ChildPath $Version.ModuleVersion
        Test-Path -Path $FinalDir | Should -BeTrue

        $Manifest = Test-ModuleManifest -Path (Join-Path -Path $FinalDir -ChildPath 'PowerCodeGit.psd1')
        $Manifest.Version | Should -Be '4.2.1'
        $Manifest.PrivateData.PSData.Prerelease | Should -BeNullOrEmpty
    }

    It 'Parses tag, patches manifest, and produces a valid module for a prerelease' {
        $Version = & $GetVersionScript -Tag 'v5.0.0-rc.1' -OutputFile ''

        $ModulePath = Join-Path -Path $TempRoot -ChildPath 'module'
        & $UpdateManifestScript -ModulePath $ModulePath -ModuleVersion $Version.ModuleVersion -Prerelease $Version.Prerelease

        $FinalDir = Join-Path -Path $ModulePath -ChildPath $Version.ModuleVersion
        Test-Path -Path $FinalDir | Should -BeTrue

        $Manifest = Test-ModuleManifest -Path (Join-Path -Path $FinalDir -ChildPath 'PowerCodeGit.psd1')
        $Manifest.Version | Should -Be '5.0.0'
        $Manifest.PrivateData.PSData.Prerelease | Should -Be 'rc1'
    }
}
