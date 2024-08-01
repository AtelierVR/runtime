#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Nox.Mods;
using Nox.Editor.Manage;

namespace Nox.Editor.Mods
{
    public class ModSource
    {
        public string Path;
        public ModMetadata Metadata;
        public string DllPatch => System.IO.Path.Combine(
            Application.dataPath,
            "..", "Library", "ScriptAssemblies",
            Metadata.GetId() + ".dll"
        );
    }



    [InitializeOnLoad]
    public class EditorModManager
    {
        private static List<EditorMod> _mods = new();
        private static List<ModMetadata> _detectedmetadatas = new();
        public static CCK.Mods.ModMetadata[] DetectedMetadatas() => _detectedmetadatas.ToArray();

        static EditorModManager()
        {
            foreach (var mod in _mods) RemoveMod(mod);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Update;

            var sources = SourceModManager.FindAllSource();
            Debug.Log("Found " + string.Join(", ", sources));
            if (sources.Length == 0) return;

            // get all metadata
            var modMetas = sources
                .Select(SourceModManager.FindMetadata)
                .Where(meta => meta != null)
                .Select(meta => new ModSource() { Path = SourceModManager.FindSource(meta.GetId()), Metadata = meta })
                .ToList();
            _detectedmetadatas = modMetas.Select(meta => meta.Metadata).ToList();
            if (modMetas.Count == 0) return;

            // check if the mod has good entry points
            modMetas = modMetas
                .Where(meta => meta.Metadata.GetEntryPoints().GetMain().Length + meta.Metadata.GetEntryPoints().GetEditor().Length > 0)
                .Where(meta =>
                {
                    if (File.Exists(meta.DllPatch)) return true;
                    Debug.LogWarning("DLL file not found: " + meta.DllPatch);
                    return false;
                })
                .ToList();
            if (modMetas.Count == 0) return;

            // check if the mod has no conflicts provides
            modMetas = modMetas
                .Where(meta =>
                {
                    foreach (var provide in meta.Metadata.GetProvides())
                        foreach (var mod in modMetas)
                            if (mod.Metadata.GetId() == provide)
                            {
                                Debug.LogWarning("Mod " + meta.Metadata.GetId() + " provides " + provide + " but it is already provided by " + mod.Metadata.GetId());
                                return false;
                            }
                    return true;
                })
                .ToList();
            if (modMetas.Count == 0) return;


            foreach (var mod in _mods) RemoveMod(mod);

            Debug.Log("Loading mods");

            var modSearchers = new List<ModSource>();
            foreach (var mod in modMetas)
            {
                var asmdefFiles = Directory.GetFiles(mod.Path, mod.Metadata.GetId() + ".asmdef", SearchOption.TopDirectoryOnly);
                var asmdef = asmdefFiles.Length == 1 ? JObject.Parse(File.ReadAllText(asmdefFiles[0])) : null;
                if (asmdef == null || asmdef["name"] == null)
                {
                    Debug.LogError("No name found in asmdef file " + asmdefFiles[0]);
                    continue;
                }
                modSearchers.Add(mod);
                Debug.Log("Found mod " + mod.Metadata.GetId() + " v" + mod.Metadata.GetVersion());
                if (mod.Metadata.GetProvides().Length > 0)
                    Debug.Log("Provides: " + string.Join(", ", mod.Metadata.GetProvides()));
            }
            var platformEngine = new List<ModSource>();
            foreach (var mod in modSearchers)
                if (mod.Metadata.GetEntryPoints().GetMain().Length + mod.Metadata.GetEntryPoints().GetEditor().Length >= 0)
                    platformEngine.Add(mod);

            var ckeckedMods = new List<ModSource>();

            foreach (var mod in platformEngine)
            {
                foreach (var provide in mod.Metadata.GetProvides())
                    foreach (var mod2 in platformEngine)
                        if (mod2.Metadata.GetId() == provide)
                        {
                            Debug.LogError("Mod " + mod.Metadata.GetId() + " provides " + provide + " but it is already provided by " + mod2.Metadata.GetId());
                            break;
                        }
                ckeckedMods.Add(mod);
            }

            var readyMods = new List<ModSource>();
            var breakMods = new List<ModSource>();

            // check if the mod has no breaks depends conflicts
            foreach (var mod in ckeckedMods)
            {
                Debug.Log("Checking mod " + mod.Metadata.GetId());
                var broken = false;
                foreach (var required in mod.Metadata.GetBreaks())
                {
                    var found = false;
                    foreach (var mod2 in readyMods)
                        if (mod2.Metadata.GetId() == required.GetId() || mod2.Metadata.GetProvides().Contains(required.GetId()))
                            if (required.GetVersion().Matches(mod2.Metadata.GetVersion()))
                            {
                                found = true;
                                break;
                            }
                    if (found)
                    {
                        Debug.LogError("Mod " + mod.Metadata.GetId()
                            + " breaks " + required.GetId() + required.GetVersion().ToString()
                            + " but it is present (0x01)");
                        broken = true;
                    }
                }

                foreach (var required in mod.Metadata.GetDepends())
                {
                    var found = false;
                    ModSource gameMod = null;
                    foreach (var mod2 in ckeckedMods)
                        if (mod2.Metadata.GetId() == required.GetId() || mod2.Metadata.GetProvides().Contains(required.GetId()))
                        {
                            gameMod = mod2;
                            if (required.GetVersion().Matches(mod2.Metadata.GetVersion()))
                            {
                                found = true;
                                break;
                            }
                        }
                    if (!found)
                    {
                        Debug.LogError("Mod " + mod.Metadata.GetId()
                            + " depends on " + required.GetId() + required.GetVersion().ToString()
                            + (
                                gameMod != null
                                    ? " but it is present as " + gameMod.Metadata.GetId() + gameMod.Metadata.GetVersion().ToString()
                                    : " but it is not present (0x02)"
                            ));
                        broken = true;
                    }
                }

                foreach (var required in mod.Metadata.GetConflicts())
                {
                    var found = false;
                    foreach (var mod2 in ckeckedMods)
                        if (mod2.Metadata.GetId() == required.GetId() || mod2.Metadata.GetProvides().Contains(required.GetId()))
                            if (required.GetVersion().Matches(mod2.Metadata.GetVersion()))
                            {
                                found = true;
                                break;
                            }
                    if (found)
                    {
                        Debug.LogError("Mod " + mod.Metadata.GetId()
                            + " conflicts with " + required.GetId() + required.GetVersion().ToString()
                            + " but it is present (0x03)");
                        broken = true;
                    }
                }

                foreach (var required in mod.Metadata.GetRecommends())
                {
                    var found = false;
                    ModSource gameMod = null;
                    foreach (var mod2 in ckeckedMods)
                        if (mod2.Metadata.GetId() == required.GetId() || mod2.Metadata.GetProvides().Contains(required.GetId()))
                        {
                            gameMod = mod2;
                            if (required.GetVersion().Matches(mod2.Metadata.GetVersion()))
                            {
                                found = true;
                                break;
                            }
                        }
                    if (!found)
                    {
                        Debug.LogWarning("Mod " + mod.Metadata.GetId()
                            + " recommends " + required.GetId() + required.GetVersion().ToString()
                            + (
                                gameMod != null
                                    ? " but it is present as " + gameMod.Metadata.GetId() + gameMod.Metadata.GetVersion().ToString()
                                    : " but it is not present (0x04)"
                            ));
                    }
                }

                Debug.Log("Mod " + mod.Metadata.GetId() + " v" + mod.Metadata.GetVersion() + (broken ? " is broken" : " is ready"));
                if (broken)
                    breakMods.Add(mod);
                else readyMods.Add(mod);
            }

            Debug.Log("Mods: " + readyMods.Count + " ready, " + breakMods.Count + " broken");

            // sort by dependencies
            readyMods.Sort((a, b) =>
            {
                var i = 0;
                foreach (var required in a.Metadata.GetDepends())
                    if (required.GetId() == b.Metadata.GetId() || b.Metadata.GetProvides().Contains(required.GetId()))
                        i++;
                foreach (var required in b.Metadata.GetDepends())
                    if (required.GetId() == a.Metadata.GetId() || a.Metadata.GetProvides().Contains(required.GetId()))
                        i--;
                return i;
            });

            for (var i = 0; i < readyMods.Count; i++)
                Debug.Log("Loading mod " + readyMods[i].Metadata.GetId() + " v" + readyMods[i].Metadata.GetVersion() + " (" + (i + 1) + "/" + readyMods.Count + ")");


            // load mods with entry points
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var success = true;
            foreach (var mod in readyMods)
            {
                var m = mod.Metadata.GetEntryPoints().GetMain();
                var e = mod.Metadata.GetEntryPoints().GetEditor();

                // get all assembly and find the main entry points
                var mc = new List<CCK.Mods.Initializers.ModInitializer>();
                var ec = new List<CCK.Mods.Initializers.EditorModInitializer>();
                foreach (Assembly assembly in assemblies)
                    foreach (var type in assembly.GetTypes())
                        if (m.Contains(type.FullName) && type.GetInterface(typeof(CCK.Mods.Initializers.ModInitializer).FullName) != null)
                            mc.Add((CCK.Mods.Initializers.ModInitializer)Activator.CreateInstance(type));
                        else if (e.Contains(type.FullName) && type.GetInterface(typeof(CCK.Mods.Initializers.EditorModInitializer).FullName) != null)
                            ec.Add((CCK.Mods.Initializers.EditorModInitializer)Activator.CreateInstance(type));

                // check if all entry points are found
                if (mc.Count != m.Length)
                    Debug.LogError("Failed to load main entry points for mod " + mod.Metadata.GetId());
                if (ec.Count != e.Length)
                    Debug.LogError("Failed to load editor entry points for mod " + mod.Metadata.GetId());

                // create the mod
                try
                {
                    Debug.Log("Loading mod " + mod.Metadata.GetId());
                    var modInstance = new EditorMod(mod.Metadata, mc.ToArray(), ec.ToArray(), mod.Path);
                    AddMod(modInstance);
                    if (!modInstance.SetEnabled(true))
                        throw new Exception("Failed to enable mod");
                }
                catch
                {
                    Debug.LogError("Failed to load mod " + mod.Metadata.GetId());
                    success = false;
                    break;
                }
            }
            if (!success)
                foreach (var modsearch in readyMods)
                {
                    var mod = GetMod(modsearch.Metadata.GetId());
                    if (mod != null)
                    {
                        mod.SetEnabled(false);
                        RemoveMod(mod);
                    }
                }
        }

