---
applyTo: 'tests/PowerCode.Git.SystemTests/**/*.ps1,scripts/Invoke-SystemTests.ps1'
description: 'How to run system tests for PowerCode.Git cmdlets'
---

# Running System Tests

System tests live in `tests/PowerCode.Git.SystemTests/` and are run inside a
clean `pwsh` child process so that binary module assemblies do not lock the
build outputs of the hosting process.

## Run all system tests

```powershell
.\scripts\Invoke-SystemTests.ps1
```

## Run tests for a specific command

Pass the cmdlet name (without the `.Tests.ps1` suffix) to `-CommandName`:

```powershell
.\scripts\Invoke-SystemTests.ps1 -CommandName Get-GitBranch
```

Multiple commands can be specified as a comma-separated list:

```powershell
.\scripts\Invoke-SystemTests.ps1 -CommandName Get-GitBranch, Save-GitCommit
```

## Skip the build step

When the solution is already built, add `-NoBuild` to avoid a redundant
`dotnet build` invocation:

```powershell
.\scripts\Invoke-SystemTests.ps1 -CommandName Set-GitTag -NoBuild
```

## Tab completion

`-CommandName` supports tab completion — it enumerates the `*.Tests.ps1` files
in the system-test directory automatically.
