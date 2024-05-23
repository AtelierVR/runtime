#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
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

                Debug.Log("Found mod: " + modSearcher.ModInfoPath);
                var modmetadata = ModMetadata.LoadFromPath(modSearcher.ModInfoPath);
                Debug.Log("Mod name: " + modmetadata.GetName());
            }


        }

        private class ModSearcher
        {
            public string ModInfoPath;
            public string ModDLLPath;
            public string ModASMDefPath;
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