        private class ModSearcher
        {
            public string ModInfoPath;
            public string ModDLLPath;
            public string ModASMDefPath;
            public ModMetadata ModMetadata;
        }

        private static void AddMod(EditorMod mod) => _mods.Add(mod);

        private static void RemoveMod(EditorMod mod)
        {
            if (mod.IsEnabled())
                mod.SetEnabled(false);
            _mods.Remove(mod);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                foreach (var mod in _mods) RemoveMod(mod);
        }

        private static void Update()
        {
            foreach (var mod in _mods)
                if (mod.IsEnabled())
                {
                    foreach (var modInitializer in mod.GetMainClasses())
                        modInitializer.OnUpdate();
                    foreach (var modInitializer in mod.GetEditorClasses())
                        modInitializer.OnUpdateEditor();
                }
        }

        internal static List<EditorMod> GetMods() => _mods;

        internal static EditorMod GetMod(string id) =>
            _mods.FirstOrDefault(mod => mod.GetMetadata().GetId() == id || mod.GetMetadata().GetProvides().Contains(id));

        internal static bool HasMod(string id)
        {
            foreach (var mod in _mods)
                if (mod.GetMetadata().GetId() == id || mod.GetMetadata().GetProvides().Contains(id))
                    return true;
            return false;
        }

        internal static string[] GetResourcesPaths()
        {
            var paths = new List<string>();
            foreach (var mod in _mods)
                paths.Add(mod.GetResourcePath());
            return paths.ToArray();
        }
    }
}
#endif