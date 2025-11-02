# Socket Binding Exception Fix

## Problem
The application was experiencing a `SocketException` with the French error message:
> Une seule utilisation de chaque adresse de socket (protocole/adresse réseau/port) est habituellement autorisée.

This translates to: "Only one usage of each socket address (protocol/network address/port) is normally allowed."

## Root Cause
The WebSocket server in the `api.nox.control` module was attempting to bind to a port that was already in use. This occurred in the `WebSocketServer.Start()` method when `_listener.Start()` was called.

## Solution Applied

### 1. Enhanced Error Handling in WebSocketServer
- Added try-catch block around `_listener.Start()` in `WebSocketServer.cs`
- Provides clear error messages when socket binding fails
- Preserves the exception for upstream handling

### 2. Improved Port Management in Main.cs
- Enhanced the `Reload()` method with retry logic
- If the preferred port fails, automatically tries an alternative free port
- Added comprehensive error logging for troubleshooting

### 3. Robust Port Testing
- Updated `IsUsablePort()` method to use `IPAddress.Any` instead of `IPAddress.Loopback`
- Added specific exception handling with warning messages
- Improved `GetFreePort()` method with fallback random port generation

### 4. Better Resource Cleanup
- Enhanced `Stop()` method with proper exception handling
- Improved `OnDisposeMain()` with defensive programming practices
- Ensures proper resource disposal even if errors occur during shutdown

## Technical Changes

### Files Modified:
1. `Packages/api.nox.control/Scripts/WebSocketServer.cs`
2. `Packages/api.nox.control/Scripts/Main.cs`

### Key Improvements:
- **Port Conflict Resolution**: Automatic fallback to alternative ports
- **Error Visibility**: Clear logging of socket binding issues
- **Resource Management**: Proper cleanup to prevent port leaks
- **Robustness**: Defensive programming for edge cases

## Benefits
1. **Reliability**: Application continues to function even if preferred port is unavailable
2. **Diagnostics**: Clear error messages help identify port conflicts
3. **Stability**: Better resource management prevents future port binding issues
4. **User Experience**: Automatic recovery reduces need for manual intervention

## Prevention
The fix includes mechanisms to:
- Test port availability before binding
- Automatically find alternative ports
- Properly clean up resources to prevent port leaks
- Log detailed information for troubleshooting

This solution ensures the WebSocket control server can start reliably even in environments with port conflicts.