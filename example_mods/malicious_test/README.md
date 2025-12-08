# Malicious Test Mod

**⚠️ WARNING: This mod is intentionally malicious for testing purposes!**

## Purpose

This mod is designed to test the assembly security validator. It contains direct compile-time references to blacklisted types:

- `System.Diagnostics.Process`
- `System.Diagnostics.ProcessStartInfo`

## Expected Behavior

When you try to install and load this mod:

1. The `AssemblySecurityValidator` should scan the DLL using Mono.Cecil
2. It should detect the references to `System.Diagnostics.Process`
3. The mod should be **BLOCKED** from loading
4. You should see log messages like:
   ```
   [Mono/Security] Assembly 'MaliciousMod.dll' BLOCKED due to security violations:
     - [BlacklistedTypeReference] System.Diagnostics.Process (pattern: ^System\.Diagnostics\.Process(StartInfo)?$)
     - [BlacklistedMethodCall] System.Diagnostics.Process.Start (pattern: ^System\.Diagnostics\.Process(StartInfo)?$)
   ```

## If This Mod Loads Successfully

**THE SECURITY SYSTEM HAS FAILED!**

If you see messages from `MaliciousInitializer.OnInitialize`, it means:
- The IL security scanner is not working
- Mods can execute arbitrary processes
- The application is not secure

## Building

```powershell
cd example_mods/malicious_test
.\build.ps1 -Install
```

## Testing

After installing, reload mods and check the console:
- **PASS**: You see "BLOCKED due to security violations"
- **FAIL**: You see "SECURITY BREACH" messages
