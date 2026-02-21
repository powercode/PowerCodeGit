---
applyTo: 'docs/help/**/*.md,tests/PowerCode.Git.SystemTests/**/*.ps1'
description: 'Every PlatyPS help example must have a corresponding system test'
---

# Help Example Testing Policy

Every example in the PlatyPS help documentation (`docs/help/PowerCode.Git/*.md`) **must** have a
corresponding system test in `tests/PowerCode.Git.SystemTests/` that exercises the same scenario.

## Rules

- No example may exist in a help file unless a system test proves it works.
- When adding or modifying a help example, add or update the matching system test.
- When removing a system test, remove the corresponding help example.
- System test names should clearly reference the example they verify.

## Example Format

Each example must have:

1. A description line explaining what the example does.
2. A fenced code block tagged `powershell` containing the command(s).
3. Optionally, a second fenced code block tagged `output` showing expected output.

### Template

~~~markdown
Description of what the example demonstrates.

```powershell
Get-GitLog -MaxCount 5
```

```output
abc1234 Fix typo in README
def5678 Add initial project structure
```
~~~
