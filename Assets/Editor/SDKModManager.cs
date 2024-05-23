#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Nox.CCK.Mods;
using UnityEditor;
using UnityEngine;
using ModMetadata = Nox.Mods.ModMetadata;

namespace Nox.CCK.Editor
{
    [InitializeOnLoad]
    public class CCKModManager
    {
        public static List<Nox.Mods.EditorMod> Mods = new();

        static CCKModManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Update;

            Mods.Clear();
            // get all mod.json(c) with asmdef file

            var modFiles = Directory.GetFiles(Application.dataPath, "nox.mod.json", SearchOption.AllDirectories);
            var modcFiles = Directory.GetFiles(Application.dataPath, "nox.mod.jsonc", SearchOption.AllDirectories);
            var files = new List<string>();
            files.AddRange(modFiles);
            files.AddRange(modcFiles);

            var modSearchers = new List<ModSearcher>();
            foreach (var file in files)
            {
                var modSearcher = new ModSearcher();
                modSearcher.ModInfoPath = file;
                var parentDir = Directory.GetParent(file).FullName;
                var asmdefFiles = Directory.GetFiles(parentDir, "*.asmdef", SearchOption.TopDirectoryOnly);
                var dll = Directory.GetFiles(parentDir, "*.dll", SearchOption.TopDirectoryOnly);
                if (asmdefFiles.Length > 1)
                {
                    Debug.LogError("More than one asmdef file found in " + parentDir);
                    continue;
                }
                modSearcher.ModDLLPath = dll.Length == 1 ? dll[0] : null;
                if (asmdefFiles.Length == 1)
                {
                    JObject asmdef = JObject.Parse(File.ReadAllText(asmdefFiles[0]));
                    if (asmdef["name"] == null)
                    {
                        Debug.LogError("No name found in asmdef file " + asmdefFiles[0]);
                        continue;
                    }
                    modSearcher.ModASMDefPath = asmdefFiles[0];
                    modSearcher.ModDLLPath = Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies", asmdef["name"] + ".dll");
                }
                if (modSearcher.ModDLLPath == null || !File.Exists(modSearcher.ModDLLPath))
                {
                    Debug.LogError("DLL file not found: " + modSearcher.ModDLLPath);
                    continue;
                }
                var modmetadata = ModMetadata.LoadFromPath(modSearcher.ModInfoPath);
                if (modmetadata == null)
                {
                    Debug.LogError("Failed to load metadata from " + modSearcher.ModInfoPath);
                    continue;
                }
                modSearcher.ModMetadata = modmetadata;
                modSearchers.Add(modSearcher);
                Debug.Log("Found mod " + modmetadata.GetId() + " v" + modmetadata.GetVersion());
                if (modmetadata.GetProvides().Length > 0)
                    Debug.Log("Provides: " + string.Join(", ", modmetadata.GetProvides()));
            }

            var sidedMods = new List<ModSearcher>();
            foreach (var modSearcher in modSearchers)
                if (modSearcher.ModMetadata.GetSide().HasFlag(CCK.Mods.Metadata.SideFlags.Editor))
                    sidedMods.Add(modSearcher);
                else Debug.LogWarning("Mod " + modSearcher.ModMetadata.GetId() + " is not for the editor");

            var platformEngine = new List<ModSearcher>();
            foreach (var modSearcher in sidedMods)
            {
                var incompatiblePlatform = true;
                foreach (var platform in modSearcher.ModMetadata.GetPlatforms())
                    if (platform == "*")
                    {
                        Debug.Log("Mod " + modSearcher.ModMetadata.GetId() + " is for all platforms");
                        incompatiblePlatform = false;
                        break;
                    }
                    else if (platform == SuppordTarget.GetTargetName((SuppordBuildTarget)EditorUserBuildSettings.activeBuildTarget))
                    {
                        incompatiblePlatform = false;
                        break;
                    }
                var incompatibleEngine = true;
                Mods.Metadata.Engine engineMod = null;
                foreach (var engine in modSearcher.ModMetadata.GetEngines())
                    if (engine.GetName() == Engine.Unity)
                    {
                        engineMod = engine;
                        if (engine.GetVersion().Matches(new System.Version(Application.unityVersion.Split('f')[0])))
                        {
                            incompatibleEngine = false;
                            break;
                        }
                    }
                if (incompatiblePlatform)
                    Debug.LogError("Mod " + modSearcher.ModMetadata.GetId()
                        + " not supported on the current platform (" + SuppordTarget.GetTargetName((SuppordBuildTarget)EditorUserBuildSettings.activeBuildTarget) + ")");
                if (incompatibleEngine)
                    if (engineMod != null)
                        Debug.LogError("Mod " + modSearcher.ModMetadata.GetId()
                            + " support " + engineMod.GetName() + engineMod.GetVersion().ToString()
                            + " but the current version is " + Application.unityVersion);
                    else Debug.LogError("Mod " + modSearcher.ModMetadata.GetId() + " does not support Unity v" + Application.unityVersion);

                var noclass = modSearcher.ModMetadata.GetEntryPoints().GetMain().Length + modSearcher.ModMetadata.GetEntryPoints().GetEditor().Length == 0;
                if (!incompatiblePlatform && !incompatibleEngine && !noclass)
                    platformEngine.Add(modSearcher);
            }

