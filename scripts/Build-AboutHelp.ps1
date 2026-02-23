<#
.SYNOPSIS
    Generates about_PowerCode.Git.help.txt from the PlatyPS module markdown.
.DESCRIPTION
    Reads the PlatyPS v2 module page at docs/help/PowerCode.Git/PowerCode.Git.md
    using Import-MarkdownModuleFile and converts it into a plain-text about topic
    that PowerShell can display via Get-Help about_PowerCode.Git.

    The output file follows the strict positional format required by Get-Help:
      Line 1: TOPIC
      Line 2:     about_PowerCode.Git   (4-space indent)
      Line 3: blank
      Line 4: SYNOPSIS
      Line 5:     <description>         (4-space indent)

    Subsequent sections (LONG DESCRIPTION, CMDLETS, SEE ALSO) are freeform.
    All lines are kept within 80 characters. The file is encoded as UTF-8 with BOM.

    The script installs Microsoft.PowerShell.PlatyPS from PSGallery if it is
    not already available.
.PARAMETER Configuration
    The build configuration whose module layout to target. Defaults to 'debug'.
.PARAMETER OutputPath
    Optional override for the output directory. When omitted the script
    auto-discovers the module layout under artifacts/module/PowerCode.Git/<version>/en-US.
.EXAMPLE
    .\scripts\Build-AboutHelp.ps1
.EXAMPLE
    .\scripts\Build-AboutHelp.ps1 -Configuration Release
.EXAMPLE
    .\scripts\Build-AboutHelp.ps1 -OutputPath ./artifacts/help/en-US
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('debug', 'release')]
    [string]$Configuration = 'debug',

    [Parameter()]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path -Path "$PSScriptRoot/..").Path
$ModulePagePath = Join-Path -Path $RepoRoot -ChildPath 'docs/help/PowerCode.Git/PowerCode.Git.md'

if (-not (Test-Path -Path $ModulePagePath)) {
    Write-Error -Message "Module page not found: $ModulePagePath"
    return
}

# Resolve the output directory (en-US alongside the MAML XML)
if (-not $OutputPath) {
    $ModuleLayoutDir = Join-Path -Path $RepoRoot -ChildPath 'artifacts/module/PowerCode.Git'
    $VersionedDir = Get-ChildItem -Path $ModuleLayoutDir -Directory -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $VersionedDir) {
        Write-Error -Message "No versioned module folder found under '$ModuleLayoutDir'. Build the solution first."
        return
    }
    $OutputPath = Join-Path -Path $VersionedDir.FullName -ChildPath 'en-US'
}

if (-not (Test-Path -Path $OutputPath)) {
    $null = New-Item -Path $OutputPath -ItemType Directory -Force
}

# Ensure Microsoft.PowerShell.PlatyPS is available
$PlatyPSModule = Get-Module -Name Microsoft.PowerShell.PlatyPS -ListAvailable |
    Select-Object -First 1
if (-not $PlatyPSModule) {
    Write-Host 'Installing Microsoft.PowerShell.PlatyPS...' -ForegroundColor Yellow
    Install-PSResource -Name Microsoft.PowerShell.PlatyPS -TrustRepository -Repository PSGallery
}

Import-Module -Name Microsoft.PowerShell.PlatyPS -Force

Write-Host 'Generating about_PowerCode.Git.help.txt...' -ForegroundColor Cyan
Write-Host "  Source: $ModulePagePath" -ForegroundColor DarkGray
Write-Host "  Output: $OutputPath" -ForegroundColor DarkGray

# Parse the module page with PlatyPS v2
$ModuleInfo = Import-MarkdownModuleFile -Path $ModulePagePath
$Description = $ModuleInfo.Description
$Commands = $ModuleInfo.CommandGroups | ForEach-Object -MemberName Commands

# --- Helper: wrap a single paragraph to fit within a maximum line width ---
function Format-WrappedText {
    param(
        [string]$Text,
        [int]$MaxWidth = 80,
        [int]$IndentSize = 4
    )

    $Indent = ' ' * $IndentSize
    $Available = $MaxWidth - $IndentSize
    $Words = $Text -split '\s+'
    $Lines = [System.Collections.Generic.List[string]]::new()
    $Current = ''

    foreach ($Word in $Words) {
        if ($Current.Length -eq 0) {
            $Current = $Word
        }
        elseif (($Current.Length + 1 + $Word.Length) -le $Available) {
            $Current += " $Word"
        }
        else {
            $Lines.Add("$Indent$Current")
            $Current = $Word
        }
    }

    if ($Current.Length -gt 0) {
        $Lines.Add("$Indent$Current")
    }

    return $Lines
}

# --- Build the about-topic text ---
$Sb = [System.Text.StringBuilder]::new()

# TOPIC (lines 1-3: strict positional format)
$null = $Sb.AppendLine('TOPIC')
$null = $Sb.AppendLine('    about_PowerCode.Git')
$null = $Sb.AppendLine()

# SYNOPSIS (lines 4-5: strict positional format)
$null = $Sb.AppendLine('SYNOPSIS')
foreach ($Line in (Format-WrappedText -Text $Description)) {
    $null = $Sb.AppendLine($Line)
}
$null = $Sb.AppendLine()

# LONG DESCRIPTION
$null = $Sb.AppendLine('LONG DESCRIPTION')
foreach ($Line in (Format-WrappedText -Text $Description)) {
    $null = $Sb.AppendLine($Line)
}
$null = $Sb.AppendLine()

# CMDLETS
$null = $Sb.AppendLine('CMDLETS')
foreach ($Cmd in $Commands) {
    $null = $Sb.AppendLine("    $($Cmd.Name)")
    foreach ($Line in (Format-WrappedText -Text $Cmd.Description -IndentSize 8)) {
        $null = $Sb.AppendLine($Line)
    }
    $null = $Sb.AppendLine()
}

# SEE ALSO
$null = $Sb.AppendLine('SEE ALSO')
$null = $Sb.AppendLine('    https://github.com/PowerCode/PowerGit')

# Write with UTF-8 BOM encoding
$AboutFilePath = Join-Path -Path $OutputPath -ChildPath 'about_PowerCode.Git.help.txt'
$Utf8Bom = [System.Text.UTF8Encoding]::new($true)
[System.IO.File]::WriteAllText($AboutFilePath, $Sb.ToString(), $Utf8Bom)

if (Test-Path -Path $AboutFilePath) {
    Write-Host "About topic generated: $AboutFilePath" -ForegroundColor Green
}
else {
    Write-Error -Message "Expected about topic was not created at '$AboutFilePath'."
}
