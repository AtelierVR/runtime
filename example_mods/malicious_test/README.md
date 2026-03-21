# Malicious Test Mod

> **⚠️ WARNING — This mod is intentionally malicious.**
> It exists solely to verify that the security system blocks it. If it ever loads successfully, the security system has failed.

## Purpose

This mod contains direct compile-time references to `System.Diagnostics.Process`, which is on the security blacklist. The IL scanner must detect these references and **block the mod before it is loaded** — `OnInitialize` should never execute.

## What It References (Intentionally)

| Symbol | Why It Is Blocked |
|--------|-------------------|
| `System.Diagnostics.Process` | Can launch arbitrary OS processes |
| `System.Diagnostics.ProcessStartInfo` | Configures process execution |

## Expected Behavior

When the loader encounters this mod, you should see:

```
[Mono/Security] Assembly 'MaliciousMod.dll' BLOCKED due to security violations:
  - [BlacklistedTypeReference] System.Diagnostics.Process
      (pattern: ^System\.Diagnostics\.Process(StartInfo)?$)
  - [BlacklistedMethodCall] System.Diagnostics.Process.Start
      (pattern: ^System\.Diagnostics\.Process(StartInfo)?$)
```

The entry point `MaliciousInitializer.OnInitialize` should **never execute**.

## Failure Indicator

If you see any of the following in the console, the security system has failed:

```
!!! SECURITY BREACH !!!
MaliciousMod was allowed to load!
```

## Building

```powershell
cd example_mods/malicious_test
dotnet build -c Release -o build/malicious_test
```

> **Note:** The C# build will succeed — the compiler is unaware of IL-level restrictions. The block happens at **load time**, not compile time.

## Testing

After installing the mod and reloading, check the console:

| Result | Expected Message |
|--------|-----------------|
| **PASS** | `BLOCKED due to security violations` |
| **FAIL** | `SECURITY BREACH` |

## Further Reading

- [Security guide](../../docs/guide/security.md)
- [Blacklist test mod](../blacklist_test/) — validates the validator itself
