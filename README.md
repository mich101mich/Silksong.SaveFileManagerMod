# SaveFileManager

A Hollow Knight: Silksong mod.

## Dependency Source Setup

This project is configured with two dependency modes:

- Runtime/build mode: references game and BepInEx DLLs from `SilksongPath.props`.
- Editor/design-time mode: references decompiled/source projects so go-to-definition and symbol navigation work.

Use the setup script to populate those editor sources:

```powershell
./scripts/Update-DependencySources.ps1 -Full
```

What it does:

- Clones `https://github.com/BepInEx/BepInEx` and `https://github.com/BepInEx/HarmonyX` into the paths configured in `SilksongPath.props` if they are missing.
- Decompiles the required Silksong + BepInEx DLLs into compilable projects under `$(SilksongSourceFolder)`.

Useful flags:

```powershell
# Clone/update repos and decompile everything
./scripts/Update-DependencySources.ps1 -Full

# Clone/update repositories only
./scripts/Update-DependencySources.ps1 -CloneRepos

# Regenerate decompiled projects only
./scripts/Update-DependencySources.ps1 -Decompile

# Also pull latest changes for already cloned repositories
./scripts/Update-DependencySources.ps1 -CloneRepos -Pull
```
