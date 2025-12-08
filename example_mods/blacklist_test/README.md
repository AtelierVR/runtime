# Blacklist Test Mod

This is a **security test mod** that attempts to use various blacklisted assemblies to verify that the mod loader's security system is working correctly.

## Purpose

This mod tests the assembly blacklist system by attempting to:

1. **Process Access** - Try to use `System.Diagnostics.Process` to start external processes
2. **Socket Access** - Try to use `System.Net.Sockets` for raw network access
3. **Reflection.Emit** - Try to dynamically generate code at runtime
4. **C# Compiler** - Try to compile and execute C# code dynamically
5. **Security Access** - Try to access `System.Security` internals
6. **Registry Access** - Try to access Windows Registry via `Microsoft.Win32`

## Expected Results

All tests should **PASS** (meaning the blacklist blocked the access):

```
[BlacklistTest] Test 1 PASSED: Process type not accessible
[BlacklistTest] Test 2 PASSED: Socket type not accessible
[BlacklistTest] Test 3 PASSED: AssemblyBuilder not accessible
[BlacklistTest] Test 4 PASSED: CSharpCodeProvider not accessible
[BlacklistTest] Test 5 PASSED: SecurityManager not accessible
[BlacklistTest] Test 6 PASSED: Registry not accessible
```

If any test shows `SECURITY ISSUE`, it means the blacklist is not working correctly for that assembly.

## Building

```powershell
cd example_mods/blacklist_test
.\build.ps1
```

Or manually:

```powershell
dotnet build -c Release
```

## Installation

After building, copy the following files to your Mods folder:
- `bin/BlacklistTestMod.dll`
- `nox.mod.json`

The build script does this automatically.

## Note

This mod is for **testing purposes only**. It intentionally tries to access dangerous system APIs to verify they are properly blocked by the security system.