            var ckeckedMods = new List<ModSearcher>();
            foreach (var modSearcher in sidedMods)
            {
                foreach (var provide in modSearcher.ModMetadata.GetProvides())
                    foreach (var mod in sidedMods)
                        if (mod.ModMetadata.GetId() == provide)
                        {
                            Debug.LogError("Mod " + modSearcher.ModMetadata.GetId() + " provides " + provide + " but it is already provided by " + mod.ModMetadata.GetId());
                            break;
                        }
                ckeckedMods.Add(modSearcher);
            }

            var readyMods = new List<ModSearcher>();
            var breakMods = new List<ModSearcher>();
            foreach (var modSearcher in ckeckedMods)
            {
                var broken = false;
                foreach (var required in modSearcher.ModMetadata.GetBreaks())
                {
                    var found = false;
                    foreach (var mod in readyMods)
                        if (mod.ModMetadata.GetId() == required.GetId() || mod.ModMetadata.GetProvides().Contains(required.GetId()))
                            if (required.GetVersion().Matches(mod.ModMetadata.GetVersion()))
                            {
                                found = true;
                                break;
                            }
                    if (found)
                    {
                        Debug.LogError("Mod " + modSearcher.ModMetadata.GetId()
                            + " breaks " + required.GetId() + required.GetVersion().ToString()
                            + " but it is present (0x01)");
                        broken = true;
                    }
                }

                foreach (var required in modSearcher.ModMetadata.GetDepends())
                {
                    var found = false;
                    ModSearcher gameMod = null;
                    foreach (var mod in ckeckedMods)
                        if (mod.ModMetadata.GetId() == required.GetId() || mod.ModMetadata.GetProvides().Contains(required.GetId()))
                        {
                            gameMod = mod;
                            if (required.GetVersion().Matches(mod.ModMetadata.GetVersion()))
                            {
                                found = true;
                                break;
                            }
                        }
                    if (!found)
                    {
                        Debug.LogError("Mod " + modSearcher.ModMetadata.GetId()
                            + " depends on " + required.GetId() + required.GetVersion().ToString()
                            + (
                                gameMod != null
                                    ? " but it is present as " + gameMod.ModMetadata.GetId() + gameMod.ModMetadata.GetVersion().ToString()
                                    : " but it is not present (0x02)"
                            ));
                        broken = true;
                    }
                }

                foreach (var required in modSearcher.ModMetadata.GetConflicts())
                {
                    var found = false;
                    foreach (var mod in ckeckedMods)
                        if (mod.ModMetadata.GetId() == required.GetId() || mod.ModMetadata.GetProvides().Contains(required.GetId()))
                            if (required.GetVersion().Matches(mod.ModMetadata.GetVersion()))
                            {
                                found = true;
                                break;
                            }
                    if (found)
                    {
                        Debug.LogError("Mod " + modSearcher.ModMetadata.GetId()
                            + " conflicts with " + required.GetId() + required.GetVersion().ToString()
                            + " but it is present (0x03)");
                        broken = true;
                    }
                }

                foreach (var required in modSearcher.ModMetadata.GetRecommends())
                {
                    var found = false;
                    ModSearcher gameMod = null;
                    foreach (var mod in ckeckedMods)
                        if (mod.ModMetadata.GetId() == required.GetId() || mod.ModMetadata.GetProvides().Contains(required.GetId()))
                        {
                            gameMod = mod;
                            if (required.GetVersion().Matches(mod.ModMetadata.GetVersion()))
                            {
                                found = true;
                                break;
                            }
                        }
                    if (!found)
                    {
                        Debug.LogWarning("Mod " + modSearcher.ModMetadata.GetId()
                            + " recommends " + required.GetId() + required.GetVersion().ToString()
                            + (
                                gameMod != null
                                    ? " but it is present as " + gameMod.ModMetadata.GetId() + gameMod.ModMetadata.GetVersion().ToString()
                                    : " but it is not present (0x04)"
                            ));
                    }
                }

                if (broken)
                    breakMods.Add(modSearcher);
                else readyMods.Add(modSearcher);
            }

