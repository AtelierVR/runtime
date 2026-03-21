# Hello World Example Mod

A minimal example mod demonstrating the Nox modding system. It prints "Hello World!" to the console during initialization.

## What You Will Learn

- How to declare a mod manifest (`nox.mod.json`)
- How to implement `IMainModInitializer` lifecycle methods
- How to use `ILoggerAPI` for structured logging
- How to build and install a mod

## Structure

| File | Purpose |
|------|---------|
| `nox.mod.json` | Mod manifest — identity, metadata, entry points |
| `HelloWorldInitializer.cs` | Entry point implementing `IMainModInitializer` |
| `HelloWorldMod.csproj` | C# project targeting `netstandard2.1` |
| `build.ps1` | PowerShell build + optional install script |

## Prerequisites

- [.NET SDK 6+](https://dotnet.microsoft.com/download)
- Unity project opened at least once (generates `Library/ScriptAssemblies/`)

## Building

```powershell
cd example_mods/hello_world
dotnet build -c Release -o build/hello_world
```

The output folder `build/hello_world/` will contain:
- `HelloWorldMod.dll` — the compiled assembly
- `nox.mod.json` — the manifest (copied by the build script)

## Installation

Copy the build output folder to your mods directory:

```
%APPDATA%\.nox\mods\hello_world\
```

Or use the build script with the `-Install` flag:

```powershell
.\build.ps1 -Install
```

## Expected Console Output

On load:

```
[HelloWorld] OnInitialize called!
==========================================
          Hello World!
   From HelloWorldMod example mod
==========================================
[HelloWorld] OnPostInitializeMain - Mod fully initialized!
```

On unload:

```
[HelloWorld] OnPreDispose - Mod is about to be disposed!
[HelloWorld] OnDispose - Goodbye World!
```

## Implementation Notes

- `OnInitialize(IModCoreAPI)` is called first — store the `api` reference here.
- `OnInitializeMain(IMainModCoreAPI)` is called in the main application context — this is where the "Hello World!" message is printed.
- Always use `ILoggerAPI` (`_api.LoggerAPI.Log(...)`) instead of `Debug.Log()` — it automatically tags output with your mod ID.
- Null `_api` in `OnDispose` to release the reference cleanly.

## Further Reading

- [Getting Started guide](../../docs/guide/getting-started.md)
- [Lifecycle reference](../../docs/guide/lifecycle.md)
- [Core API reference](../../docs/reference/core-api.md)
```
