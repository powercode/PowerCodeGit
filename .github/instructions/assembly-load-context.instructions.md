---
description: 'Assembly Load Context isolation pattern for PowerCode.Git modules'
applyTo: 'src/PowerCode.Git.Abstractions/**,src/PowerCode.Git/DependencyContext.cs,src/PowerCode.Git/PowerCodeGitDependencyLoadContext.cs,src/PowerCode.Git.Core/**'
---

# Assembly Load Context Isolation

PowerCode.Git uses a custom `AssemblyLoadContext` to isolate LibGit2Sharp and its native
dependencies from the PowerShell host process. A shared **PowerCode.Git.Abstractions**
assembly provides the interface and model types that both the cmdlet layer (default
ALC) and the core layer (isolated ALC) use, so no reflection is needed after initial
service construction.

## Architecture

```
PowerShell host (default ALC)
 ├─ PowerCode.Git.dll                       ← Cmdlets, ServiceFactory, ModuleInitializer
 ├─ PowerCode.Git.Abstractions.dll          ← Shared interfaces & DTOs
 │
 └─ PowerCode.Git.DependencyContext (isolated ALC, loads from dependencies/)
     ├─ PowerCode.Git.Core.dll              ← Service implementations
     ├─ LibGit2Sharp.dll               ← Third-party dependency
     └─ runtimes/<rid>/native/…        ← Native libraries
```

## Rules for adding new types

### DTOs / Models

1. Define the class in `PowerCode.Git.Abstractions/Models/`.
2. Use the namespace `PowerCode.Git.Abstractions.Models`.
3. The type **must not** reference any type from `PowerCode.Git.Core` or any third-party
   package (e.g. LibGit2Sharp). It may only use BCL types and other Abstractions types.
4. Keep the type as a plain data carrier — constructor + read-only properties, or a
   positional `record`. No behaviour that requires isolated dependencies.
5. Both `PowerCode.Git` (cmdlet project) and `PowerCode.Git.Core` reference this type at
   compile time through their `<ProjectReference>` to `PowerCode.Git.Abstractions`.

### Interfaces / Service contracts

1. Define the interface in `PowerCode.Git.Abstractions/Services/`.
2. Use the namespace `PowerCode.Git.Abstractions.Services`.
3. Parameter and return types **must** come from `PowerCode.Git.Abstractions` or the BCL.
   Never expose a LibGit2Sharp type (or any other isolated dependency) in a contract signature.
4. Keep interfaces focused — one capability per interface (ISP).

### Service implementations (PowerCode.Git.Core)

1. Implement the interface in `PowerCode.Git.Core/Services/`.
2. The class references LibGit2Sharp freely — it runs inside the isolated ALC.
3. Map LibGit2Sharp objects to Abstractions DTOs before returning.
4. The implementation **must** have a public parameterless constructor so
   `DependencyContext` can instantiate it via `Activator.CreateInstance`.

### Wiring a new service into the cmdlet layer

1. Add a factory method in `DependencyContext`:

   ```csharp
   public static IMyNewService CreateMyNewService()
   {
       EnsureInitialized();
       var type = coreAssembly!.GetType("PowerCode.Git.Core.Services.MyNewService")
           ?? throw new InvalidOperationException("...");
       return (IMyNewService)Activator.CreateInstance(type)!;
   }
   ```

   This is the **only** place reflection is used — a single `Activator.CreateInstance`
   followed by a direct cast to the shared interface.

2. Expose the factory call through `ServiceFactory` (or inject directly into the cmdlet).
3. The cmdlet works with the interface and DTOs from Abstractions — fully strongly typed.

### PowerCodeGitDependencyLoadContext

The custom `AssemblyLoadContext` **must return `null`** for `PowerCode.Git.Abstractions`
in its `Load` override. This causes the runtime to fall back to the default context,
ensuring both sides share the exact same type identity. Without this, casts from the
isolated instance to the shared interface would fail with `InvalidCastException`.

### Module layout

The `BuildVersionedModuleLayout` MSBuild target produces:

```
artifacts/module/PowerCode.Git/<version>/
  PowerCode.Git.dll
  PowerCode.Git.Abstractions.dll        ← shared, loaded in default ALC
  PowerCode.Git.psd1
  dependencies/
    PowerCode.Git.Core.dll              ← isolated ALC
    PowerCode.Git.Core.deps.json        ← required by AssemblyDependencyResolver
    LibGit2Sharp.dll
    runtimes/
      win-x64/native/git2-*.dll
      linux-x64/native/libgit2-*.so
      osx-arm64/native/libgit2-*.dylib
      …
```

`PowerCode.Git.Abstractions.dll` **must** be at the module root (not only in `dependencies/`)
so it loads in the default ALC. Any copy that ends up in `dependencies/` is harmless
because the custom ALC skips it.

## Checklist for every new cross-boundary type

- [ ] Type is defined in `PowerCode.Git.Abstractions` with no third-party dependencies.
- [ ] `PowerCode.Git.Core` implementation maps isolated types → Abstractions DTOs.
- [ ] Implementation has a public parameterless constructor.
- [ ] `DependencyContext` has a factory method with `Activator.CreateInstance` + cast.
- [ ] `PowerCodeGitDependencyLoadContext.Load` still returns `null` for `PowerCode.Git.Abstractions`.
- [ ] Cmdlet uses only Abstractions types — no direct reference to `PowerCode.Git.Core`.