            // prioritize mods by dependencies
            readyMods.Sort((a, b) =>
            {
                foreach (var required in a.ModMetadata.GetDepends())
                    if (required.GetId() == b.ModMetadata.GetId() || b.ModMetadata.GetProvides().Contains(required.GetId()))
                        return 1;
                foreach (var required in b.ModMetadata.GetDepends())
                    if (required.GetId() == a.ModMetadata.GetId() || a.ModMetadata.GetProvides().Contains(required.GetId()))
                        return -1;
                return 0;
            });

            for (var i = 0; i < readyMods.Count; i++)
                Debug.Log("Loading mod " + readyMods[i].ModMetadata.GetId() + " v" + readyMods[i].ModMetadata.GetVersion() + " (" + (i + 1) + "/" + readyMods.Count + ")");

            var loadedMods = new List<Nox.Mods.EditorMod>();
            var success = true;
            foreach (var modSearcher in readyMods)
            {
                var assembly = Assembly.LoadFile(modSearcher.ModDLLPath);
                var main = modSearcher.ModMetadata.GetEntryPoints().GetMain();
                var editor = modSearcher.ModMetadata.GetEntryPoints().GetEditor();
                var mainClasses = new List<Mods.Initializers.ModInitializer>();
                var editorClasses = new List<Mods.Initializers.EditorModInitializer>();
                foreach (var type in assembly.GetTypes())
                    if (main.Contains(type.FullName) && type.GetInterfaces().Contains(typeof(Mods.Initializers.ModInitializer)))
                        mainClasses.Add((Mods.Initializers.ModInitializer)Activator.CreateInstance(type));
                    else if (editor.Contains(type.FullName) && type.GetInterfaces().Contains(typeof(Mods.Initializers.EditorModInitializer)))
                        editorClasses.Add((Mods.Initializers.EditorModInitializer)Activator.CreateInstance(type));
                if (mainClasses.Count != main.Length)
                    Debug.LogError("Failed to load main entry points for mod " + modSearcher.ModMetadata.GetId());
                if (editorClasses.Count != editor.Length)
                    Debug.LogError("Failed to load editor entry points for mod " + modSearcher.ModMetadata.GetId());

                Debug.Log("Loaded mod " + modSearcher.ModMetadata.GetId() + " v" + modSearcher.ModMetadata.GetVersion());

                try
                {
                    var mod = new Nox.Mods.EditorMod(modSearcher.ModMetadata, editorClasses[0]);
                    loadedMods.Add(mod);
                    mod.SetEnabled(true);
                }
                catch
                {
                    Debug.LogError("Failed to load mod " + modSearcher.ModMetadata.GetId());
                    success = false;
                }
                if (!success) break;
            }

            if (success)
                foreach (var mod in loadedMods) AddMod(mod);
            else
                foreach (var mod in loadedMods)
                    try
                    {
                        mod.SetEnabled(false);
                    }
                    catch
                    {
                        Debug.LogError("Failed to unload mod " + mod.GetMetadata().GetId());
                    }

        }

        private class ModSearcher
        {
            public string ModInfoPath;
            public string ModDLLPath;
            public string ModASMDefPath;
            public ModMetadata ModMetadata;
        }

        private static void AddMod(Nox.Mods.EditorMod mod) => Mods.Add(mod);

        private static void RemoveMod(Nox.Mods.EditorMod mod) => Mods.Remove(mod);

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                foreach (var mod in Mods) RemoveMod(mod);
        }

        private static void Update()
        {
            foreach (var mod in Mods)
                mod.GetEditorAssembly().OnUpdateEditor();
        }
    }
}
#endif