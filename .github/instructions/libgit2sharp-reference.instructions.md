---
description: 'LibGit2Sharp source code reference available in adjacent repo directory'
applyTo: 'src/PowerCode.Git.Core/**,src/PowerCode.Git.Abstractions/**'
---

# LibGit2Sharp Reference

The full source code for [LibGit2Sharp](https://github.com/libgit2/libgit2sharp) is
available in a sibling directory next to this repository (e.g. `../libgit2sharp`).

## Test suite as usage reference

The LibGit2Sharp test suite is an excellent source of information on how the library is
intended to be used. When implementing or modifying `PowerCode.Git.Core` services that
wrap LibGit2Sharp, consult the corresponding tests in the LibGit2Sharp repository to
understand correct API usage, expected behaviour, and edge cases.

Key test locations:

- `../libgit2sharp/LibGit2Sharp.Tests/` — unit and integration tests covering all
  major LibGit2Sharp operations (clone, fetch, push, merge, checkout, diff, tags,
  worktrees, etc.).

## When to consult

- Before wrapping a new LibGit2Sharp API in a `PowerCode.Git.Core` service.
- When debugging unexpected behaviour from a LibGit2Sharp call.
- When unsure about correct option types, enum values, or callback signatures.
