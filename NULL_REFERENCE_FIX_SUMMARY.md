# Null Reference Exception Fix - WorldComponent

## Problem
A `NullReferenceException` was occurring in the `WorldComponent.SearchInstances` method at line 173. The error was:

```
NullReferenceException: Object reference not set to an instance of an object
  at api.nox.world.client.WorldComponent.SearchInstances (Nox.Worlds.IWorld world, System.String server, System.Threading.CancellationToken token, System.Action`1[T] callback) [0x0005a] in .\Packages\api.nox.world\src\client\WorldComponent.cs:173
```

## Root Cause
The issue was caused by a race condition in accessing the `Client.InstanceAPI` property. The code pattern was:

1. Check if `Client.InstanceAPI == null` at line 168
2. Use `Client.InstanceAPI` directly at lines 173 and 177 without storing it locally

Since `InstanceAPI` is a property that dynamically resolves the mod instance, it could become `null` between the check and the usage, especially during mod loading/unloading or initialization phases.

## Solution Applied

### 1. Fixed Race Condition in WorldComponent.cs
**Before:**
```csharp
if (Client.InstanceAPI == null) {
    Logger.LogError("InstanceAPI is not available", this, tag: "WorldComponent");
    return Array.Empty<IInstance>();
}

var request = Client.InstanceAPI  // Could be null here!
    .MakeSearchRequest()
    .SetWorld(world.ToIdentifier());

var response = await Client.InstanceAPI.Search(request, server)  // And here!
```

**After:**
```csharp
var instanceAPI = Client.InstanceAPI;  // Store once
if (instanceAPI == null) {
    Logger.LogError("InstanceAPI is not available", this, tag: "WorldComponent");
    return Array.Empty<IInstance>();
}

var request = instanceAPI  // Safe to use
    .MakeSearchRequest()
    .SetWorld(world.ToIdentifier());

var response = await instanceAPI.Search(request, server)  // Safe to use
```

### 2. Enhanced Defensive Programming in Client.cs
Made the API property accessors more robust by adding null-conditional operators:

**Before:**
```csharp
internal static IUiAPI UiAPI
    => Main.Instance.CoreAPI.ModAPI
        .GetMod("ui")
        .GetInstance<IUiAPI>();

internal static IInstanceAPI InstanceAPI
    => Main.Instance.CoreAPI.ModAPI
        .GetMod("instance")
        .GetInstance<IInstanceAPI>();
```

**After:**
```csharp
internal static IUiAPI UiAPI
    => Main.Instance?.CoreAPI?.ModAPI?
        .GetMod("ui")?
        .GetInstance<IUiAPI>();

internal static IInstanceAPI InstanceAPI
    => Main.Instance?.CoreAPI?.ModAPI?
        .GetMod("instance")?
        .GetInstance<IInstanceAPI>();
```

### 3. Improved Asset Loading Methods
Enhanced the `GetAsset` and `GetAssetAsync` methods with proper null checks:

**Before:**
```csharp
public static T GetAsset<T>(string path, string ns = null) where T : UnityEngine.Object
    => string.IsNullOrEmpty(ns)
        ? Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(path)
        : Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(ns, path);
```

**After:**
```csharp
public static T GetAsset<T>(string path, string ns = null) where T : UnityEngine.Object {
    if (Main.Instance?.CoreAPI?.AssetAPI == null) return null;
    return string.IsNullOrEmpty(ns)
        ? Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(path)
        : Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(ns, path);
}
```

## Technical Changes

### Files Modified:
1. `Packages/api.nox.world/src/client/WorldComponent.cs` - Fixed race condition in `SearchInstances` method
2. `Packages/api.nox.world/src/Client.cs` - Enhanced defensive programming for API properties and asset methods

### Key Improvements:
- **Thread Safety**: Eliminated race condition by storing API reference locally
- **Null Safety**: Added comprehensive null checks throughout the chain
- **Error Resilience**: Better handling of mod unavailability scenarios
- **Debugging**: Preserved clear error messages for troubleshooting

## Benefits
1. **Stability**: Eliminates the NullReferenceException during world component operations
2. **Robustness**: Application gracefully handles mod loading/unloading scenarios
3. **User Experience**: Prevents crashes when accessing world instances
4. **Maintainability**: Consistent defensive programming patterns across the codebase

## Prevention
This fix establishes a pattern for safely accessing dynamic API properties:
- Always store property values locally when using multiple times
- Use null-conditional operators for chained property access
- Provide meaningful error messages for debugging
- Handle edge cases gracefully rather than crashing

This solution ensures the world component functionality remains stable even during complex mod initialization sequences.