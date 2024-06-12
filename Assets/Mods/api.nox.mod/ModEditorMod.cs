#if UNITY_EDITOR
using System.Collections.Generic;
using Nox.CCK.Editor;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Nox.CCK;
using System.IO;
using System.Linq;
using System;

namespace api.nox.mod
{
    public class ModEditorMod : EditorModInitializer
    {
        internal static EditorModCoreAPI _api;
        internal List<string> _selectedMods = new();

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            _api = api;
            _selectedMods = new List<string>(Config.Load().Get("modding.selected_mods", new string[0]));
            api.PanelAPI.AddLocalPanel(new PanelExample(this));
        }

        public void OnUpdateEditor()
        {
        }

        public void OnDispose()
        {
        }

        internal void SaveSelectedMods(List<string> mods)
        {
            var config = Config.Load();
            config.Set("modding.selected_mods", mods.ToArray());
            config.Save();
        }
    }


    public class PanelExample : EditorPanelBuilder
    {
        public string Id { get; } = "builder";
        public string Name { get; } = "Mod/Builder";
        public bool Hidded { get; } = false;
        internal VisualElement _root = new();
        private ModEditorMod _mod;

        internal PanelExample(ModEditorMod mod) => _mod = mod;


        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();
            _root.Add(ModEditorMod._api.AssetAPI.GetLocalAsset<VisualTreeAsset>("builder").CloneTree());
            _root.Q<Label>("version").text = "v" + ModEditorMod._api.ModMetadata.GetVersion();
            var list = ModEditorMod._api.AssetAPI.GetLocalAsset<VisualTreeAsset>("list");
            var container = _root.Q<VisualElement>("list");
            var metadatas = ModEditorMod._api.ModAPI.GetDetectedMetadatas();
            Debug.Log("Detected mods: " + string.Join(", ", metadatas.Select(m => m.GetId())));
            var folder = Config.Load().Get<string>("modding.build_folder");
            if (!string.IsNullOrEmpty(folder))
                _root.Q<TextField>("build-folder").value = folder;
            else
            {
                folder = Path.Combine(Application.dataPath, "BuildedMods");
                SelectBuildFolder(folder);
            }

            _root.Q<TextField>("build-folder").RegisterCallback<ChangeEvent<string>>(evt => SelectBuildFolder(evt.newValue));
            _root.Q<Button>("open-build-folder").clicked += () => EditorUtility.RevealInFinder(folder);
            _root.Q<Button>("build-folder-select").clicked += () =>
            {
                var path = EditorUtility.SaveFolderPanel("Select folder to save build", "", "");
                if (string.IsNullOrEmpty(path)) return;
                SelectBuildFolder(path);
            };
            var platform = _root.Q<EnumField>("platform-field");
            platform.Init(SuppordTarget.GetCurrentTarget());
            foreach (var metadata in metadatas)
            {
                var item = list.CloneTree();
                item.Q<Label>("title").text = metadata.GetId();
                item.Q<Toggle>("check").value = _mod._selectedMods.Contains(metadata.GetId());
                item.Q<Toggle>("check").RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                        _mod._selectedMods.Add(metadata.GetId());
                    else
                        _mod._selectedMods.Remove(metadata.GetId());
                    _mod.SaveSelectedMods(_mod._selectedMods);
                });
                container.Add(item);
            }

            _root.Q<Button>("build-button").clicked += () =>
            {
                var path = Config.Load().Get<string>("modding.build_folder");
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError("Build folder not found!");
                    return;
                }
                else if (Directory.Exists(path))
                    Directory.Delete(path, true);
                if (!Directory.CreateDirectory(path).Exists)
                {
                    Debug.LogError("Failed to create build folder!");
                    return;
                }

                var platform = (BuildTarget)(SupportBuildTarget)_root.Q<EnumField>("platform-field").value;
                if (!GameBuilder.CanBuild(platform))
                {
                    EditorUtility.DisplayDialog("Error!", "Platform not supported!", "Ok");
                    Debug.LogError($"Platform {platform} not supported!");
                    return;
                }

                var select = new List<string>();
                foreach (var se in _mod._selectedMods)
                    if (FindModSource.FindSource(se) != null) select.Add(se);

                if (select.Count == 0)
                {
                    EditorUtility.DisplayDialog("Error!", "No mods selected!", "Ok");
                    Debug.LogError("No mods selected!");
                    return;
                }
                else if (select.Count != _mod._selectedMods.Count)
                {
                    EditorUtility.DisplayDialog("Error!", "Some mods not found!", "Ok");
                    var missing = _mod._selectedMods.Except(select).ToArray();
                    Debug.LogError($"Some mods not found: {string.Join(", ", missing)}");
                    return;
                }

                Debug.Log("Building mods: " + string.Join(", ", select) + " for platform " + platform + "...");

                var build = GameBuilder.Build(platform);
                if (!build)
                {
                    EditorUtility.DisplayDialog("Error!", "Build failed!", "Ok");
                    Debug.LogError("Failed to build!");
                    return;
                }

                var guidOfSelected = new Dictionary<string, string>();
                foreach (var id in select)
                {
                    var guid = Guid.NewGuid().ToString().Replace("-", "");
                    var pathDll = GameBuilder.SelectCompiled(platform, id, guid);
                    if (string.IsNullOrEmpty(pathDll))
                    {
                        EditorUtility.DisplayDialog("Error!", "Failed to select compiled!", "Ok");
                        Debug.LogError($"Failed to select compiled for {id}");
                        return;
                    }

                    var filenameDll = Path.GetFileName(pathDll);
                    var pathMeta = GMetadata.FindMetadata(id);
                    if (string.IsNullOrEmpty(pathMeta))
                    {
                        EditorUtility.DisplayDialog("Error!", "Failed to find metadata!", "Ok");
                        Debug.LogError($"Failed to find metadata for {id}");
                        return;
                    }

                    var mmeta = ModEditorMod._api.LibsAPI.LoadMetadata(pathMeta);
                    if (mmeta == null)
                    {
                        EditorUtility.DisplayDialog("Error!", "Failed to load metadata!", "Ok");
                        Debug.LogError($"Failed to load metadata for {id} at {pathMeta}");
                        return;
                    }

                    var gmeta = new GMetadata(ModEditorMod._api.LibsAPI.LoadMetadata(pathMeta));
                    gmeta.SetNewReference(new GReference(
                        mmeta.GetId(),
                        filenameDll,
                        new GEngine(Engine.Unity, Application.unityVersion.Split("f")[0]),
                        PlatfromExtensions.GetPlatfromFromBuildTarget(platform)
                    ));

                    gmeta.Save(pathMeta);
                    guidOfSelected.Add(id, guid);
                }

                Debug.Log("Build complete! Packing archives...");

                // pack archive
                var pathpacking = Config.Load().Get<string>("modding.build_folder");
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError("Build folder not found!");
                    return;
                }
                else foreach (var id in select)
                        if (!ModPacker.PackArchive(pathpacking, id))
                        {
                            EditorUtility.DisplayDialog("Error!", "Failed to pack archive!", "Ok");
                            Debug.LogError($"Failed to pack archive for {id}");
                            return;
                        }

                EditorUtility.DisplayDialog("Build complete!", "Build complete!", "Ok");
                Debug.Log("Build complete!");
            };

            return _root;
        }

        private void SelectBuildFolder(string path)
        {
            var config = Config.Load();
            config.Set("modding.build_folder", path);
            config.Save();
            _root.Q<TextField>("build-folder").value = path;
        }

        public void OnClosed()
        {
            Debug.Log("Panel Example closed!");
        }
    }
}
#endif