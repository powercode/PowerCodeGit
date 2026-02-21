---
applyTo: 'docs/help/**/*.md,scripts/New-CommandHelpDoc.ps1,scripts/Update-HelpDocs.ps1'
description: 'How to create and update PlatyPS command help for PowerCode.Git cmdlets'
---

# Command Help Guidelines

Help documentation lives under `docs/help/PowerCode.Git/` as PlatyPS v2 markdown files
(one file per cmdlet). The tooling uses `Microsoft.PowerShell.PlatyPS` (version 1.0.0 or
later — this **is** the v2 rewrite).

## Creating initial help for a new cmdlet

Run `New-CommandHelpDoc.ps1` in a clean `pwsh` process. This generates the scaffold from
the compiled module via reflection:

```powershell
pwsh -NoProfile -File scripts/New-CommandHelpDoc.ps1 -CommandName Set-GitTag
```

Multiple cmdlets can be bootstrapped at once:

```powershell
pwsh -NoProfile -File scripts/New-CommandHelpDoc.ps1 -CommandName Set-GitTag, Get-GitTag
```

The script refuses to overwrite an existing file. Pass `-Force` to override:

```powershell
pwsh -NoProfile -File scripts/New-CommandHelpDoc.ps1 -CommandName Set-GitTag -Force
```

After the file is generated, **edit it by hand** to add meaningful descriptions and examples.
Every example you add must have a matching system test (see `help-examples.instructions.md`).

## Updating help after parameter changes

When parameters are added, removed, or renamed on an existing cmdlet, run `Update-HelpDocs.ps1`.
It refreshes all parameter blocks while preserving hand-written descriptions and examples:

```powershell
pwsh -NoProfile -File scripts/Update-HelpDocs.ps1
```

Use `-Configuration Release` when you want to update from the release build:

```powershell
pwsh -NoProfile -File scripts/Update-HelpDocs.ps1 -Configuration Release
```

`Update-HelpDocs.ps1` also creates scaffold files for any cmdlet that does **not** yet have
a help file, so it can be used instead of `New-CommandHelpDoc.ps1` when updating everything
in one pass.

## Why a clean pwsh process is required

The PowerCode.Git module is a binary module. Once its assemblies are loaded into a process
they are locked and cannot be replaced by a rebuild. Both scripts must therefore be invoked
via `pwsh -NoProfile -File` (or inside a fresh terminal) rather than dot-sourced or run
inside the same session that built the module.

## PlatyPS version

Always use `Microsoft.PowerShell.PlatyPS` version 1.0.0 or later (the v2 rewrite).
Never use the legacy `platyPS` module. Both scripts enforce this and will install the
correct version automatically if it is missing.
