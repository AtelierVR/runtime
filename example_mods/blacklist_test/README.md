# Blacklist Test Mod

A security test mod that verifies the assembly blacklist system correctly recognizes dangerous types **before any mod is loaded**. This mod loads successfully — it tests the *validator* itself, not the blocked types directly.

## How It Works

The mod uses reflection to locate `AssemblySecurityValidator` and calls its `IsTypeBlacklisted` method for a list of known-dangerous types. It does **not** directly reference any blacklisted type, so it passes the IL scan and loads normally.

## What It Tests

The following dangerous types must be recognized by the blacklist:

| Type | Category |
|------|----------|
| `System.Diagnostics.Process` | Process execution |
| `System.Diagnostics.ProcessStartInfo` | Process configuration |
| `System.Net.Sockets.Socket` | Raw socket access |
| `System.Net.Sockets.TcpClient` | TCP networking |
| `System.Reflection.Emit.AssemblyBuilder` | Dynamic assembly creation |
| `System.Reflection.Emit.TypeBuilder` | Dynamic type definition |
| `System.Reflection.Emit.ILGenerator` | IL code generation |
| `System.Reflection.Emit.DynamicMethod` | Dynamic methods |
| `Microsoft.CSharp.CSharpCodeProvider` | Runtime C# compilation |
| `System.Security.SecurityManager` | Security manipulation |
| `Microsoft.Win32.Registry` | Windows Registry |
| `Microsoft.Win32.RegistryKey` | Registry key access |
| `System.AppDomain` | AppDomain access |

## Expected Output

```
  ✓ System.Diagnostics.Process
      (Process execution) - Pattern: ^System\.Diagnostics\.Process(StartInfo)?$
  ✓ System.Net.Sockets.Socket
      ...
Type Blacklist Results: 15/15 dangerous types recognized
>>> TYPE BLACKLIST TEST PASSED <<<
```

If the `AssemblySecurityValidator` is not found (e.g., running in IL2CPP), the mod falls back to legacy runtime reflection tests.

## Building

```powershell
cd example_mods/blacklist_test
dotnet build -c Release -o build/blacklist_test
```

## Installation

Copy the build output to your mods directory:

```
%APPDATA%\.nox\mods\blacklist_test\
```

## How the Security System Works

1. When a mod folder is discovered, the loader calls `AssemblySecurityValidator` **before** loading any DLL.
2. The validator uses **Mono.Cecil** to scan IL bytecode for references to forbidden types.
3. If a violation is found, the mod is blocked and an error is logged — `OnInitialize` is never called.
4. The blacklist is regex-based and permission-aware: a mod that declares the required permission is allowed to use that type.

To test that a malicious mod is blocked, see [`example_mods/malicious_test`](../malicious_test/).

## Further Reading

- [Security guide](../../docs/guide/security.md)
- [Permissions guide](../../docs/guide/permissions.md)
