---
name: powershell-syntax-parser
description: 'Use the PowerShell language parser to troubleshoot script syntax errors. Provides structured access to tokens and parse errors via [System.Management.Automation.Language.Parser]. Use when a user needs to diagnose syntax problems, inspect token streams, or validate PowerShell scripts without executing them.'
---

# PowerShell Syntax Parser Skill

Parse PowerShell scripts and extract **tokens** and **parse errors** as structured JSON — without executing any code.

## Bundled Script

The [parse-syntax.ps1](./scripts/parse-syntax.ps1) script wraps `[System.Management.Automation.Language.Parser]` and returns structured JSON. It exits `0` on success and `1` when errors are found. A summary is written to stderr for quick triage.

### Parameters

| Parameter | Required | Description |
|---|---|---|
| `-Path` | Yes* | One or more file paths or wildcard patterns. Accepts pipeline input. |
| `-Code` | Yes* | Inline PowerShell code string to parse. |
| `-IncludeTokens` | No | Adds the full token stream to the JSON output. |
| `-Quiet` | No | Suppresses JSON output; communicates only via exit code. |

*One of `-Path` or `-Code` is required.

### Examples

```powershell
# Parse a single file
pwsh -NoProfile -File ./scripts/parse-syntax.ps1 -Path ./MyScript.ps1

# Parse inline code
pwsh -NoProfile -File ./scripts/parse-syntax.ps1 -Code 'function Foo { param($x }'

# Parse multiple files via wildcards
pwsh -NoProfile -File ./scripts/parse-syntax.ps1 -Path ./src/*.ps1

# Include the full token stream
pwsh -NoProfile -File ./scripts/parse-syntax.ps1 -Path ./MyScript.ps1 -IncludeTokens

# Exit-code-only for CI gates
pwsh -NoProfile -File ./scripts/parse-syntax.ps1 -Path ./src/*.ps1 -Quiet
```

Within a PowerShell session you can also pass arrays or pipe `Get-ChildItem`:

```powershell
& ./scripts/parse-syntax.ps1 -Path ./src/Module.ps1, ./src/Helpers.ps1

Get-ChildItem -Recurse -Filter *.ps1 | & ./scripts/parse-syntax.ps1
```

> **Note:** `pwsh -File` does not support array parameters or pipeline input from the calling shell. Use wildcards with `pwsh -File`, or call the script directly within a session for those scenarios.

### JSON Output Shape

```json
{
  "file": "C:\\Scripts\\MyScript.ps1",
  "hasErrors": true,
  "errorCount": 1,
  "errors": [
    {
      "line": 3,
      "column": 5,
      "errorId": "MissingEndCurlyBrace",
      "message": "Missing closing '}' in statement block or type definition.",
      "text": "{",
      "incompleteInput": true
    }
  ],
  "tokenCount": 12
}
```

Multiple files produce a JSON **array** of these objects. When `-IncludeTokens` is set, each object also includes a `tokens` array with `line`, `column`, `kind`, `flags`, and `text` per token.

## Raw API (Advanced)

For in-session use without the bundled script, call the parser directly:

```powershell
using namespace System.Management.Automation.Language

$tokens = $null; $errors = $null
$ast = [Parser]::ParseFile('.\MyScript.ps1', [ref]$tokens, [ref]$errors)
# Or: $ast = [Parser]::ParseInput($codeString, [ref]$tokens, [ref]$errors)

if ($errors.Count -gt 0) {
    $errors | ForEach-Object {
        [pscustomobject]@{
            Line    = $_.Extent.StartLineNumber
            Column  = $_.Extent.StartColumnNumber
            ErrorId = $_.ErrorId
            Message = $_.Message
        }
    } | Format-Table -AutoSize
}
```

## Key Notes

- Parsing is **safe** — no code is executed.
- Partial ASTs are returned even when errors exist.
- `incompleteInput = true` means truncated input (unterminated string, missing brace) rather than an illegal construct.
- Use `$ast.FindAll({ param($node) $node -is [FunctionDefinitionAst] }, $true)` to walk the AST for specific node types.
