using namespace System.Management.Automation.Language
<#
.SYNOPSIS
    Parses PowerShell code and outputs structured syntax errors and token information.

.DESCRIPTION
    Wraps [System.Management.Automation.Language.Parser] to validate PowerShell
    scripts without executing them. Returns JSON output suitable for programmatic
    consumption by AI agents and automation tools.

    Accepts one or more file paths (-Path), wildcard patterns, pipeline input,
    or inline code (-Code).
    By default only errors are shown. Use -IncludeTokens to add the full token stream.
    Use -Quiet for exit-code-only mode (0 = clean, 1 = errors found).

.PARAMETER Path
    One or more paths (or wildcard patterns) to PowerShell script files.
    Also accepts paths from the pipeline.

.PARAMETER Code
    A string of PowerShell code to parse.

.PARAMETER IncludeTokens
    When set, includes the full token stream in the output.

.PARAMETER Quiet
    Suppresses all stdout output. The script communicates only via exit code:
    0 = no parse errors, 1 = one or more parse errors found.

.EXAMPLE
    pwsh -NoProfile -File parse-syntax.ps1 -Path ./MyScript.ps1

.EXAMPLE
    pwsh -NoProfile -File parse-syntax.ps1 -Code 'function Foo { param($x }'

.EXAMPLE
    pwsh -NoProfile -File parse-syntax.ps1 -Path ./src/*.ps1 -IncludeTokens

.EXAMPLE
    Get-ChildItem -Recurse -Filter *.ps1 | pwsh -NoProfile -File parse-syntax.ps1

.EXAMPLE
    pwsh -NoProfile -File parse-syntax.ps1 -Path ./src/*.ps1 -Quiet

.OUTPUTS
    JSON object (single file) or JSON array (multiple files) with properties:
    file, hasErrors, errorCount, errors, tokenCount, tokens (optional).
    When -Quiet is set, no output is produced.
#>
[CmdletBinding(DefaultParameterSetName = 'Path')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Path', Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
    [Alias('FullName', 'PSPath')]
    [string[]]$Path,

    [Parameter(Mandatory, ParameterSetName = 'Code')]
    [string]$Code,

    [switch]$IncludeTokens,

    [switch]$Quiet
)

begin {
    Set-StrictMode -Version Latest
    $ErrorActionPreference = 'Stop'

    $allResults = [System.Collections.Generic.List[object]]::new()
    $globalHasErrors = $false

    function ParseOne {
        param([string]$Source, [string]$DisplayPath)

        $tokens = $null
        $errors = $null

        try {
            if ($Source -eq '<inline>') {
                $null = [Parser]::ParseInput($DisplayPath, [ref]$tokens, [ref]$errors)
                # $DisplayPath actually holds the code for inline; $Source flags the mode
                $filePath = '<inline>'
            }
            else {
                $null = [Parser]::ParseFile($Source, [ref]$tokens, [ref]$errors)
                $filePath = $Source
            }
        }
        catch {
            return [pscustomobject]@{
                file       = $DisplayPath
                hasErrors  = $true
                errorCount = 1
                errors     = @(
                    [pscustomobject]@{
                        line            = 0
                        column          = 0
                        errorId         = 'ScriptError'
                        message         = $_.Exception.Message
                        text            = ''
                        incompleteInput = $false
                    }
                )
                tokenCount = 0
            }
        }

        $parsedErrors = @(
            foreach ($e in $errors) {
                [pscustomobject]@{
                    line            = $e.Extent.StartLineNumber
                    column          = $e.Extent.StartColumnNumber
                    errorId         = $e.ErrorId
                    message         = $e.Message
                    text            = $e.Extent.Text
                    incompleteInput = $e.IncompleteInput
                }
            }
        )

        $result = [ordered]@{
            file       = $filePath
            hasErrors  = $errors.Count -gt 0
            errorCount = $errors.Count
            errors     = $parsedErrors
            tokenCount = $tokens.Count
        }

        if ($script:IncludeTokens) {
            $result['tokens'] = @(
                foreach ($t in $tokens) {
                    [pscustomobject]@{
                        line   = $t.Extent.StartLineNumber
                        column = $t.Extent.StartColumnNumber
                        kind   = [string]$t.Kind
                        flags  = [string]$t.TokenFlags
                        text   = $t.Text
                    }
                }
            )
        }

        return [pscustomobject]$result
    }
}

process {
    if ($PSCmdlet.ParameterSetName -eq 'Code') {
        $r = ParseOne -Source '<inline>' -DisplayPath $Code
        if ($r.hasErrors) { $globalHasErrors = $true }
        $allResults.Add($r)
    }
    else {
        foreach ($p in $Path) {
            # Resolve wildcards and multiple paths
            try {
                $resolved = @(Resolve-Path -Path $p -ErrorAction Stop)
            }
            catch {
                # Unresolvable path — record a structured error
                $r = [pscustomobject]@{
                    file       = $p
                    hasErrors  = $true
                    errorCount = 1
                    errors     = @(
                        [pscustomobject]@{
                            line            = 0
                            column          = 0
                            errorId         = 'ScriptError'
                            message         = $_.Exception.Message
                            text            = ''
                            incompleteInput = $false
                        }
                    )
                    tokenCount = 0
                }
                $globalHasErrors = $true
                $allResults.Add($r)
                continue
            }

            foreach ($rp in $resolved) {
                $r = ParseOne -Source $rp.Path -DisplayPath $rp.Path
                if ($r.hasErrors) { $globalHasErrors = $true }
                $allResults.Add($r)
            }
        }
    }
}

end {
    if (-not $Quiet) {
        if ($allResults.Count -eq 1) {
            $allResults[0] | ConvertTo-Json -Depth 5
        }
        else {
            $allResults | ConvertTo-Json -Depth 5
        }
    }

    # Summary to stderr so agents can quickly triage
    $totalErrors = [int]($allResults | Measure-Object -Property errorCount -Sum).Sum
    $filesWithErrors = @($allResults | Where-Object { $_.hasErrors }).Count
    if ($totalErrors -gt 0) {
        [Console]::Error.WriteLine("$totalErrors error(s) in $filesWithErrors of $($allResults.Count) file(s)")
    }

    if ($globalHasErrors) { exit 1 } else { exit 0 }
}
