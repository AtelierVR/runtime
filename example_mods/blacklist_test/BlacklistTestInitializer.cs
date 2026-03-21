using System;
using System.Linq;
using System.Reflection;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace BlacklistTestMod
{
    /// <summary>
    /// Test mod that verifies the assembly security validation system.
    /// 
    /// IMPORTANT: This mod tests the VALIDATION API, not actual blocked access.
    /// 
    /// The security system works at IL-scanning level:
    /// - It scans assembly IL code BEFORE loading
    /// - It blocks mods that have COMPILE-TIME references to dangerous types
    /// - Runtime reflection (Type.GetType) is NOT blocked by IL scanning
    /// 
    /// This test mod:
    /// 1. Uses reflection to access the validator API
    /// 2. Tests that the validator correctly identifies dangerous types
    /// 3. Does NOT directly reference dangerous types (so it loads successfully)
    /// </summary>
    public class BlacklistTestInitializer : IMainModInitializer
    {
        private IModCoreAPI _api;

        public void OnInitialize(IModCoreAPI api)
        {
            _api = api;
            Debug.Log("[BlacklistTest] OnInitialize - Starting security validation tests...");
        }

        public void OnInitializeMain(IMainModCoreAPI api)
        {
            Debug.Log("==========================================");
            Debug.Log("   Assembly Security Validator Test Mod");
            Debug.Log("===========================================");
            Debug.Log("");
            Debug.Log("This mod tests the IL-level security validation");
            Debug.Log("that scans assemblies BEFORE loading them.");
            Debug.Log("");
            
            // Get the validator type via reflection using our own assembly's reference
            var validatorType = GetValidatorType();
            if (validatorType == null)
            {
                Debug.LogWarning("[BlacklistTest] AssemblySecurityValidator not found.");
                Debug.LogWarning("This might mean:");
                Debug.LogWarning("  1. The validator hasn't been added to Nox.ModLoader yet");
                Debug.LogWarning("  2. You're running in IL2CPP mode (validator is Mono-only)");
                Debug.Log("");
                Debug.Log("Falling back to runtime reflection tests...");
                RunLegacyTests();
                return;
            }
            
            Debug.Log("[BlacklistTest] Found AssemblySecurityValidator - running tests...");
            Debug.Log("");
            
            // Test type blacklist patterns
            TestTypeBlacklist(validatorType);
            
            Debug.Log("");
            Debug.Log("===========================================");
            Debug.Log("      Security Tests Completed");
            Debug.Log("===========================================");
            Debug.Log("");
            Debug.Log("HOW THE SECURITY SYSTEM WORKS:");
            Debug.Log("- IL Scanner checks assembly BEFORE loading");
            Debug.Log("- Blocks compile-time references to dangerous types");
            Debug.Log("- Runtime reflection (Type.GetType) is NOT blocked");
            Debug.Log("");
            Debug.Log("To test blocking, create a mod that directly uses:");
            Debug.Log("  new System.Diagnostics.Process()");
            Debug.Log("  System.Net.Sockets.Socket.Create()");
            Debug.Log("  System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly()");
            Debug.Log("That mod will fail to load with security violations.");
        }

        private Type GetValidatorType()
        {
            // Try to find the AssemblySecurityValidator type using Type.GetType
            // This avoids using AppDomain.GetAssemblies() which is blacklisted
            try
            {
                // Try to get the type directly by its full assembly-qualified name
                var typeName = "Nox.ModLoader.Assemblies.AssemblySecurityValidator, Nox.ModLoader";
                var type = Type.GetType(typeName);
                if (type != null) return type;
                
                // Alternative: use the IModCoreAPI type to find the assembly
                var apiType = typeof(IModCoreAPI);
                var modLoaderAssembly = apiType.Assembly;
                
                // The IModCoreAPI is in Nox.CCK, but we can navigate from there
                // Try loading from Nox.ModLoader assembly reference
                foreach (var refAssemblyName in modLoaderAssembly.GetReferencedAssemblies())
                {
                    if (refAssemblyName.Name == "Nox.ModLoader")
                    {
                        var modLoaderAsm = Assembly.Load(refAssemblyName);
                        type = modLoaderAsm.GetType("Nox.ModLoader.Assemblies.AssemblySecurityValidator");
                        if (type != null) return type;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BlacklistTest] Failed to find validator: {ex.Message}");
            }
            return null;
        }

        private void TestTypeBlacklist(Type validatorType)
        {
            Debug.Log("=== Testing Type Blacklist Patterns ===");
            
            // Get all methods named IsTypeBlacklisted and find the one with 2 parameters
            var methods = validatorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "IsTypeBlacklisted")
                .ToArray();
            
            var isTypeBlacklisted = methods.FirstOrDefault(m => m.GetParameters().Length == 2);
            
            if (isTypeBlacklisted == null)
            {
                Debug.LogError("[BlacklistTest] Could not find IsTypeBlacklisted method with 2 parameters!");
                Debug.Log($"[BlacklistTest] Found {methods.Length} overloads:");
                foreach (var m in methods)
                {
                    var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Debug.Log($"  - IsTypeBlacklisted({parms})");
                }
                return;
            }
            
            // Types that SHOULD be blocked
            var dangerousTypes = new[]
            {
                ("System.Diagnostics.Process", "Process execution"),
                ("System.Diagnostics.ProcessStartInfo", "Process configuration"),
                ("System.Net.Sockets.Socket", "Raw socket access"),
                ("System.Net.Sockets.TcpClient", "TCP networking"),
                ("System.Reflection.Emit.AssemblyBuilder", "Dynamic assembly creation"),
                ("System.Reflection.Emit.TypeBuilder", "Dynamic type creation"),
                ("System.Reflection.Emit.ILGenerator", "IL code generation"),
                ("System.Reflection.Emit.DynamicMethod", "Dynamic methods"),
                ("Microsoft.CSharp.CSharpCodeProvider", "C# compilation"),
                ("System.Security.SecurityManager", "Security manipulation"),
                ("Microsoft.Win32.Registry", "Windows registry access"),
                ("Microsoft.Win32.RegistryKey", "Registry key access"),
                ("System.Environment", "Environment access"),
                ("System.AppDomain", "AppDomain manipulation"),
                ("UnityEditor.EditorApplication", "Editor API (builds only)"),
            };
            
            Debug.Log("");
            Debug.Log("Checking dangerous type patterns:");
            int blockedCorrectly = 0;
            foreach (var (typeName, description) in dangerousTypes)
            {
                var args = new object[] { typeName, null };
                var isBlocked = (bool)isTypeBlacklisted.Invoke(null, args);
                var matchedPattern = args[1] as string;
                
                if (isBlocked)
                {
                    Debug.Log($"  ✓ {typeName}");
                    Debug.Log($"      ({description}) - Pattern: {matchedPattern}");
                    blockedCorrectly++;
                }
                else
                {
                    Debug.LogWarning($"  ✗ {typeName} NOT in blacklist");
                    Debug.LogWarning($"      ({description})");
                }
            }
            
            Debug.Log("");
            Debug.Log($"Type Blacklist Results: {blockedCorrectly}/{dangerousTypes.Length} dangerous types recognized");
            
            if (blockedCorrectly >= dangerousTypes.Length * 0.8f) // 80% threshold
            {
                Debug.Log(">>> TYPE BLACKLIST TEST PASSED <<<");
            }
            else
            {
                Debug.LogWarning(">>> TYPE BLACKLIST MAY NEED UPDATES <<<");
            }
        }

        private void RunLegacyTests()
        {
            Debug.Log("=== Legacy Runtime Reflection Tests ===");
            Debug.Log("(These test if types exist, not if they're blocked)");
            Debug.Log("");
            
            TestTypeExists("System.Diagnostics.Process", "System");
            TestTypeExists("System.Net.Sockets.Socket", "System");
            TestTypeExists("System.Reflection.Emit.AssemblyBuilder", "mscorlib");
            TestTypeExists("Microsoft.CSharp.CSharpCodeProvider", "Microsoft.CSharp");
            TestTypeExists("System.Security.SecurityManager", "mscorlib");
            TestTypeExists("Microsoft.Win32.Registry", "mscorlib");
            
            Debug.Log("");
            Debug.Log("NOTE: Type.GetType() uses runtime reflection.");
            Debug.Log("The IL security scanner blocks COMPILE-TIME references,");
            Debug.Log("not runtime reflection. Types may exist but still be blocked");
            Debug.Log("for mods that directly reference them in their code.");
        }

        private void TestTypeExists(string typeName, string assemblyHint)
        {
            try
            {
                var fullTypeName = $"{typeName}, {assemblyHint}";
                var type = Type.GetType(fullTypeName);
                if (type != null)
                {
                    Debug.Log($"  ℹ {typeName}: Exists (runtime accessible)");
                }
                else
                {
                    Debug.Log($"  ℹ {typeName}: Not found (may not be loaded)");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"  ℹ {typeName}: Error - {ex.GetType().Name}");
            }
        }

        public void OnPostInitializeMain()
        {
            Debug.Log("[BlacklistTest] Test mod initialization complete.");
        }

        public void OnDispose()
        {
            Debug.Log("[BlacklistTest] Test mod disposed.");
        }
    }
}
