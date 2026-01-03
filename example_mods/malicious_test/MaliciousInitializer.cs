using System;
using System.Diagnostics;          // <-- This references Process, which is BLACKLISTED
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace MaliciousMod
{
    /// <summary>
    /// THIS MOD IS INTENTIONALLY MALICIOUS FOR TESTING PURPOSES.
    /// 
    /// It contains direct compile-time references to blacklisted types:
    /// - System.Diagnostics.Process
    /// - System.Diagnostics.ProcessStartInfo
    /// 
    /// The security validator should BLOCK this mod from loading.
    /// If this mod's OnInitialize ever runs, it means the security system failed.
    /// </summary>
    public class MaliciousInitializer : IMainModInitializer
    {
        public void OnInitialize(IModCoreAPI api)
        {
            // If this code runs, the security system has FAILED
            UnityEngine.Debug.LogError("!!! SECURITY BREACH !!!");
            UnityEngine.Debug.LogError("MaliciousMod was allowed to load!");
            UnityEngine.Debug.LogError("The security validator is NOT working!");
        }

        public void OnInitializeMain(IMainModCoreAPI api)
        {
            // This code should NEVER run
            // The security validator should block this mod at load time
            
            UnityEngine.Debug.LogError("!!! SECURITY BREACH - ATTEMPTING MALICIOUS ACTION !!!");
            
            // Direct reference to Process - this is what the IL scanner should detect
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c echo SECURITY_BREACH",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            
            // If we get here, the system is compromised
            try
            {
                var process = Process.Start(startInfo);
                var output = process?.StandardOutput.ReadToEnd();
                UnityEngine.Debug.LogError($"!!! EXECUTED COMMAND: {output} !!!");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Process failed but mod still loaded: {ex.Message}");
            }
        }

        public void OnDispose()
        {
            UnityEngine.Debug.LogError("MaliciousMod disposing - security was breached");
        }
    }
}
