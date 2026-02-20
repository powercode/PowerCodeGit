---
description: 'Assembly Load Context isolation pattern for PowerCodeGit modules'
applyTo: 'src/PowerCodeGit.Abstractions/**,src/PowerCodeGit/DependencyContext.cs,src/PowerCodeGit/PowerCodeGitDependencyLoadContext.cs,src/PowerCodeGit.Core/**'
---

# Assembly Load Context Isolation

PowerCodeGit uses a custom `AssemblyLoadContext` to isolate LibGit2Sharp and its native
dependencies from the PowerShell host process. A shared **PowerCodeGit.Abstractions**
assembly provides the interface and model types that both the cmdlet layer (default
ALC) and the core layer (isolated ALC) use, so no reflection is needed after initial
service construction.

## Architecture

```
PowerShell host (default ALC)
 ├─ PowerCodeGit.dll                       ← Cmdlets, ServiceFactory, ModuleInitializer
 ├─ PowerCodeGit.Abstractions.dll          ← Shared interfaces & DTOs
 │
 └─ PowerCodeGit.DependencyContext (isolated ALC, loads from dependencies/)
     ├─ PowerCodeGit.Core.dll              ← Service implementations
     ├─ LibGit2Sharp.dll               ← Third-party dependency
     └─ runtimes/<rid>/native/…        ← Native libraries
```

## Rules for adding new types

### DTOs / Models

1. Define the class in `PowerCodeGit.Abstractions/Models/`.
2. Use the namespace `PowerCodeGit.Abstractions.Models`.
3. The type **must not** reference any type from `PowerCodeGit.Core` or any third-party
   package (e.g. LibGit2Sharp). It may only use BCL types and other Abstractions types.
4. Keep the type as a plain data carrier — constructor + read-only properties, or a
   positional `record`. No behaviour that requires isolated dependencies.
5. Both `PowerCodeGit` (cmdlet project) and `PowerCodeGit.Core` reference this type at
   compile time through their `<ProjectReference>` to `PowerCodeGit.Abstractions`.

### Interfaces / Service contracts

1. Define the interface in `PowerCodeGit.Abstractions/Services/`.
2. Use the namespace `PowerCodeGit.Abstractions.Services`.
3. Parameter and return types **must** come from `PowerCodeGit.Abstractions` or the BCL.
   Never expose a LibGit2Sharp type (or any other isolated dependency) in a contract signature.
4. Keep interfaces focused — one capability per interface (ISP).

### Service implementations (PowerCodeGit.Core)

1. Implement the interface in `PowerCodeGit.Core/Services/`.
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
       var type = coreAssembly!.GetType("PowerCodeGit.Core.Services.MyNewService")
           ?? throw new InvalidOperationException("...");
       return (IMyNewService)Activator.CreateInstance(type)!;
   }
   ```

   This is the **only** place reflection is used — a single `Activator.CreateInstance`
   followed by a direct cast to the shared interface.

2. Expose the factory call through `ServiceFactory` (or inject directly into the cmdlet).
3. The cmdlet works with the interface and DTOs from Abstractions — fully strongly typed.

### PowerCodeGitDependencyLoadContext

The custom `AssemblyLoadContext` **must return `null`** for `PowerCodeGit.Abstractions`
in its `Load` override. This causes the runtime to fall back to the default context,
ensuring both sides share the exact same type identity. Without this, casts from the
isolated instance to the shared interface would fail with `InvalidCastException`.

### Module layout

The `BuildVersionedModuleLayout` MSBuild target produces:

```
artifacts/module/PowerCodeGit/<version>/
  PowerCodeGit.dll
  PowerCodeGit.Abstractions.dll        ← shared, loaded in default ALC
  PowerCodeGit.psd1
  dependencies/
    PowerCodeGit.Core.dll              ← isolated ALC
    PowerCodeGit.Core.deps.json        ← required by AssemblyDependencyResolver
    LibGit2Sharp.dll
    runtimes/
      win-x64/native/git2-*.dll
      linux-x64/native/libgit2-*.so
      osx-arm64/native/libgit2-*.dylib
      …
```

`PowerCodeGit.Abstractions.dll` **must** be at the module root (not only in `dependencies/`)
so it loads in the default ALC. Any copy that ends up in `dependencies/` is harmless
because the custom ALC skips it.

## Checklist for every new cross-boundary type

- [ ] Type is defined in `PowerCodeGit.Abstractions` with no third-party dependencies.
- [ ] `PowerCodeGit.Core` implementation maps isolated types → Abstractions DTOs.
- [ ] Implementation has a public parameterless constructor.
- [ ] `DependencyContext` has a factory method with `Activator.CreateInstance` + cast.
- [ ] `PowerCodeGitDependencyLoadContext.Load` still returns `null` for `PowerCodeGit.Abstractions`.
- [ ] Cmdlet uses only Abstractions types — no direct reference to `PowerCodeGit.Core`.
